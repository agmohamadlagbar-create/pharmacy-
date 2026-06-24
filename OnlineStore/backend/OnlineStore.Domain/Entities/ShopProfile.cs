namespace OnlineStore.Domain.Entities;

public class ShopProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = default!;
    public string ShopName { get; set; } = default!;
    public string StoreType { get; set; } = default!; // Restaurant, Market, Pharmacy, Other
    public string RegionCode { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string? OwnerName { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public string? OpeningTime { get; set; }
    public string? ClosingTime { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int LowStockThreshold { get; set; } = 10;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
