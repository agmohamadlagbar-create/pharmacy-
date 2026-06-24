namespace OnlineStore.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    /// <summary>The Store user who owns this product.</summary>
    public string StoreUserId { get; set; } = default!;

    public int CategoryId { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> AdditionalImageUrls { get; set; } = new();

    public int StockQuantity { get; set; }
    public int OriginalQuantity { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Pending;
    public string? RejectionReason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Category Category { get; set; } = default!;
}
