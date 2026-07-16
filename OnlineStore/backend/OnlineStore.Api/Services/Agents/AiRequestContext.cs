namespace OnlineStore.Api.Services.Agents;

public class AiRequestContext
{
    public string AgentName { get; init; } = "";
    public TaskComplexity Complexity { get; init; } = TaskComplexity.Medium;
    public string? ForceProvider { get; init; }
    public string? ForceModel { get; init; }
    public double? Temperature { get; init; }
}

public enum TaskComplexity
{
    Simple,
    Medium,
    Complex,
    Background
}
