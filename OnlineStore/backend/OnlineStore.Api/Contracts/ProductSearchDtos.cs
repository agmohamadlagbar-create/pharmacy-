namespace OnlineStore.Api.Contracts;

public class ProductSearchResultDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public double? DistanceKm { get; set; }
}

public class ProductSearchSummaryDto
{
    public List<ProductSearchResultDto> AllAvailable { get; set; } = new();
    public ProductSearchResultDto? BestPrice { get; set; }
    public ProductSearchResultDto? NearestStore { get; set; }
}
