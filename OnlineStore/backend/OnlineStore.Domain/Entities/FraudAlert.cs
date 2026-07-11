namespace OnlineStore.Domain.Entities;

public class FraudAlert
{
    public int Id { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? OrderIds { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? EvidenceJson { get; set; }
    public string Severity { get; set; } = "medium";
    public string Status { get; set; } = "open";
    public string? ResolvedBy { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}
