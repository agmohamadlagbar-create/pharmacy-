namespace OnlineStore.Domain.Entities;

public class DeliveryJob
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    /// <summary>Null until a driver accepts the job.</summary>
    public string? DeliveryUserId { get; set; }

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Unassigned;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }

    // Navigation
    public Order Order { get; set; } = default!;
}
