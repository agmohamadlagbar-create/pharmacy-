namespace OnlineStore.Domain.Entities;

public class ModerationFlag
{
    public int Id { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int? ContentId { get; set; }
    public string FlagType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? EvidenceJson { get; set; }
    public string Severity { get; set; } = "medium";
    public string Status { get; set; } = "open";
    public string? ResolvedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}
