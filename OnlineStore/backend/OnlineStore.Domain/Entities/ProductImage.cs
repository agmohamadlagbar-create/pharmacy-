namespace OnlineStore.Domain.Entities;

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Country { get; set; } = default!;
    public string City { get; set; } = default!;
    public string StorageKey { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string ImageType { get; set; } = "original"; // original, thumb, medium, large
    public string Status { get; set; } = "active";       // active, archived, deleted
    public int SortOrder { get; set; }
    public string UploadedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    // Navigation
    public Product Product { get; set; } = default!;
}
