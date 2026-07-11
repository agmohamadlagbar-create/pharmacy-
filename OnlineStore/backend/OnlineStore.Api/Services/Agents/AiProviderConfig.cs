namespace OnlineStore.Api.Services.Agents;

public class AiProviderConfig
{
    public int GroqThresholdPerMinute { get; set; } = 25;
    public bool EnableLoadBalancing { get; set; } = true;
    public bool EnableFallback { get; set; } = true;
    public string DefaultFallbackModel { get; set; } = "glm-5.2";
    public Dictionary<string, ModelRoute> ModelRouting { get; set; } = new();
}

public class ModelRoute
{
    public string Provider { get; set; } = "groq";
    public string Model { get; set; } = "qwen3-27b";
}
