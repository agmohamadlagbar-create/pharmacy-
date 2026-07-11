namespace OnlineStore.Domain.Entities;

public class BusinessAdvisorMemory
{
    public int Id { get; set; }

    /// <summary>The store owner this memory belongs to.</summary>
    public string StoreUserId { get; set; } = default!;

    /// <summary>"Strategy", "Observation", "Milestone", "Context".</summary>
    public string MemoryType { get; set; } = default!;

    /// <summary>The memory content.</summary>
    public string Content { get; set; } = default!;

    /// <summary>Importance level from 1 (low) to 10 (critical).</summary>
    public int Importance { get; set; } = 5;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Optional expiration for time-limited observations.</summary>
    public DateTime? ExpiresAtUtc { get; set; }
}
