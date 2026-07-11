namespace OnlineStore.Domain.Entities;

public class AiFeedback
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int? MessageId { get; set; }
    public string UserId { get; set; } = default!;

    public int? Rating { get; set; }
    public string? FeedbackType { get; set; }
    public string? Comment { get; set; }

    public string? ImplicitSignal { get; set; }
    public string? AgentName { get; set; }
    public string FeedbackSource { get; set; } = "explicit";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public AiConversation Conversation { get; set; } = default!;
    public AiMessage? Message { get; set; }
}
