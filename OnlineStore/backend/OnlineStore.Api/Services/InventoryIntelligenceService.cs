using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

/// <summary>
/// Inventory intelligence service providing stock analysis, restock suggestions,
/// and sales velocity tracking for shop owners.
/// </summary>
public class InventoryIntelligenceService : IInventoryIntelligenceService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<InventoryIntelligenceService> _logger;

    public InventoryIntelligenceService(ApplicationDbContext db, ILogger<InventoryIntelligenceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get products that are running low on stock based on the store's threshold.
    /// </summary>
    public async Task<object> GetLowStockProductsAsync(string storeUserId)
    {
        var shopProfile = await _db.ShopProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == storeUserId);

        var threshold = shopProfile?.LowStockThreshold ?? 10;

        var lowStockProducts = await _db.Products
            .AsNoTracking()
            .Where(p => p.StoreUserId == storeUserId &&
                        p.Status == ProductStatus.Approved &&
                        p.StockQuantity <= threshold)
            .OrderBy(p => p.StockQuantity)
            .Select(p => new
            {
                p.Id,
                p.Name,
                CurrentStock = p.StockQuantity,
                Threshold = threshold,
                p.Price,
                Category = p.Category != null ? p.Category.Name : "Uncategorized"
            })
            .Take(20)
            .ToListAsync();

        return new
        {
            Threshold = threshold,
            Count = lowStockProducts.Count,
            Products = lowStockProducts
        };
    }

    /// <summary>
    /// Get products that are selling quickly based on recent order volume.
    /// </summary>
    public async Task<object> GetFastSellingProductsAsync(string storeUserId, int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var fastSelling = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order!.StoreUserId == storeUserId &&
                         oi.Order.CreatedAtUtc >= since &&
                         oi.Order.Status != OrderStatus.Cancelled &&
                         oi.ProductId != null)
            .GroupBy(oi => new { oi.ProductId, oi.Product!.Name, oi.Product.StockQuantity })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                CurrentStock = g.Key.StockQuantity,
                TotalSold = g.Sum(x => x.Quantity),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(15)
            .ToListAsync();

        return new
        {
            Period = $"Last {days} days",
            Count = fastSelling.Count,
            Products = fastSelling.Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.CurrentStock,
                p.TotalSold,
                p.OrderCount,
                DailyAverage = Math.Round((double)p.TotalSold / days, 1),
                DaysUntilStockout = p.TotalSold > 0
                    ? Math.Round((double)p.CurrentStock / ((double)p.TotalSold / days), 0)
                    : 999
            })
        };
    }

    /// <summary>
    /// Generate intelligent restocking suggestions combining low stock and sales velocity.
    /// </summary>
    public async Task<object> GetRestockSuggestionsAsync(string storeUserId)
    {
        var lowStock = await GetLowStockProductsAsync(storeUserId);
        var fastSelling = await GetFastSellingProductsAsync(storeUserId, 14); // 2-week window

        return new
        {
            LowStock = lowStock,
            FastSelling = fastSelling,
            Summary = "Review the low stock products and fast-selling items above. " +
                      "Priority restocking should focus on items that appear in both lists."
        };
    }

    /// <summary>
    /// Get overall inventory health score: percentage of products at risk, overstocked, and balanced.
    /// </summary>
    public async Task<object> GetInventoryHealthScoreAsync(string storeUserId)
    {
        var shopProfile = await _db.ShopProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == storeUserId);

        var threshold = shopProfile?.LowStockThreshold ?? 10;

        var allProducts = await _db.Products
            .AsNoTracking()
            .Where(p => p.StoreUserId == storeUserId && p.Status == ProductStatus.Approved)
            .Select(p => new { p.Id, p.StockQuantity })
            .ToListAsync();

        var total = allProducts.Count;
        if (total == 0)
        {
            return new
            {
                TotalProducts = 0,
                AtRiskPercent = 0.0,
                OverstockedPercent = 0.0,
                BalancedPercent = 0.0,
                AtRiskCount = 0,
                OverstockedCount = 0,
                BalancedCount = 0,
                Threshold = threshold,
                OverstockThreshold = 100,
                HealthRating = "No products"
            };
        }

        var atRisk = allProducts.Count(p => p.StockQuantity <= threshold);
        var overstocked = allProducts.Count(p => p.StockQuantity > 100);
        var balanced = total - atRisk - overstocked;

        var healthRating = (atRisk * 1.0 / total) switch
        {
            <= 0.05 => "Excellent",
            <= 0.15 => "Good",
            <= 0.30 => "Fair",
            _ => "Needs Attention"
        };

        return new
        {
            TotalProducts = total,
            AtRiskPercent = Math.Round(atRisk * 100.0 / total, 1),
            OverstockedPercent = Math.Round(overstocked * 100.0 / total, 1),
            BalancedPercent = Math.Round(balanced * 100.0 / total, 1),
            AtRiskCount = atRisk,
            OverstockedCount = overstocked,
            BalancedCount = balanced,
            Threshold = threshold,
            OverstockThreshold = 100,
            HealthRating = healthRating
        };
    }

    /// <summary>
    /// Compare current period sales with the same period last year to predict seasonal demand.
    /// </summary>
    public async Task<object> GetSeasonalDemandForecastAsync(string storeUserId)
    {
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var lastYearStart = periodStart.AddYears(-1);
        var lastYearEnd = periodEnd.AddYears(-1);

        // Current period sales
        var currentSales = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order!.StoreUserId == storeUserId &&
                         oi.Order.CreatedAtUtc >= periodStart &&
                         oi.Order.CreatedAtUtc <= periodEnd &&
                         oi.Order.Status != OrderStatus.Cancelled &&
                         oi.ProductId != null)
            .GroupBy(oi => new { oi.ProductId, oi.Product!.Name })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                TotalSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(10)
            .ToListAsync();

        // Same period last year sales
        var lastYearSales = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order!.StoreUserId == storeUserId &&
                         oi.Order.CreatedAtUtc >= lastYearStart &&
                         oi.Order.CreatedAtUtc <= lastYearEnd &&
                         oi.Order.Status != OrderStatus.Cancelled &&
                         oi.ProductId != null)
            .GroupBy(oi => new { oi.ProductId, oi.Product!.Name })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                TotalSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(10)
            .ToListAsync();

        var currentTotalRevenue = currentSales.Sum(s => s.Revenue);
        var lastYearTotalRevenue = lastYearSales.Sum(s => s.Revenue);
        var revenueChange = lastYearTotalRevenue > 0
            ? Math.Round((currentTotalRevenue - lastYearTotalRevenue) / lastYearTotalRevenue * 100, 1)
            : 0;

        return new
        {
            CurrentPeriod = new { Start = periodStart.ToString("yyyy-MM-dd"), End = periodEnd.ToString("yyyy-MM-dd") },
            LastYearPeriod = new { Start = lastYearStart.ToString("yyyy-MM-dd"), End = lastYearEnd.ToString("yyyy-MM-dd") },
            CurrentSales = currentSales,
            LastYearSales = lastYearSales,
            CurrentTotalRevenue = currentTotalRevenue,
            LastYearTotalRevenue = lastYearTotalRevenue,
            RevenueChangePercent = revenueChange,
            Direction = revenueChange >= 0 ? "up" : "down",
            Forecast = revenueChange >= 0
                ? "Demand is trending upward compared to last year. Consider increasing stock levels."
                : "Demand is trending downward compared to last year. Consider promotional strategies to boost sales."
        };
    }
}
