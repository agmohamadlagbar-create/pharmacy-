using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class MarketingService : IMarketingService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<MarketingService> _logger;

    public MarketingService(ApplicationDbContext db, ILogger<MarketingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<object> GetMarketingInsightsAsync(string storeUserId)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);

        // Top performing products this month
        var topProducts = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order!.StoreUserId == storeUserId &&
                         oi.Order.CreatedAtUtc >= monthStart &&
                         oi.Order.Status != OrderStatus.Cancelled &&
                         oi.ProductId != null)
            .GroupBy(oi => new { oi.ProductId, oi.Product!.Name, oi.Product.Price })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                TotalSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.UnitPrice * x.Quantity),
                UnitPrice = g.Key.Price
            })
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .ToListAsync();

        // Revenue comparison month-over-month
        var currentMonthRevenue = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order!.StoreUserId == storeUserId &&
                         oi.Order.CreatedAtUtc >= monthStart &&
                         oi.Order.Status != OrderStatus.Cancelled)
            .SumAsync(oi => oi.UnitPrice * oi.Quantity);

        var lastMonthRevenue = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order!.StoreUserId == storeUserId &&
                         oi.Order.CreatedAtUtc >= lastMonthStart &&
                         oi.Order.CreatedAtUtc < monthStart &&
                         oi.Order.Status != OrderStatus.Cancelled)
            .SumAsync(oi => oi.UnitPrice * oi.Quantity);

        var revenueChange = lastMonthRevenue > 0
            ? Math.Round((currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue * 100, 1)
            : 0;

        // New customers this month vs returning
        var thisMonthBuyers = await _db.Orders
            .AsNoTracking()
            .Where(o => o.StoreUserId == storeUserId &&
                        o.CreatedAtUtc >= monthStart &&
                        o.Status != OrderStatus.Cancelled)
            .Select(o => o.UserId)
            .Distinct()
            .ToListAsync();

        var allPreviousBuyers = await _db.Orders
            .AsNoTracking()
            .Where(o => o.StoreUserId == storeUserId &&
                        o.CreatedAtUtc < monthStart &&
                        o.Status != OrderStatus.Cancelled)
            .Select(o => o.UserId)
            .Distinct()
            .ToListAsync();

        var newCustomers = thisMonthBuyers.Except(allPreviousBuyers).Count();
        var returningCustomers = thisMonthBuyers.Intersect(allPreviousBuyers).Count();
        var totalCustomersThisMonth = thisMonthBuyers.Count;

        return new
        {
            TopProducts = topProducts,
            Revenue = new
            {
                CurrentMonth = currentMonthRevenue,
                LastMonth = lastMonthRevenue,
                ChangePercent = revenueChange,
                Direction = revenueChange >= 0 ? "up" : "down"
            },
            Customers = new
            {
                TotalThisMonth = totalCustomersThisMonth,
                NewCustomers = newCustomers,
                ReturningCustomers = returningCustomers,
                NewCustomerPercent = totalCustomersThisMonth > 0
                    ? Math.Round(newCustomers * 100.0 / totalCustomersThisMonth, 1)
                    : 0
            }
        };
    }

    public async Task<object> SuggestPromotionsAsync(string storeUserId)
    {
        // Find slow-moving products: low sales velocity despite adequate stock
        var since90Days = DateTime.UtcNow.AddDays(-90);

        var productSales = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order!.StoreUserId == storeUserId &&
                         oi.Order.CreatedAtUtc >= since90Days &&
                         oi.Order.Status != OrderStatus.Cancelled &&
                         oi.ProductId != null)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(x => x.Quantity) })
            .ToListAsync();

        var productIdsWithSales = productSales.Select(p => p.ProductId).ToHashSet();
        var salesLookup = productSales.ToDictionary(p => p.ProductId!.Value, p => p.TotalSold);

        var slowMoving = await _db.Products
            .AsNoTracking()
            .Where(p => p.StoreUserId == storeUserId &&
                        p.Status == ProductStatus.Approved &&
                        p.StockQuantity > 20)
            .Select(p => new { p.Id, p.Name, p.Price, CategoryName = p.Category!.Name, p.StockQuantity })
            .ToListAsync();

        var candidates = slowMoving
            .Where(p =>
            {
                salesLookup.TryGetValue(p.Id, out var sold);
                return sold <= 2; // At most 2 units sold in 90 days
            })
            .OrderByDescending(p => p.StockQuantity)
            .Take(15)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                Category = p.CategoryName,
                p.StockQuantity,
                UnitsSold90Days = salesLookup.GetValueOrDefault(p.Id, 0),
                SuggestedDiscount = p.Price switch
                {
                    >= 100 => "15-25% off",
                    >= 50 => "10-20% off",
                    >= 20 => "10-15% off",
                    _ => "Buy one get one free"
                },
                Strategy = p.StockQuantity > 100
                    ? "Deep discount to clear inventory"
                    : "Promotional bundle or limited-time offer"
            })
            .ToList();

        return new
        {
            Period90Days = since90Days.ToString("yyyy-MM-dd"),
            SuggestedPromotions = candidates,
            Count = candidates.Count,
            Summary = candidates.Count > 0
                ? $"Found {candidates.Count} slow-moving products suitable for promotional campaigns."
                : "No slow-moving products identified at this time."
        };
    }

    public async Task<object> SuggestBundlesAsync(string storeUserId)
    {
        // Identify frequently co-purchased products via order-level co-occurrence
        var since90Days = DateTime.UtcNow.AddDays(-90);

        var orderProductLists = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order!.StoreUserId == storeUserId &&
                         oi.Order.CreatedAtUtc >= since90Days &&
                         oi.Order.Status != OrderStatus.Cancelled &&
                         oi.ProductId != null)
            .GroupBy(oi => oi.OrderId)
            .Select(g => g.Select(oi => oi.ProductId!.Value).Distinct().ToList())
            .ToListAsync();

        var pairCounts = new Dictionary<(int, int), int>();
        var productNames = new Dictionary<int, string>();

        foreach (var productIds in orderProductLists)
        {
            for (var i = 0; i < productIds.Count; i++)
            {
                for (var j = i + 1; j < productIds.Count; j++)
                {
                    var a = productIds[i];
                    var b = productIds[j];
                    var key = a < b ? (a, b) : (b, a);
                    pairCounts.TryGetValue(key, out var count);
                    pairCounts[key] = count + 1;
                }
            }
        }

        // Get product names
        var allIds = pairCounts.Keys.SelectMany(k => new[] { k.Item1, k.Item2 }).Distinct().ToList();
        var nameMap = await _db.Products
            .AsNoTracking()
            .Where(p => allIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Price, p.StockQuantity, p.Status })
            .ToListAsync();

        var nameLookup = nameMap.ToDictionary(p => p.Id, p => p.Name);
        var priceLookup = nameMap.ToDictionary(p => p.Id, p => p.Price);
        var stockLookup = nameMap.ToDictionary(p => p.Id, p => p.StockQuantity);
        var statusLookup = nameMap.ToDictionary(p => p.Id, p => p.Status);

        var bundleSuggestions = pairCounts
            .Where(kvp => kvp.Value >= 2)
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .Select(kvp =>
            {
                var (productA, productB) = kvp.Key;
                var stockA = stockLookup.GetValueOrDefault(productA, 0);
                var stockB = stockLookup.GetValueOrDefault(productB, 0);

                return new
                {
                    ProductA = new { Id = productA, Name = nameLookup.GetValueOrDefault(productA, "Unknown") },
                    ProductB = new { Id = productB, Name = nameLookup.GetValueOrDefault(productB, "Unknown") },
                    CoPurchaseCount = kvp.Value,
                    CombinedPrice = priceLookup.GetValueOrDefault(productA, 0) + priceLookup.GetValueOrDefault(productB, 0),
                    SuggestedBundlePrice = Math.Round((priceLookup.GetValueOrDefault(productA, 0) + priceLookup.GetValueOrDefault(productB, 0)) * 0.85m, 2),
                    DiscountPercent = 15,
                    InStock = stockA > 0 && stockB > 0
                };
            })
            .ToList();

        return new
        {
            Period90Days = since90Days.ToString("yyyy-MM-dd"),
            BundleSuggestions = bundleSuggestions,
            Count = bundleSuggestions.Count,
            Summary = bundleSuggestions.Count > 0
                ? $"Found {bundleSuggestions.Count} frequently co-purchased product pairs suitable for bundle deals."
                : "Not enough order data to suggest bundles yet."
        };
    }
}
