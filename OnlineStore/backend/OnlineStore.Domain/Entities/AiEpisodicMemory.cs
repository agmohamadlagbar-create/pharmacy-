namespace OnlineStore.Domain.Entities;

/// <summary>
/// Records important past interactions that the AI should recall.
/// Auto-detected by the system (complaints, large purchases, refunds, special requests).
/// </summary>
public class AiEpisodicMemory
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public int ConversationId { get; set; }

    /// <summary>"complaint_resolved", "large_purchase", "refund_processed", "special_request", "escalated_to_human"</summary>
    public string EpisodeType { get; set; } = default!;

    /// <summary>Auto-generated summary of what happened in this episode.</summary>
    public string Summary { get; set; } = default!;

    /// <summary>"resolved", "pending", "escalated"</summary>
    public string? Outcome { get; set; }

    /// <summary>"positive", "neutral", "negative"</summary>
    public string? SentimentAtEnd { get; set; }

    /// <summary>JSON: product IDs, order IDs, amounts involved.</summary>
    public string? KeyEntitiesJson { get; set; }

    /// <summary>0.0–1.0, higher = more important to recall.</summary>
    public double Importance { get; set; } = 0.5;

    public DateTime EpisodeDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public AiConversation Conversation { get; set; } = default!;
}
