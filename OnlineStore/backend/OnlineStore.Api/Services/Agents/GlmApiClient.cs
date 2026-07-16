using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents;

public class GlmApiClient : IAiProviderClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GlmApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ProviderName => "glm";

    public GlmApiClient(HttpClient httpClient, ILogger<GlmApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ChatCompletionResult> SendChatCompletionAsync(
        string model,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools = null,
        double? temperature = null,
        CancellationToken cancellationToken = default)
    {
        var requestDto = new GroqChatRequestDto
        {
            Model = model,
            Messages = messages,
            Tools = tools,
            Stream = null,
            Temperature = temperature
        };

        var jsonContent = JsonSerializer.Serialize(requestDto, _jsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("GLM API returned an error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new RateLimitException(ProviderName);
            }

            response.EnsureSuccessStatusCode();
        }

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseDto = JsonSerializer.Deserialize<GroqChatResponseDto>(responseString, _jsonOptions);
        var responseMessage = responseDto?.Choices?.FirstOrDefault()?.Message;

        return new ChatCompletionResult
        {
            Content = responseMessage?.Content ?? string.Empty,
            ToolCalls = responseMessage?.ToolCalls ?? new List<GroqToolCallDto>()
        };
    }

    public async Task<ChatCompletionResult> SendChatCompletionStreamingAsync(
        string model,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools,
        Func<string, CancellationToken, Task> onToken,
        double? temperature = null,
        CancellationToken cancellationToken = default)
    {
        var requestDto = new GroqChatRequestDto
        {
            Model = model,
            Messages = messages,
            Tools = tools,
            Stream = true,
            Temperature = temperature
        };

        var jsonContent = JsonSerializer.Serialize(requestDto, _jsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("GLM streaming API returned an error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new RateLimitException(ProviderName);
            }

            response.EnsureSuccessStatusCode();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var fullResponseBuilder = new StringBuilder();
        var toolCallAccumulators = new Dictionary<int, StreamingToolCallAccumulator>();

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;

            var payload = line["data:".Length..].Trim();
            if (string.Equals(payload, "[DONE]", StringComparison.Ordinal)) break;

            GroqStreamChunkDto? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<GroqStreamChunkDto>(payload, _jsonOptions);
            }
            catch (JsonException jsonException)
            {
                _logger.LogWarning(jsonException, "Unable to parse GLM stream chunk: {Payload}", payload);
                continue;
            }

            var choice = chunk?.Choices?.FirstOrDefault();
            var delta = choice?.Delta;
            if (delta is null) continue;

            if (!string.IsNullOrEmpty(delta.Content))
            {
                fullResponseBuilder.Append(delta.Content);
                await onToken(delta.Content, cancellationToken);
            }

            if (delta.ToolCalls is { Count: > 0 })
            {
                MergeToolCallDeltas(toolCallAccumulators, delta.ToolCalls);
            }
        }

        var toolCalls = BuildToolCalls(toolCallAccumulators);
        return new ChatCompletionResult
        {
            Content = fullResponseBuilder.ToString(),
            ToolCalls = toolCalls
        };
    }

    private static void MergeToolCallDeltas(
        Dictionary<int, StreamingToolCallAccumulator> accumulators,
        IReadOnlyCollection<GroqStreamToolCallDeltaDto> deltas)
    {
        foreach (var delta in deltas)
        {
            if (!accumulators.TryGetValue(delta.Index, out var accumulator))
            {
                accumulator = new StreamingToolCallAccumulator(delta.Index);
                accumulators[delta.Index] = accumulator;
            }

            if (!string.IsNullOrWhiteSpace(delta.Id)) accumulator.Id = delta.Id;
            if (!string.IsNullOrWhiteSpace(delta.Type)) accumulator.Type = delta.Type;
            if (!string.IsNullOrWhiteSpace(delta.Function?.Name)) accumulator.Name = delta.Function.Name;
            if (!string.IsNullOrEmpty(delta.Function?.Arguments)) accumulator.ArgumentsBuilder.Append(delta.Function.Arguments);
        }
    }

    private static List<GroqToolCallDto> BuildToolCalls(Dictionary<int, StreamingToolCallAccumulator> accumulators)
    {
        return accumulators
            .OrderBy(kvp => kvp.Key)
            .Select(kvp =>
            {
                var value = kvp.Value;
                return new GroqToolCallDto
                {
                    Id = string.IsNullOrWhiteSpace(value.Id) ? $"call_{Guid.NewGuid():N}" : value.Id,
                    Type = string.IsNullOrWhiteSpace(value.Type) ? "function" : value.Type,
                    Function = new GroqToolCallFunctionDto
                    {
                        Name = value.Name ?? string.Empty,
                        Arguments = value.ArgumentsBuilder.ToString()
                    }
                };
            })
            .ToList();
    }

    private sealed class StreamingToolCallAccumulator
    {
        public StreamingToolCallAccumulator(int index) { Index = index; }
        public int Index { get; }
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Name { get; set; }
        public StringBuilder ArgumentsBuilder { get; } = new();
    }
}
