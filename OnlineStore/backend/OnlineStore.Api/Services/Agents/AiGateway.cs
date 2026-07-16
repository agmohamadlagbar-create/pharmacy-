using Microsoft.Extensions.Options;
using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents;

public class AiGateway
{
    private readonly GroqApiClient _groqClient;
    private readonly GlmApiClient _glmClient;
    private readonly FreeLlmApiClient _freeLlmClient;
    private readonly AiUsageTracker _usageTracker;
    private readonly AiProviderConfig _config;
    private readonly ILogger<AiGateway> _logger;

    public AiGateway(
        GroqApiClient groqClient,
        GlmApiClient glmClient,
        FreeLlmApiClient freeLlmClient,
        AiUsageTracker usageTracker,
        IOptions<AiProviderConfig> config,
        ILogger<AiGateway> logger)
    {
        _groqClient = groqClient;
        _glmClient = glmClient;
        _freeLlmClient = freeLlmClient;
        _usageTracker = usageTracker;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<ChatCompletionResult> SendAsync(
        AiRequestContext requestContext,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools = null,
        CancellationToken ct = default)
    {
        var route = ResolveRoute(requestContext);
        var temperature = requestContext.Temperature;

        _logger.LogInformation("[AiGateway] Agent={Agent}, Complexity={Complexity}, Provider={Provider}, Model={Model}, Temperature={Temperature}",
            requestContext.AgentName, requestContext.Complexity, route.Provider, route.Model, temperature?.ToString() ?? "default");

        if (_config.EnableLoadBalancing && route.Provider == "groq" && _usageTracker.IsAboveThreshold("groq", _config.GroqThresholdPerMinute))
        {
            _logger.LogInformation("[AiGateway] Groq above threshold. Pre-emptively routing to GLM fallback model.");
            return await ExecuteWithTrackingAsync(_glmClient, _config.DefaultFallbackModel, messages, tools, temperature, true, ct);
        }

        var primaryClient = route.Provider switch
        {
            "groq" => (IAiProviderClient)_groqClient,
            "glm" => _glmClient,
            "freellmapi" => _freeLlmClient,
            _ => _groqClient
        };

        try
        {
            return await ExecuteWithTrackingAsync(primaryClient, route.Model, messages, tools, temperature, false, ct);
        }
        catch (RateLimitException ex) when (_config.EnableFallback)
        {
            if (ex.ProviderName == "groq")
            {
                _logger.LogWarning("[AiGateway] Groq rate limited. Falling back to GLM {Model}.", _config.DefaultFallbackModel);
                _usageTracker.RecordFallback("groq", "glm");
                return await ExecuteWithTrackingAsync(_glmClient, _config.DefaultFallbackModel, messages, tools, temperature, true, ct);
            }
            else if (ex.ProviderName == "freellmapi")
            {
                var fallbackGroqModel = _config.DefaultFallbackModel;
                _logger.LogWarning("[AiGateway] FreeLLMAPI rate limited. Falling back to Groq {Model}.", fallbackGroqModel);
                _usageTracker.RecordFallback("freellmapi", "groq");
                return await ExecuteWithTrackingAsync(_groqClient, fallbackGroqModel, messages, tools, temperature, true, ct);
            }
            throw;
        }
    }

    public async Task<ChatCompletionResult> SendStreamingAsync(
        AiRequestContext requestContext,
        List<GroqMessageDto> messages,
        List<GroqToolDto>? tools,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken ct = default)
    {
        var route = ResolveRoute(requestContext);
        var temperature = requestContext.Temperature;

        _logger.LogInformation("[AiGateway] Agent={Agent}, Complexity={Complexity}, Provider={Provider}, Model={Model}, Temperature={Temperature} (streaming)",
            requestContext.AgentName, requestContext.Complexity, route.Provider, route.Model, temperature?.ToString() ?? "default");

        if (_config.EnableLoadBalancing && route.Provider == "groq" && _usageTracker.IsAboveThreshold("groq", _config.GroqThresholdPerMinute))
        {
            _logger.LogInformation("[AiGateway] Groq above threshold. Pre-emptively routing to GLM fallback model (streaming).");
            return await ExecuteStreamingWithTrackingAsync(_glmClient, _config.DefaultFallbackModel, messages, tools, onToken, temperature, true, ct);
        }

        var primaryClient = route.Provider switch
        {
            "groq" => (IAiProviderClient)_groqClient,
            "glm" => _glmClient,
            "freellmapi" => _freeLlmClient,
            _ => _groqClient
        };

        try
        {
            return await ExecuteStreamingWithTrackingAsync(primaryClient, route.Model, messages, tools, onToken, temperature, false, ct);
        }
        catch (RateLimitException ex) when (_config.EnableFallback)
        {
            if (ex.ProviderName == "groq")
            {
                _logger.LogWarning("[AiGateway] Groq rate limited. Falling back to GLM {Model} (streaming).", _config.DefaultFallbackModel);
                _usageTracker.RecordFallback("groq", "glm");
                return await ExecuteStreamingWithTrackingAsync(_glmClient, _config.DefaultFallbackModel, messages, tools, onToken, temperature, true, ct);
            }
            else if (ex.ProviderName == "freellmapi")
            {
                var fallbackGroqModel = _config.DefaultFallbackModel;
                _logger.LogWarning("[AiGateway] FreeLLMAPI rate limited. Falling back to Groq {Model} (streaming).", fallbackGroqModel);
                _usageTracker.RecordFallback("freellmapi", "groq");
                return await ExecuteStreamingWithTrackingAsync(_groqClient, fallbackGroqModel, messages, tools, onToken, temperature, true, ct);
            }
            throw;
        }
    }

    private (string Provider, string Model) ResolveRoute(AiRequestContext ctx)
    {
        if (!string.IsNullOrEmpty(ctx.ForceProvider) && !string.IsNullOrEmpty(ctx.ForceModel))
            return (ctx.ForceProvider, ctx.ForceModel);

        var complexityKey = ctx.Complexity.ToString();
        if (_config.ModelRouting.TryGetValue(complexityKey, out var route))
            return (route.Provider, route.Model);

        return ("groq", "qwen3-27b");
    }

    private async Task<ChatCompletionResult> ExecuteWithTrackingAsync(
        IAiProviderClient client, string model,
        List<GroqMessageDto> messages, List<GroqToolDto>? tools,
        double? temperature, bool isFallback, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await client.SendChatCompletionAsync(model, messages, tools, temperature, ct);
        sw.Stop();

        _usageTracker.RecordRequest(client.ProviderName, model, EstimateTokens(messages, result.Content), sw.ElapsedMilliseconds);
        if (isFallback) _usageTracker.RecordFallback("scheduled", client.ProviderName);

        return result;
    }

    private async Task<ChatCompletionResult> ExecuteStreamingWithTrackingAsync(
        IAiProviderClient client, string model,
        List<GroqMessageDto> messages, List<GroqToolDto>? tools,
        Func<string, CancellationToken, Task> onToken,
        double? temperature, bool isFallback, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await client.SendChatCompletionStreamingAsync(model, messages, tools, onToken, temperature, ct);
        sw.Stop();

        _usageTracker.RecordRequest(client.ProviderName, model, EstimateTokens(messages, result.Content), sw.ElapsedMilliseconds);
        if (isFallback) _usageTracker.RecordFallback("scheduled", client.ProviderName);

        return result;
    }

    private static int EstimateTokens(List<GroqMessageDto> messages, string responseContent)
    {
        var totalChars = messages.Sum(m => (m.Content?.Length ?? 0) + (m.Name?.Length ?? 0));
        totalChars += responseContent.Length;
        return totalChars / 4;
    }
}
