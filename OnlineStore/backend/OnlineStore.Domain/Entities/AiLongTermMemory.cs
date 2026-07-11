namespace OnlineStore.Domain.Entities;

/// <summary>
/// Cross-conversation memory — persists key facts across sessions.
/// Examples: "User is buying for a wedding", "User prefers organic products", "User runs a bakery".
/// Short-term memories (type="context") expire after 30 days.
/// Long-term memories (type="fact","relationship") never expire unless inactive.
/// </summary>
public class AiLongTermMemory
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;

    /// <summary>"fact" (never expires), "context" (30-day TTL), "relationship" (never expires)</summary>
    public string MemoryType { get; set; } = default!;

    /// <summary>The actual memory text, e.g. "User mentioned they are preparing for a wedding in August".</summary>
    public string Content { get; set; } = default!;

    /// <summary>Brief context of where this memory was extracted from.</summary>
    public string? SourceConversationSummary { get; set; }

    public int SourceConversationId { get; set; }

    /// <summary>0.0–1.0, higher = more likely to be recalled.</summary>
    public double Importance { get; set; } = 0.5;

    /// <summary>Incremented each time this memory is included in a context. Used for LRU eviction.</summary>
    public int AccessCount { get; set; }

    public DateTime LastAccessedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Null for "fact"/"relationship" types. Set to CreatedAtUtc+30d for "context" type.</summary>
    public DateTime? ExpiresAtUtc { get; set; }
}
