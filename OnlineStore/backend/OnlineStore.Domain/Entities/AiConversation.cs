namespace OnlineStore.Domain.Entities;

public class AiConversation
{
    public int Id { get; set; }

    /// <summary>The user who owns this conversation.</summary>
    public string UserId { get; set; } = default!;

    /// <summary>Optional friendly title derived from the first message.</summary>
    public string? Title { get; set; }

    /// <summary>The model used for this conversation (for example, llama-3.3-70b-versatile).</summary>
    public string? ModelName { get; set; }

    /// <summary>Optional system prompt snapshot used to initialize this conversation.</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>Updated whenever a new message is persisted.</summary>
    public DateTime? LastMessageAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public AiConversationState State { get; set; } = new();

    // Navigation
    public ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
    public ICollection<AiToolCall> ToolCalls { get; set; } = new List<AiToolCall>();
}
