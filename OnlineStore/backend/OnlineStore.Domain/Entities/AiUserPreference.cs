namespace OnlineStore.Domain.Entities;

/// <summary>
/// Stores per-user preferences learned from conversations.
/// Examples: preferred language, product categories, price sensitivity, communication style.
/// Long-term preferences never expire unless the user is inactive for an extended period.
/// </summary>
public class AiUserPreference
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;

    /// <summary>e.g. "preferred_language", "price_range", "favorite_categories", "communication_style"</summary>
    public string PreferenceKey { get; set; } = default!;

    /// <summary>e.g. "ar", "0-50", "electronics,clothing", "concise"</summary>
    public string PreferenceValue { get; set; } = default!;

    /// <summary>"inferred" (auto-detected) or "explicit" (user stated directly)</summary>
    public string Source { get; set; } = "inferred";

    /// <summary>0.0–1.0, increases as pattern repeats across conversations.</summary>
    public double Confidence { get; set; } = 0.5;

    /// <summary>Number of times this preference was observed.</summary>
    public int OccurrenceCount { get; set; } = 1;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
