using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents;

/// <summary>
/// Shared context passed between agents during orchestration.
/// Contains the user's message, identity, conversation state, and accumulated results.
/// </summary>
public class AgentContext
{
    /// <summary>The user's original message.</summary>
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>The authenticated user's ID.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>The active conversation ID.</summary>
    public int ConversationId { get; set; }

    /// <summary>The user's role: "Customer", "Shop", "Admin".</summary>
    public string UserRole { get; set; } = "Customer";

    /// <summary>Conversation history loaded from the database (Groq-compatible DTOs).</summary>
    public List<GroqMessageDto> ConversationHistory { get; set; } = new();

    /// <summary>Results accumulated from previous agents in the orchestration chain.</summary>
    public List<AgentResult> PreviousResults { get; set; } = new();

    /// <summary>The intent classified by the Planner agent.</summary>
    public string? ClassifiedIntent { get; set; }

    /// <summary>Optional metadata from the Planner's task decomposition.</summary>
    public List<string> PlannedTasks { get; set; } = new();

    /// <summary>Optional A/B testing prompt variant override.</summary>
    public string? SystemPromptVariant { get; set; }
}

/// <summary>
/// The result produced by an agent after processing a request.
/// </summary>
public class AgentResult
{
    /// <summary>The name of the agent that produced this result.</summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>The text response produced by the agent.</summary>
    public string ResponseText { get; set; } = string.Empty;

    /// <summary>Whether this result should be passed through the Reflection agent.</summary>
    public bool RequiresReflection { get; set; }

    /// <summary>Whether the agent successfully handled the request.</summary>
    public bool Success { get; set; } = true;

    /// <summary>Optional structured data (e.g., recommendation JSON, report data).</summary>
    public string? StructuredData { get; set; }

    /// <summary>The type of structured data, if any (e.g., "recommendations", "report").</summary>
    public string? StructuredDataType { get; set; }
}
