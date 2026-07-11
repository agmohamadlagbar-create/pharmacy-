namespace OnlineStore.Domain.Entities;

public class BusinessGoal
{
    public int Id { get; set; }

    /// <summary>The store owner who owns this goal.</summary>
    public string StoreUserId { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>The metric being targeted, e.g. "revenue", "orders", "conversion".</summary>
    public string? TargetMetric { get; set; }

    /// <summary>The target value for the metric, e.g. 20 for "increase by 20%".</summary>
    public decimal? TargetValue { get; set; }

    /// <summary>Current tracked progress toward the goal (percentage or absolute).</summary>
    public decimal CurrentProgress { get; set; }

    public GoalStatus Status { get; set; } = GoalStatus.Active;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public enum GoalStatus
{
    Active,
    Achieved,
    Abandoned
}
