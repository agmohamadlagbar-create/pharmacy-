using OnlineStore.Domain.Entities;

namespace OnlineStore.Api.Contracts;

// ═══════════════════════════════════════════════════
//  Business Goal DTOs
// ═══════════════════════════════════════════════════

public class CreateGoalRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TargetMetric { get; set; }
    public decimal? TargetValue { get; set; }
}

public class UpdateGoalRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? CurrentProgress { get; set; }
    public GoalStatus? Status { get; set; }
}

public class GoalResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TargetMetric { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal CurrentProgress { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

// ═══════════════════════════════════════════════════
//  Business Recommendation DTOs
// ═══════════════════════════════════════════════════

public class RecommendationResponse
{
    public int Id { get; set; }
    public int? GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? VerificationCriteria { get; set; }
    public string? VerificationResult { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class UpdateRecommendationStatusRequest
{
    public RecommendationStatus Status { get; set; }
    public string? Result { get; set; }
}

// ═══════════════════════════════════════════════════
//  Health Score DTOs
// ═══════════════════════════════════════════════════

public class HealthScoreResponse
{
    public int Score { get; set; }
    public HealthScoreBreakdown Breakdown { get; set; } = new();
    public DateTime CalculatedAtUtc { get; set; }
}

public class HealthScoreBreakdown
{
    public int RevenueTrend { get; set; }
    public int OrderGrowth { get; set; }
    public int InventoryHealth { get; set; }
    public int ProductQuality { get; set; }
    public int ReviewScore { get; set; }
    public int CategoryCompleteness { get; set; }
    public int SeoCompleteness { get; set; }
    public int ImageCompleteness { get; set; }
}

// ═══════════════════════════════════════════════════
//  Report DTOs
// ═══════════════════════════════════════════════════

public class ReportResponse
{
    public int Id { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string ReportContent { get; set; } = string.Empty;
    public int? HealthScore { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class GenerateReportRequest
{
    public string ReportType { get; set; } = "Daily"; // Daily, Weekly, Monthly
}

// ═══════════════════════════════════════════════════
//  Store Inspection DTOs
// ═══════════════════════════════════════════════════

public class StoreInspectionResult
{
    // Store info
    public string ShopName { get; set; } = string.Empty;
    public string StoreType { get; set; } = string.Empty;

    // Product metrics
    public int TotalProducts { get; set; }
    public int ProductsWithoutDescription { get; set; }
    public int ProductsWithShortDescription { get; set; }
    public int ProductsWithOneImage { get; set; }
    public int ProductsWithNoImages { get; set; }
    public int ProductsWithNoCategory { get; set; }
    public int ProductsWithNoSales { get; set; }
    public List<string> TopProductNames { get; set; } = new();

    // Order metrics
    public int TotalOrders30Days { get; set; }
    public int TotalOrdersPrevious30Days { get; set; }
    public decimal Revenue30Days { get; set; }
    public decimal RevenuePrevious30Days { get; set; }
    public decimal RevenueGrowthPercent { get; set; }
    public decimal OrderGrowthPercent { get; set; }

    // Review metrics
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }

    // Inventory metrics
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }

    // Category coverage
    public int CategoriesUsed { get; set; }
    public int TotalCategories { get; set; }

    public DateTime InspectedAtUtc { get; set; } = DateTime.UtcNow;
}
