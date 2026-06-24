namespace OnlineStore.Domain.Entities;

public class AdBanner
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? LinkUrl { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
