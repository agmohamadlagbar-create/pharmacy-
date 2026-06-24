namespace OnlineStore.Domain.Entities;

public class AiMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    /// <summary>The user who owns this message.</summary>
    public string UserId { get; set; } = default!;

    /// <summary>Monotonic sequence number inside a conversation.</summary>
    public int SequenceNumber { get; set; }

    /// <summary>"user", "assistant", "system", or "tool"</summary>
    public string Role { get; set; } = default!;

    public string Content { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public AiConversation Conversation { get; set; } = default!;
    public ICollection<AiToolCall> ToolCalls { get; set; } = new List<AiToolCall>();
}
