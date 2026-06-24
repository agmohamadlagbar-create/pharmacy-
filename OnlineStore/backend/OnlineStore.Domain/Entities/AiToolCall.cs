namespace OnlineStore.Domain.Entities;

public class AiToolCall
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    /// <summary>
    /// Optional related assistant message that initiated this tool call.
    /// This can be null when tool calls are persisted independently.
    /// </summary>
    public int? MessageId { get; set; }

    /// <summary>Provider tool-call identifier used for idempotency and tracing.</summary>
    public string ToolCallId { get; set; } = default!;

    public string ToolName { get; set; } = default!;

    /// <summary>JSON payload sent to the tool.</summary>
    public string? ArgumentsJson { get; set; }

    /// <summary>JSON payload returned by the tool (or serialized textual result).</summary>
    public string? ResultJson { get; set; }

    /// <summary>Requested, Succeeded, or Failed.</summary>
    public string Status { get; set; } = "Requested";

    public string? ErrorMessage { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    // Navigation
    public AiConversation Conversation { get; set; } = default!;
    public AiMessage? Message { get; set; }
}
