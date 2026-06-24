namespace OnlineStore.Domain.Entities;

public class UserImage
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Country { get; set; } = default!;
    public string City { get; set; } = default!;
    public string StorageKey { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string ImageType { get; set; } = "original"; // original, avatar
    public string Status { get; set; } = "active";       // active, archived, deleted
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ArchivedAt { get; set; }
}
