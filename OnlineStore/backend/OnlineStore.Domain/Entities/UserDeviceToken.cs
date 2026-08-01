namespace OnlineStore.Domain.Entities;

public class UserDeviceToken
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;
    public string DeviceToken { get; set; } = default!;
    public string Platform { get; set; } = "Web"; // "Web", "Android", "iOS"
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedAtUtc { get; set; } = DateTime.UtcNow;
}
