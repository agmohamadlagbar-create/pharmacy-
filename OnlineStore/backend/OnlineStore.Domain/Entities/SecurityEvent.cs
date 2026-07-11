namespace OnlineStore.Domain.Entities;

public class SecurityEvent
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string? Details { get; set; }
    public string Severity { get; set; } = "low";
    public DateTime CreatedAtUtc { get; set; }
}
