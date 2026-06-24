namespace OnlineStore.Api.Contracts;

public class RecommendedProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string StoreName { get; set; } = string.Empty;

    /// <summary>Why this product is recommended.</summary>
    public string Reason { get; set; } = string.Empty;
}

public class RecommendationResultDto
{
    public List<RecommendedProductDto> FrequentlyBoughtTogether { get; set; } = new();
    public List<RecommendedProductDto> RelatedByCategory { get; set; } = new();
    public List<RecommendedProductDto> SmartUpsells { get; set; } = new();
    public List<RecommendedProductDto> BasedOnHistory { get; set; } = new();
}
