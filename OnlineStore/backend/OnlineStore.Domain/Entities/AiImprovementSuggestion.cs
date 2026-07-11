namespace OnlineStore.Domain.Entities;

public class AiImprovementSuggestion
{
    public int Id { get; set; }
    public string Category { get; set; } = default!;
    public string AgentName { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? SuggestedChange { get; set; }
    public string? EvidenceJson { get; set; }

    public string Status { get; set; } = "pending";
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }

    public int Priority { get; set; } = 3;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}
