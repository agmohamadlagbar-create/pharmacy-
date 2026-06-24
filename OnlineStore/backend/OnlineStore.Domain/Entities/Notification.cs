namespace OnlineStore.Domain.Entities;

public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
