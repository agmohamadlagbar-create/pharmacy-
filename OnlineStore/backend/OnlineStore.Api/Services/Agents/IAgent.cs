namespace OnlineStore.Api.Services.Agents;

/// <summary>
/// Core interface for all specialized AI agents in the multi-agent ecosystem.
/// Each agent has a single responsibility and its own set of authorized tools.
/// </summary>
public interface IAgent
{
    /// <summary>Unique identifier for routing (e.g., "planner", "shopping", "customer_service").</summary>
    string AgentName { get; }

    /// <summary>The specialized system prompt that defines this agent's behavior and boundaries.</summary>
    string SystemPrompt { get; }

    /// <summary>Default task complexity for model routing decisions.</summary>
    TaskComplexity DefaultComplexity { get; }

    /// <summary>
    /// Process a user message within this agent's domain of expertise.
    /// Returns the agent's response text.
    /// </summary>
    Task<AgentResult> ProcessAsync(AgentContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a user message with streaming support.
    /// Tokens are emitted via the onToken callback as they arrive.
    /// Returns the complete response text.
    /// </summary>
    Task<AgentResult> ProcessStreamingAsync(
        AgentContext context,
        Func<string, CancellationToken, Task> onToken,
        CancellationToken cancellationToken = default);
}
