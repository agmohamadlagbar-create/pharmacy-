using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents;

/// <summary>
/// API Client communicating with FreeLLMAPI gateway endpoint.
/// </summary>
public class FreeLlmApiClient : IAiProviderClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FreeLlmApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ProviderName => "freellmapi";

    public FreeLlmApiClient(HttpClient httpClient, ILogger<FreeLlmApiClient> logger)
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
        CancellationToken cancellationToken = default)
    {
        var requestDto = new GroqChatRequestDto
        {
            Model = model,
            Messages = messages,
            Tools = tools,
            Stream = null
        };

        var jsonContent = JsonSerializer.Serialize(requestDto, _jsonOptions);

        _logger.LogDebug("[FreeLLMAPI] Sending request to model={Model}, tools={ToolCount}, messages={MessageCount}",
            model, tools?.Count ?? 0, messages.Count);

        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("FreeLLMAPI returned an error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                throw new RateLimitException(ProviderName);
            }

            response.EnsureSuccessStatusCode();
        }

        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseDto = JsonSerializer.Deserialize<GroqChatResponseDto>(responseString, _jsonOptions);
        var responseMessage = responseDto?.Choices?.FirstOrDefault()?.Message;
        var finishReason = responseDto?.Choices?.FirstOrDefault()?.FinishReason;

        var toolCalls = responseMessage?.ToolCalls ?? new List<GroqToolCallDto>();

        // Diagnostic: log when tools were sent but model didn't use them
        if (tools is { Count: > 0 } && toolCalls.Count == 0)
        {
            _logger.LogWarning(
                "[FreeLLMAPI] Tools were sent ({ToolCount} tools) but model returned NO tool_calls. " +
                "FinishReason={FinishReason}. Response content preview: {ContentPreview}",
                tools.Count, finishReason ?? "null",
                (responseMessage?.Content ?? "").Substring(0, Math.Min(responseMessage?.Content?.Length ?? 0, 300)));
        }
        else if (toolCalls.Count > 0)
        {
            _logger.LogInformation("[FreeLLMAPI] Model returned {ToolCallCount} tool call(s): {ToolNames}",
                toolCalls.Count, string.Join(", ", toolCalls.Select(tc => tc.Function.Name)));
        }

        return new ChatCompletionResult
        {
            Content = responseMessage?.Content ?? string.Empty,
            ToolCalls = toolCalls
        };
    }

    public async Task<ChatCompletionResult> SendChatCompletionStreamingAsync(
        string model,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken = default)
    {
        var requestDto = new GroqChatRequestDto
        {
            Model = model,
            Messages = messages,
            Tools = tools,
            Stream = true
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
            _logger.LogError("FreeLLMAPI streaming API returned an error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);

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

            if (payload.Contains("\"error\":", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("FreeLLMAPI streaming API returned an error payload: {Payload}", payload);
                throw new HttpRequestException($"FreeLLMAPI streaming API error: {payload}");
            }

            GroqStreamChunkDto? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<GroqStreamChunkDto>(payload, _jsonOptions);
            }
            catch (JsonException jsonException)
            {
                _logger.LogWarning(jsonException, "Unable to parse FreeLLMAPI stream chunk: {Payload}", payload);
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

        // Diagnostic: log tool-call results in streaming mode
        if (tools is { Count: > 0 } && toolCalls.Count == 0)
        {
            _logger.LogWarning(
                "[FreeLLMAPI Streaming] Tools were sent ({ToolCount} tools) but model returned NO tool_calls. " +
                "Response content preview: {ContentPreview}",
                tools.Count,
                fullResponseBuilder.ToString().Substring(0, Math.Min(fullResponseBuilder.Length, 300)));
        }
        else if (toolCalls.Count > 0)
        {
            _logger.LogInformation("[FreeLLMAPI Streaming] Model returned {ToolCallCount} tool call(s): {ToolNames}",
                toolCalls.Count, string.Join(", ", toolCalls.Select(tc => tc.Function.Name)));
        }

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
