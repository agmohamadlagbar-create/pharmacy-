namespace OnlineStore.Domain.Entities;

public class DeliveryProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;
    public string? Phone { get; set; }
    public string? VehicleInfo { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
