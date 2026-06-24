namespace OnlineStore.Api.Contracts;

public class StoreSelectionRequestDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public double CustomerLatitude { get; set; }
    public double CustomerLongitude { get; set; }
}

public class StoreOptionDto
{
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string StoreType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }
    public double DistanceKm { get; set; }

    /// <summary>Lower is better. Combines distance, price, and stock into one ranking metric.</summary>
    public double Score { get; set; }
}

public class StoreSelectionResultDto
{
    public StoreOptionDto? BestOverall { get; set; }
    public StoreOptionDto? CheapestOption { get; set; }
    public StoreOptionDto? NearestOption { get; set; }
    public List<StoreOptionDto> AllOptions { get; set; } = new();
    public int TotalStoresFound { get; set; }
}
