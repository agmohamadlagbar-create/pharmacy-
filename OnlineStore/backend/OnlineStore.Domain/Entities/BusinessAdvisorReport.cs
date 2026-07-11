namespace OnlineStore.Domain.Entities;

public class BusinessAdvisorReport
{
    public int Id { get; set; }

    /// <summary>The store owner this report belongs to.</summary>
    public string StoreUserId { get; set; } = default!;

    /// <summary>"Daily", "Weekly", or "Monthly".</summary>
    public string ReportType { get; set; } = default!;

    /// <summary>The generated report text (coaching-style message).</summary>
    public string ReportContent { get; set; } = default!;

    /// <summary>Business health score at the time of this report (0-100).</summary>
    public int? HealthScore { get; set; }

    /// <summary>Serialized analytics snapshot for historical comparison.</summary>
    public string? ReportDataJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
