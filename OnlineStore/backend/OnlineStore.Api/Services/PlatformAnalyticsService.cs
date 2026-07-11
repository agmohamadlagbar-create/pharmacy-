using Microsoft.EntityFrameworkCore;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class PlatformAnalyticsService : IPlatformAnalyticsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PlatformAnalyticsService> _logger;

    public PlatformAnalyticsService(ApplicationDbContext db, ILogger<PlatformAnalyticsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<object> GetPlatformOverviewAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var totalGmv = await _db.Orders
            .Where(o => o.Status != Domain.Entities.OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var totalOrders = await _db.Orders.CountAsync();
        var totalUsers = await _db.Users.CountAsync();
        var totalStores = await _db.ShopProfiles.CountAsync();
        var activeUsers30 = await _db.Orders
            .Where(o => o.CreatedAtUtc >= now.AddDays(-30))
            .Select(o => o.UserId).Distinct().CountAsync();
        var newUsersThisMonth = await _db.Users
            .Where(u => u.CreatedAtUtc >= monthStart && u.CreatedAtUtc < monthEnd)
            .CountAsync();

        return new
        {
            TotalGmv = totalGmv,
            TotalOrders = totalOrders,
            TotalUsers = totalUsers,
            TotalStores = totalStores,
            ActiveUsersLast30Days = activeUsers30,
            NewUsersThisMonth = newUsersThisMonth
        };
    }

    public async Task<object> GetStoreRankingsAsync(string metric = "revenue", int top = 10)
    {
        var now = DateTime.UtcNow;
        var query = _db.ShopProfiles.AsNoTracking().ToList();

        var rankings = new List<object>();
        foreach (var shop in query)
        {
            var storeOrders = await _db.Orders
                .Where(o => o.StoreUserId == shop.UserId && o.Status != Domain.Entities.OrderStatus.Cancelled)
                .ToListAsync();

            decimal revenue = storeOrders.Sum(o => o.TotalAmount);
            int orderCount = storeOrders.Count;
            double avgRating = 0;
            var reviews = await _db.Reviews
                .Where(r => r.Product!.StoreUserId == shop.UserId)
                .ToListAsync();
            if (reviews.Any()) avgRating = reviews.Average(r => r.Rating);

            double growthRate = 0;
            var lastMonth = storeOrders.Where(o => o.CreatedAtUtc >= now.AddMonths(-1)).Sum(o => o.TotalAmount);
            var prevMonth = storeOrders.Where(o => o.CreatedAtUtc >= now.AddMonths(-2) && o.CreatedAtUtc < now.AddMonths(-1)).Sum(o => o.TotalAmount);
            if (prevMonth > 0) growthRate = (double)((lastMonth - prevMonth) / prevMonth * 100);

            rankings.Add(new
            {
                StoreUserId = shop.UserId,
                ShopName = shop.ShopName,
                shop.Location,
                Revenue = revenue,
                OrderCount = orderCount,
                AvgRating = Math.Round(avgRating, 1),
                GrowthRate = Math.Round(growthRate, 1)
            });
        }

        var ranked = metric.ToLowerInvariant() switch
        {
            "orders" => rankings.OrderByDescending(r => r.GetType().GetProperty("OrderCount")!.GetValue(r)).Take(top).ToList(),
            "rating" => rankings.OrderByDescending(r => r.GetType().GetProperty("AvgRating")!.GetValue(r)).Take(top).ToList(),
            "growth" => rankings.OrderByDescending(r => r.GetType().GetProperty("GrowthRate")!.GetValue(r)).Take(top).ToList(),
            _ => rankings.OrderByDescending(r => r.GetType().GetProperty("Revenue")!.GetValue(r)).Take(top).ToList()
        };

        return new { Metric = metric, Rankings = ranked };
    }

    public async Task<object> GetCategoryTrendsAsync(int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var prevSince = since.AddDays(-days);

        var categories = await _db.Categories.ToListAsync();
        var trends = new List<object>();

        foreach (var cat in categories)
        {
            var currentOrders = await _db.OrderItems
                .Where(oi => oi.Product!.CategoryId == cat.Id && oi.Order!.CreatedAtUtc >= since)
                .SumAsync(oi => (decimal?)oi.UnitPrice * oi.Quantity) ?? 0;

            var currentCount = await _db.OrderItems
                .CountAsync(oi => oi.Product!.CategoryId == cat.Id && oi.Order!.CreatedAtUtc >= since);

            var prevOrders = await _db.OrderItems
                .Where(oi => oi.Product!.CategoryId == cat.Id && oi.Order!.CreatedAtUtc >= prevSince && oi.Order!.CreatedAtUtc < since)
                .SumAsync(oi => (decimal?)oi.UnitPrice * oi.Quantity) ?? 0;

            double growthRate = prevOrders > 0 ? (double)((currentOrders - prevOrders) / prevOrders * 100) : 0;

            trends.Add(new
            {
                CategoryName = cat.Name,
                Revenue = currentOrders,
                OrderCount = currentCount,
                GrowthRate = Math.Round(growthRate, 1)
            });
        }

        return new
        {
            PeriodDays = days,
            Categories = trends.OrderByDescending(t => t.GetType().GetProperty("Revenue")!.GetValue(t))
        };
    }

    public async Task<object> GetRegionalAnalyticsAsync()
    {
        var stores = await _db.ShopProfiles.AsNoTracking().ToListAsync();
        var storeRegions = stores.GroupBy(s => s.RegionCode ?? "GLOBAL")
            .Select(g => new { Region = g.Key, StoreCount = g.Count() })
            .ToList();

        var customers = await _db.Users.AsNoTracking().ToListAsync();
        var customerRegions = customers.GroupBy(u => u.Country ?? "Unknown")
            .Select(g => new { Region = g.Key, CustomerCount = g.Count() })
            .ToList();

        var orders = await _db.Orders
            .Where(o => o.Status != Domain.Entities.OrderStatus.Cancelled)
            .GroupBy(o => o.StoreUserId)
            .Select(g => new { StoreUserId = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
            .ToListAsync();

        var storeRevenue = orders.GroupBy(o => stores.FirstOrDefault(s => s.UserId == o.StoreUserId)?.RegionCode ?? "GLOBAL")
            .Select(g => new { Region = g.Key, Revenue = g.Sum(x => x.Revenue) })
            .ToList();

        return new
        {
            StoresByRegion = storeRegions,
            CustomersByRegion = customerRegions,
            RevenueByRegion = storeRevenue
        };
    }
}
