namespace OnlineStore.Api.Contracts;

// ════════════════════════════════════════════
//  Analytics — Dashboard
// ════════════════════════════════════════════

/// <summary>Section 1: Summary cards.</summary>
public record AnalyticsSummaryDto(
    decimal TotalRevenue,
    decimal AverageDailyRevenue,
    decimal AverageMonthlyRevenue,
    decimal PeakDayRevenue,
    int TotalOrders,
    decimal AverageOrderValue,
    string CurrencySymbol
);

/// <summary>Section 2: Daily sales data point.</summary>
public record DailySalesPointDto(string Date, decimal Revenue, int OrderCount);

/// <summary>Section 2: Monthly sales data point.</summary>
public record MonthlySalesPointDto(string Month, decimal Revenue, int OrderCount);

/// <summary>Section 3: Top selling product.</summary>
public record TopSellingProductDto(
    int ProductId,
    string ProductName,
    string? ImageUrl,
    int QuantitySold,
    decimal Revenue
);

/// <summary>Section 4: Product preview.</summary>
public record ProductPreviewDto(
    int ProductId,
    string ProductName,
    string? ImageUrl,
    int CurrentStock,
    decimal Price,
    int TotalSoldCount
);

/// <summary>Section 5: Growth percentages.</summary>
public record GrowthDto(
    decimal RevenueGrowthPercent,
    decimal OrderGrowthPercent,
    decimal PreviousMonthRevenue,
    decimal CurrentMonthRevenue,
    int PreviousMonthOrders,
    int CurrentMonthOrders
);

/// <summary>Section 6: Trending product.</summary>
public record TrendingProductDto(
    int ProductId,
    string ProductName,
    string? ImageUrl,
    decimal GrowthPercent,
    int QuantityIncrease,
    int RecentQuantity,
    int PreviousQuantity
);

/// <summary>Section 7: Returning customers.</summary>
public record ReturningCustomersDto(
    int TotalCustomers,
    int ReturningCustomers,
    decimal ReturningPercentage
);

/// <summary>Section 8: Product rating info.</summary>
public record RatedProductDto(
    int ProductId,
    string ProductName,
    string? ImageUrl,
    double AverageRating,
    int ReviewCount
);

/// <summary>Section 8: Overall rating statistics.</summary>
public record ProductRatingStatsDto(
    double OverallAverageRating,
    int TotalReviews,
    List<RatedProductDto> HighestRated,
    List<RatedProductDto> LowestRated
);

/// <summary>Section 9: Low stock product.</summary>
public record LowStockProductDto(
    int ProductId,
    string ProductName,
    string? ImageUrl,
    int RemainingQuantity,
    int Threshold
);

/// <summary>Section 10: AI recommendation.</summary>
public record AiRecommendationDto(
    string Type,      // "stock", "sales_up", "sales_down", "rating_high", "rating_low", "low_stock"
    string Message,
    string? ProductName,
    decimal? Value
);

/// <summary>Full analytics dashboard response.</summary>
public record FullAnalyticsDto(
    AnalyticsSummaryDto Summary,
    List<DailySalesPointDto> DailySales,
    List<MonthlySalesPointDto> MonthlySales,
    List<TopSellingProductDto> TopSellingProducts,
    List<ProductPreviewDto> ProductPreviews,
    GrowthDto Growth,
    List<TrendingProductDto> TrendingProducts,
    ReturningCustomersDto ReturningCustomers,
    ProductRatingStatsDto? RatingStats,
    List<LowStockProductDto> LowStockProducts,
    List<AiRecommendationDto> AiRecommendations,
    int LowStockThreshold
);

// ════════════════════════════════════════════
//  Analytics — Reports
// ════════════════════════════════════════════

public record MonthlyReportDto(
    int Year,
    int Month,
    string MonthName,
    string CurrencySymbol,
    decimal TotalRevenue,
    int TotalOrders,
    decimal AverageOrderValue,
    decimal RevenueGrowthPercent,
    decimal OrderGrowthPercent,
    List<TopSellingProductDto> TopProducts,
    List<DailySalesPointDto> DailySales
);

public record YearlyReportDto(
    int Year,
    string CurrencySymbol,
    decimal TotalRevenue,
    int TotalOrders,
    decimal AverageOrderValue,
    List<MonthlySalesPointDto> MonthlyBreakdown,
    List<TopSellingProductDto> TopProducts,
    List<GrowthTrendDto> GrowthTrends
);

public record GrowthTrendDto(string Month, decimal RevenueGrowthPercent, decimal OrderGrowthPercent);

// ════════════════════════════════════════════
//  Analytics — Settings
// ════════════════════════════════════════════

public record UpdateLowStockThresholdRequest(int Threshold);

public class ProductSalesDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}
