namespace OnlineStore.Domain.Entities;

public class AiKnowledgeGap
{
    public int Id { get; set; }
    public string Topic { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? ExampleQuestions { get; set; }
    public int OccurrenceCount { get; set; }
    public string Status { get; set; } = "detected";
    public string? Resolution { get; set; }

    public DateTime FirstDetectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastDetectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
}
