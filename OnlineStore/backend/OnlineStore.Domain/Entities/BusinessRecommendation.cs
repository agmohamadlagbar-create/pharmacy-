namespace OnlineStore.Domain.Entities;

public class BusinessRecommendation
{
    public int Id { get; set; }

    /// <summary>The store owner this recommendation belongs to.</summary>
    public string StoreUserId { get; set; } = default!;

    /// <summary>Optional link to the business goal this recommendation supports.</summary>
    public int? GoalId { get; set; }

    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;

    /// <summary>Why this recommendation was created — data-driven reasoning.</summary>
    public string Reason { get; set; } = default!;

    public RecommendationPriority Priority { get; set; } = RecommendationPriority.Medium;
    public RecommendationStatus Status { get; set; } = RecommendationStatus.Pending;

    /// <summary>How the AI should verify if this recommendation was completed.</summary>
    public string? VerificationCriteria { get; set; }

    /// <summary>The outcome of the last verification attempt.</summary>
    public string? VerificationResult { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    // Navigation
    public BusinessGoal? Goal { get; set; }
}

public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum RecommendationStatus
{
    Pending,
    InProgress,
    Completed,
    Skipped,
    Expired
}
