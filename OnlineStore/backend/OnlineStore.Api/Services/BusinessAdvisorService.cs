using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

/// <summary>
/// Core service for the BusinessAdvisorAgent — manages goals, recommendations,
/// reports, memory, and provides store inspection/health score computation.
/// Delegates to existing services for data without duplicating their logic.
/// </summary>
public class BusinessAdvisorService : IBusinessAdvisorService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BusinessAdvisorService> _logger;

    public BusinessAdvisorService(
        ApplicationDbContext db,
        ILogger<BusinessAdvisorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════
    //  Business Goals
    // ═══════════════════════════════════════════════════

    public async Task<BusinessGoal> CreateGoalAsync(
        string storeUserId, string title, string? description,
        string? targetMetric, decimal? targetValue)
    {
        var goal = new BusinessGoal
        {
            StoreUserId = storeUserId,
            Title = title,
            Description = description,
            TargetMetric = targetMetric,
            TargetValue = targetValue,
            Status = GoalStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.BusinessGoals.Add(goal);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[BusinessAdvisor] Created goal '{Title}' for store {StoreUserId}", title, storeUserId);
        return goal;
    }

    public async Task<List<BusinessGoal>> GetGoalsAsync(string storeUserId)
    {
        return await _db.BusinessGoals
            .Where(g => g.StoreUserId == storeUserId)
            .OrderByDescending(g => g.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<BusinessGoal?> GetGoalAsync(int goalId)
    {
        return await _db.BusinessGoals.FindAsync(goalId);
    }

    public async Task UpdateGoalAsync(int goalId, string? title, string? description, decimal? currentProgress, GoalStatus? status)
    {
        var goal = await _db.BusinessGoals.FindAsync(goalId);
        if (goal == null) return;

        _db.Attach(goal);

        if (title != null) goal.Title = title;
        if (description != null) goal.Description = description;
        if (currentProgress.HasValue) goal.CurrentProgress = currentProgress.Value;
        if (status.HasValue) goal.Status = status.Value;
        goal.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════
    //  Recommendations
    // ═══════════════════════════════════════════════════

    public async Task<List<BusinessRecommendation>> GetActiveRecommendationsAsync(string storeUserId)
    {
        return await _db.BusinessRecommendations
            .Where(r => r.StoreUserId == storeUserId &&
                        (r.Status == RecommendationStatus.Pending || r.Status == RecommendationStatus.InProgress))
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<BusinessRecommendation> CreateRecommendationAsync(
        string storeUserId, int? goalId, string title, string description,
        string reason, RecommendationPriority priority, string? verificationCriteria, DateTime? dueDate)
    {
        var recommendation = new BusinessRecommendation
        {
            StoreUserId = storeUserId,
            GoalId = goalId,
            Title = title,
            Description = description,
            Reason = reason,
            Priority = priority,
            Status = RecommendationStatus.Pending,
            VerificationCriteria = verificationCriteria,
            DueDate = dueDate,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.BusinessRecommendations.Add(recommendation);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[BusinessAdvisor] Created recommendation '{Title}' for store {StoreUserId}", title, storeUserId);
        return recommendation;
    }

    public async Task UpdateRecommendationStatusAsync(int id, RecommendationStatus status, string? verificationResult)
    {
        var rec = await _db.BusinessRecommendations.FindAsync(id);
        if (rec == null) return;

        _db.Attach(rec);
        rec.Status = status;
        rec.VerificationResult = verificationResult;
        if (status == RecommendationStatus.Completed)
            rec.CompletedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task<List<BusinessRecommendation>> GetRecommendationHistoryAsync(string storeUserId, int count = 20)
    {
        return await _db.BusinessRecommendations
            .Where(r => r.StoreUserId == storeUserId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(count)
            .ToListAsync();
    }

    // ═══════════════════════════════════════════════════
    //  Reports
    // ═══════════════════════════════════════════════════

    public async Task<BusinessAdvisorReport> SaveReportAsync(
        string storeUserId, string reportType, string content, int? healthScore, string? dataJson)
    {
        var report = new BusinessAdvisorReport
        {
            StoreUserId = storeUserId,
            ReportType = reportType,
            ReportContent = content,
            HealthScore = healthScore,
            ReportDataJson = dataJson,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.BusinessAdvisorReports.Add(report);
        await _db.SaveChangesAsync();
        return report;
    }

    public async Task<List<BusinessAdvisorReport>> GetRecentReportsAsync(string storeUserId, int count = 10)
    {
        return await _db.BusinessAdvisorReports
            .Where(r => r.StoreUserId == storeUserId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(count)
            .ToListAsync();
    }

    public async Task<BusinessAdvisorReport?> GetLatestReportAsync(string storeUserId)
    {
        return await _db.BusinessAdvisorReports
            .Where(r => r.StoreUserId == storeUserId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync();
    }

    // ═══════════════════════════════════════════════════
    //  Memory
    // ═══════════════════════════════════════════════════

    public async Task SaveMemoryAsync(string storeUserId, string memoryType, string content, int importance)
    {
        var memory = new BusinessAdvisorMemory
        {
            StoreUserId = storeUserId,
            MemoryType = memoryType,
            Content = content,
            Importance = Math.Clamp(importance, 1, 10),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.BusinessAdvisorMemories.Add(memory);
        await _db.SaveChangesAsync();
    }

    public async Task<List<BusinessAdvisorMemory>> GetMemoriesAsync(string storeUserId, int count = 30)
    {
        return await _db.BusinessAdvisorMemories
            .Where(m => m.StoreUserId == storeUserId &&
                        (m.ExpiresAtUtc == null || m.ExpiresAtUtc > DateTime.UtcNow))
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreatedAtUtc)
            .Take(count)
            .ToListAsync();
    }

    // ═══════════════════════════════════════════════════
    //  Store Inspection
    // ═══════════════════════════════════════════════════

    public async Task<StoreInspectionResult> InspectStoreAsync(string storeUserId)
    {
        var result = new StoreInspectionResult();

        // Store info
        var shop = await _db.ShopProfiles.FirstOrDefaultAsync(s => s.UserId == storeUserId);
        if (shop != null)
        {
            result.ShopName = shop.ShopName;
            result.StoreType = shop.StoreType;
        }

        // Products analysis
        var products = await _db.Products
            .Where(p => p.StoreUserId == storeUserId && p.Status == ProductStatus.Approved)
            .Select(p => new
            {
                p.Id,
                p.Name,
                DescriptionLength = p.Description != null ? p.Description.Length : 0,
                p.ImageUrl,
                AdditionalImageCount = p.AdditionalImageUrls.Count,
                p.CategoryId,
                p.StockQuantity,
                p.Price
            })
            .ToListAsync();

        result.TotalProducts = products.Count;
        result.ProductsWithoutDescription = products.Count(p => p.DescriptionLength == 0);
        result.ProductsWithShortDescription = products.Count(p => p.DescriptionLength > 0 && p.DescriptionLength < 50);
        result.ProductsWithNoImages = products.Count(p => string.IsNullOrEmpty(p.ImageUrl) && p.AdditionalImageCount == 0);
        result.ProductsWithOneImage = products.Count(p => !string.IsNullOrEmpty(p.ImageUrl) && p.AdditionalImageCount == 0);
        result.ProductsWithNoCategory = products.Count(p => p.CategoryId == 0);
        result.OutOfStockProducts = products.Count(p => p.StockQuantity <= 0);
        result.LowStockProducts = products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= (shop?.LowStockThreshold ?? 10));

        // Orders & Revenue — last 30 days vs previous 30 days
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sixtyDaysAgo = now.AddDays(-60);

        var recentOrders = await _db.Orders
            .Where(o => o.StoreUserId == storeUserId && o.CreatedAtUtc >= thirtyDaysAgo)
            .ToListAsync();

        var previousOrders = await _db.Orders
            .Where(o => o.StoreUserId == storeUserId &&
                        o.CreatedAtUtc >= sixtyDaysAgo && o.CreatedAtUtc < thirtyDaysAgo)
            .ToListAsync();

        result.TotalOrders30Days = recentOrders.Count;
        result.TotalOrdersPrevious30Days = previousOrders.Count;
        result.Revenue30Days = recentOrders.Sum(o => o.TotalAmount);
        result.RevenuePrevious30Days = previousOrders.Sum(o => o.TotalAmount);

        result.RevenueGrowthPercent = result.RevenuePrevious30Days > 0
            ? Math.Round((result.Revenue30Days - result.RevenuePrevious30Days) / result.RevenuePrevious30Days * 100, 1)
            : (result.Revenue30Days > 0 ? 100 : 0);

        result.OrderGrowthPercent = result.TotalOrdersPrevious30Days > 0
            ? Math.Round((decimal)(result.TotalOrders30Days - result.TotalOrdersPrevious30Days) / result.TotalOrdersPrevious30Days * 100, 1)
            : (result.TotalOrders30Days > 0 ? 100 : 0);

        // Products with no sales
        var productIdsWithSales = await _db.OrderItems
            .Where(oi => oi.Order.StoreUserId == storeUserId && oi.Order.CreatedAtUtc >= thirtyDaysAgo)
            .Select(oi => oi.ProductId)
            .Distinct()
            .ToListAsync();

        result.ProductsWithNoSales = products.Count(p => !productIdsWithSales.Contains(p.Id));

        // Top selling product names
        var topProductIds = await _db.OrderItems
            .Where(oi => oi.Order.StoreUserId == storeUserId && oi.Order.CreatedAtUtc >= thirtyDaysAgo)
            .GroupBy(oi => oi.ProductId)
            .OrderByDescending(g => g.Sum(oi => oi.Quantity))
            .Take(5)
            .Select(g => g.Key)
            .ToListAsync();

        result.TopProductNames = products
            .Where(p => topProductIds.Contains(p.Id))
            .Select(p => p.Name)
            .ToList();

        // Reviews
        var storeProductIds = products.Select(p => p.Id).ToList();
        var reviews = await _db.Reviews
            .Where(r => storeProductIds.Contains(r.ProductId))
            .ToListAsync();

        result.TotalReviews = reviews.Count;
        result.AverageRating = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;

        // Category coverage
        result.CategoriesUsed = products.Select(p => p.CategoryId).Distinct().Count();
        result.TotalCategories = await _db.Categories.CountAsync();

        result.InspectedAtUtc = DateTime.UtcNow;
        return result;
    }

    public async Task<HealthScoreResponse> CalculateHealthScoreAsync(string storeUserId)
    {
        var inspection = await InspectStoreAsync(storeUserId);
        var breakdown = new HealthScoreBreakdown();

        // Revenue Trend (15 points max)
        breakdown.RevenueTrend = inspection.RevenueGrowthPercent switch
        {
            > 20 => 15,
            > 10 => 13,
            > 0 => 10,
            0 => 7,
            > -10 => 5,
            > -20 => 3,
            _ => 0
        };

        // Order Growth (15 points max)
        breakdown.OrderGrowth = inspection.OrderGrowthPercent switch
        {
            > 20 => 15,
            > 10 => 13,
            > 0 => 10,
            0 => 7,
            > -10 => 5,
            > -20 => 3,
            _ => 0
        };

        // Inventory Health (15 points max)
        if (inspection.TotalProducts > 0)
        {
            var stockIssueRatio = (double)(inspection.OutOfStockProducts + inspection.LowStockProducts) / inspection.TotalProducts;
            breakdown.InventoryHealth = stockIssueRatio switch
            {
                < 0.05 => 15,
                < 0.1 => 12,
                < 0.2 => 9,
                < 0.4 => 6,
                _ => 3
            };
        }
        else breakdown.InventoryHealth = 0;

        // Product Quality — descriptions (15 points max)
        if (inspection.TotalProducts > 0)
        {
            var goodDescRatio = (double)(inspection.TotalProducts - inspection.ProductsWithoutDescription - inspection.ProductsWithShortDescription) / inspection.TotalProducts;
            breakdown.ProductQuality = goodDescRatio switch
            {
                > 0.9 => 15,
                > 0.7 => 12,
                > 0.5 => 9,
                > 0.3 => 6,
                _ => 3
            };
        }
        else breakdown.ProductQuality = 0;

        // Review Score (10 points max)
        breakdown.ReviewScore = inspection.AverageRating switch
        {
            >= 4.5 => 10,
            >= 4.0 => 8,
            >= 3.5 => 6,
            >= 3.0 => 4,
            >= 2.0 => 2,
            _ => inspection.TotalReviews == 0 ? 5 : 0 // No reviews = neutral, bad reviews = low
        };

        // Category Completeness (10 points max)
        if (inspection.TotalProducts > 0)
        {
            var catRatio = inspection.TotalCategories > 0
                ? (double)inspection.CategoriesUsed / Math.Min(inspection.TotalCategories, inspection.TotalProducts)
                : 0;
            breakdown.CategoryCompleteness = catRatio switch
            {
                > 0.5 => 10,
                > 0.3 => 7,
                > 0.1 => 5,
                _ => 2
            };
        }
        else breakdown.CategoryCompleteness = 0;

        // SEO Completeness — products with description (10 points max)
        if (inspection.TotalProducts > 0)
        {
            var seoRatio = (double)(inspection.TotalProducts - inspection.ProductsWithoutDescription) / inspection.TotalProducts;
            breakdown.SeoCompleteness = seoRatio switch
            {
                > 0.9 => 10,
                > 0.7 => 8,
                > 0.5 => 6,
                > 0.3 => 4,
                _ => 2
            };
        }
        else breakdown.SeoCompleteness = 0;

        // Image Completeness (10 points max)
        if (inspection.TotalProducts > 0)
        {
            var goodImageRatio = (double)(inspection.TotalProducts - inspection.ProductsWithNoImages - inspection.ProductsWithOneImage) / inspection.TotalProducts;
            breakdown.ImageCompleteness = goodImageRatio switch
            {
                > 0.8 => 10,
                > 0.6 => 8,
                > 0.4 => 6,
                > 0.2 => 4,
                _ => 2
            };
        }
        else breakdown.ImageCompleteness = 0;

        var totalScore = breakdown.RevenueTrend + breakdown.OrderGrowth +
                         breakdown.InventoryHealth + breakdown.ProductQuality +
                         breakdown.ReviewScore + breakdown.CategoryCompleteness +
                         breakdown.SeoCompleteness + breakdown.ImageCompleteness;

        return new HealthScoreResponse
        {
            Score = Math.Clamp(totalScore, 0, 100),
            Breakdown = breakdown,
            CalculatedAtUtc = DateTime.UtcNow
        };
    }

    // ═══════════════════════════════════════════════════
    //  Active Advisor Stores
    // ═══════════════════════════════════════════════════

    public async Task<List<string>> GetStoresWithActiveAdvisorAsync()
    {
        // Stores that have at least one active business goal OR at least one pending recommendation
        var storesWithGoals = await _db.BusinessGoals
            .Where(g => g.Status == GoalStatus.Active)
            .Select(g => g.StoreUserId)
            .Distinct()
            .ToListAsync();

        var storesWithRecs = await _db.BusinessRecommendations
            .Where(r => r.Status == RecommendationStatus.Pending || r.Status == RecommendationStatus.InProgress)
            .Select(r => r.StoreUserId)
            .Distinct()
            .ToListAsync();

        return storesWithGoals.Union(storesWithRecs).Distinct().ToList();
    }
}
