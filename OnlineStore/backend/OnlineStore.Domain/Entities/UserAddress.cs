namespace OnlineStore.Domain.Entities;

public class UserAddress
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    
    /// <summary>e.g., Home, Work, Office</summary>
    public string Title { get; set; } = "Home";
    
    public string? Street { get; set; }
    public string? BuildingNumber { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    public bool IsDefault { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
