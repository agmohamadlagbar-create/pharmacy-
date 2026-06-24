using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _db;

    public AnalyticsService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ═══════════════════════════════════════════════════
    //  Currency Helper — Country → Symbol
    // ═══════════════════════════════════════════════════

    private static readonly Dictionary<string, string> CountryCurrencyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Turkey"] = "₺", ["Türkiye"] = "₺", ["TR"] = "₺",
        ["United States"] = "$", ["US"] = "$", ["USA"] = "$",
        ["United Kingdom"] = "£", ["UK"] = "£", ["GB"] = "£",
        ["Germany"] = "€", ["France"] = "€", ["Italy"] = "€", ["Spain"] = "€",
        ["Netherlands"] = "€", ["Belgium"] = "€", ["Austria"] = "€",
        ["Ireland"] = "€", ["Portugal"] = "€", ["Greece"] = "€", ["Finland"] = "€",
        ["EU"] = "€",
        ["Japan"] = "¥", ["JP"] = "¥",
        ["China"] = "¥", ["CN"] = "¥",
        ["India"] = "₹", ["IN"] = "₹",
        ["Russia"] = "₽", ["RU"] = "₽",
        ["Brazil"] = "R$", ["BR"] = "R$",
        ["South Korea"] = "₩", ["KR"] = "₩",
        ["Saudi Arabia"] = "﷼", ["SA"] = "﷼",
        ["UAE"] = "د.إ", ["United Arab Emirates"] = "د.إ",
        ["Egypt"] = "£", ["EG"] = "E£",
        ["Iraq"] = "ع.د", ["IQ"] = "ع.د",
        ["Syria"] = "£", ["SY"] = "S£",
        ["Lebanon"] = "ل.ل", ["LB"] = "ل.ل",
        ["Jordan"] = "د.ا", ["JO"] = "د.ا",
        ["Kuwait"] = "د.ك", ["KW"] = "د.ك",
        ["Qatar"] = "ر.ق", ["QA"] = "ر.ق",
        ["Bahrain"] = "د.ب", ["BH"] = "د.ب",
        ["Oman"] = "ر.ع", ["OM"] = "ر.ع",
        ["Pakistan"] = "₨", ["PK"] = "₨",
        ["Nigeria"] = "₦", ["NG"] = "₦",
        ["South Africa"] = "R", ["ZA"] = "R",
        ["Mexico"] = "$", ["MX"] = "MX$",
        ["Canada"] = "C$", ["CA"] = "C$",
        ["Australia"] = "A$", ["AU"] = "A$",
        ["Switzerland"] = "CHF", ["CH"] = "CHF",
        ["Sweden"] = "kr", ["SE"] = "kr",
        ["Norway"] = "kr", ["NO"] = "kr",
        ["Denmark"] = "kr", ["DK"] = "kr",
        ["Poland"] = "zł", ["PL"] = "zł",
        ["Czech Republic"] = "Kč", ["CZ"] = "Kč",
        ["Hungary"] = "Ft", ["HU"] = "Ft",
        ["Romania"] = "lei", ["RO"] = "lei",
        ["Thailand"] = "฿", ["TH"] = "฿",
        ["Malaysia"] = "RM", ["MY"] = "RM",
        ["Indonesia"] = "Rp", ["ID"] = "Rp",
        ["Philippines"] = "₱", ["PH"] = "₱",
        ["Vietnam"] = "₫", ["VN"] = "₫",
        ["Singapore"] = "S$", ["SG"] = "S$",
        ["Israel"] = "₪", ["IL"] = "₪",
        ["Argentina"] = "AR$", ["AR"] = "AR$",
        ["Colombia"] = "COL$", ["CO"] = "COL$",
        ["Chile"] = "CLP$", ["CL"] = "CLP$",
        ["Peru"] = "S/", ["PE"] = "S/",
    };

    private static string GetCurrencySymbol(string? country)
    {
        if (string.IsNullOrWhiteSpace(country)) return "$";
        return CountryCurrencyMap.TryGetValue(country.Trim(), out var symbol) ? symbol : "$";
    }

    // ═══════════════════════════════════════════════════
    //  Full Dashboard
    // ═══════════════════════════════════════════════════

    public async Task<FullAnalyticsDto> GetFullAnalyticsAsync(string storeUserId, DateTime? startDate, DateTime? endDate)
    {
        var now = DateTime.UtcNow;
        var rangeStart = startDate ?? now.AddYears(-1);
        var rangeEnd = endDate ?? now;

        // Load store profile (for currency, threshold)
        var shopProfile = await _db.ShopProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == storeUserId);

        var storeUser = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == storeUserId)
            .Select(u => new { u.Country })
            .FirstOrDefaultAsync();

        var currencySymbol = GetCurrencySymbol(storeUser?.Country);
        var lowStockThreshold = shopProfile?.LowStockThreshold ?? 10;

        // ── Load all non-cancelled orders for this store with items (single query) ──
        var allOrders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.StoreUserId == storeUserId && o.Status != OrderStatus.Cancelled)
            .Select(o => new
            {
                o.Id,
                o.UserId,
                o.TotalAmount,
                o.CreatedAtUtc,
                o.Status,
                Items = o.Items.Select(i => new
                {
                    i.ProductId,
                    i.Quantity,
                    i.UnitPrice
                }).ToList()
            })
            .ToListAsync();

        // ── Load all store products (single query) ──
        var allProducts = await _db.Products
            .AsNoTracking()
            .Where(p => p.StoreUserId == storeUserId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ImageUrl,
                p.Price,
                p.StockQuantity,
                p.OriginalQuantity,
                p.Status
            })
            .ToListAsync();

        var productDict = allProducts.ToDictionary(p => p.Id);

        // ── Load reviews for store products (single query) ──
        var productIds = allProducts.Select(p => p.Id).ToList();
        var allReviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => productIds.Contains(r.ProductId))
            .Select(r => new { r.ProductId, r.Rating })
            .ToListAsync();

        // ═══ Section 1: Summary Cards ═══
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var rangeOrders = allOrders.Where(o => o.CreatedAtUtc >= rangeStart && o.CreatedAtUtc <= rangeEnd).ToList();

        var totalRevenue = rangeOrders.Sum(o => o.TotalAmount);
        var todayRevenue = allOrders.Where(o => o.CreatedAtUtc >= todayStart).Sum(o => o.TotalAmount);
        var monthlyRevenue = allOrders.Where(o => o.CreatedAtUtc >= monthStart).Sum(o => o.TotalAmount);
        var yearlyRevenue = allOrders.Where(o => o.CreatedAtUtc >= yearStart).Sum(o => o.TotalAmount);
        var totalOrders = rangeOrders.Count;
        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0m;

        var summary = new AnalyticsSummaryDto(
            totalRevenue, todayRevenue, monthlyRevenue, yearlyRevenue,
            totalOrders, Math.Round(avgOrderValue, 2), currencySymbol
        );

        // ═══ Section 2: Sales Charts ═══
        // Daily — last 7 days
        var dailySales = Enumerable.Range(0, 7)
            .Select(i => now.Date.AddDays(-6 + i))
            .Select(day =>
            {
                var dayOrders = allOrders.Where(o => o.CreatedAtUtc.Date == day).ToList();
                return new DailySalesPointDto(
                    day.ToString("MMM dd"),
                    dayOrders.Sum(o => o.TotalAmount),
                    dayOrders.Count
                );
            })
            .ToList();

        // Monthly — last 12 months
        var monthlySales = Enumerable.Range(0, 12)
            .Select(i =>
            {
                var month = now.AddMonths(-11 + i);
                var mStart = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var mEnd = mStart.AddMonths(1);
                var mOrders = allOrders.Where(o => o.CreatedAtUtc >= mStart && o.CreatedAtUtc < mEnd).ToList();
                return new MonthlySalesPointDto(
                    mStart.ToString("MMM yyyy"),
                    mOrders.Sum(o => o.TotalAmount),
                    mOrders.Count
                );
            })
            .ToList();

        // ═══ Section 3: Top Selling Products ═══
        var productSales = rangeOrders
            .SelectMany(o => o.Items)
            .Where(i => i.ProductId.HasValue)
            .GroupBy(i => i.ProductId!.Value)
            .Select(g =>
            {
                var product = productDict.GetValueOrDefault(g.Key);
                return new TopSellingProductDto(
                    g.Key,
                    product?.Name ?? "Unknown",
                    product?.ImageUrl,
                    g.Sum(i => i.Quantity),
                    g.Sum(i => i.Quantity * i.UnitPrice)
                );
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(10)
            .ToList();

        // ═══ Section 4: Product Previews ═══
        var allItemsSold = allOrders
            .SelectMany(o => o.Items)
            .Where(i => i.ProductId.HasValue)
            .GroupBy(i => i.ProductId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var productPreviews = allProducts
            .Where(p => p.Status == ProductStatus.Approved)
            .Select(p => new ProductPreviewDto(
                p.Id,
                p.Name,
                p.ImageUrl,
                p.StockQuantity,
                p.Price,
                allItemsSold.GetValueOrDefault(p.Id, 0)
            ))
            .OrderByDescending(p => p.TotalSoldCount)
            .Take(12)
            .ToList();

        // ═══ Section 5: Growth Percentage ═══
        var prevMonthStart = monthStart.AddMonths(-1);
        var prevMonthEnd = monthStart;

        var prevMonthOrders = allOrders.Where(o => o.CreatedAtUtc >= prevMonthStart && o.CreatedAtUtc < prevMonthEnd).ToList();
        var currMonthOrders = allOrders.Where(o => o.CreatedAtUtc >= monthStart && o.CreatedAtUtc < monthStart.AddMonths(1)).ToList();

        var prevRevenue = prevMonthOrders.Sum(o => o.TotalAmount);
        var currRevenue = currMonthOrders.Sum(o => o.TotalAmount);
        var prevOrderCount = prevMonthOrders.Count;
        var currOrderCount = currMonthOrders.Count;

        var revenueGrowth = prevRevenue > 0
            ? Math.Round(((currRevenue - prevRevenue) / prevRevenue) * 100, 2)
            : currRevenue > 0 ? 100m : 0m;

        var orderGrowth = prevOrderCount > 0
            ? Math.Round(((decimal)(currOrderCount - prevOrderCount) / prevOrderCount) * 100, 2)
            : currOrderCount > 0 ? 100m : 0m;

        var growth = new GrowthDto(
            revenueGrowth, orderGrowth,
            prevRevenue, currRevenue,
            prevOrderCount, currOrderCount
        );

        // ═══ Section 6: Trending Products ═══
        var last7Start = now.AddDays(-7);
        var prev7Start = now.AddDays(-14);

        var last7Items = allOrders
            .Where(o => o.CreatedAtUtc >= last7Start)
            .SelectMany(o => o.Items)
            .Where(i => i.ProductId.HasValue)
            .GroupBy(i => i.ProductId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var prev7Items = allOrders
            .Where(o => o.CreatedAtUtc >= prev7Start && o.CreatedAtUtc < last7Start)
            .SelectMany(o => o.Items)
            .Where(i => i.ProductId.HasValue)
            .GroupBy(i => i.ProductId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var trendingProducts = last7Items
            .Select(kvp =>
            {
                var prevQty = prev7Items.GetValueOrDefault(kvp.Key, 0);
                var currentQty = kvp.Value;
                var growthPct = prevQty > 0
                    ? Math.Round(((decimal)(currentQty - prevQty) / prevQty) * 100, 2)
                    : currentQty > 0 ? 100m : 0m;
                var product = productDict.GetValueOrDefault(kvp.Key);
                return new TrendingProductDto(
                    kvp.Key,
                    product?.Name ?? "Unknown",
                    product?.ImageUrl,
                    growthPct,
                    currentQty - prevQty,
                    currentQty,
                    prevQty
                );
            })
            .Where(t => t.GrowthPercent > 0)
            .OrderByDescending(t => t.GrowthPercent)
            .Take(10)
            .ToList();

        // ═══ Section 7: Returning Customers ═══
        var customerOrderCounts = allOrders
            .GroupBy(o => o.UserId)
            .ToDictionary(g => g.Key, g => g.Count());

        var totalCustomers = customerOrderCounts.Count;
        var returningCount = customerOrderCounts.Count(kvp => kvp.Value > 1);
        var returningPct = totalCustomers > 0
            ? Math.Round((decimal)returningCount / totalCustomers * 100, 2)
            : 0m;

        var returningCustomers = new ReturningCustomersDto(
            totalCustomers, returningCount, returningPct
        );

        // ═══ Section 8: Product Rating Statistics ═══
        ProductRatingStatsDto? ratingStats = null;
        if (allReviews.Count > 0)
        {
            var overallAvg = allReviews.Average(r => r.Rating);
            var reviewsByProduct = allReviews
                .GroupBy(r => r.ProductId)
                .Select(g =>
                {
                    var product = productDict.GetValueOrDefault(g.Key);
                    return new RatedProductDto(
                        g.Key,
                        product?.Name ?? "Unknown",
                        product?.ImageUrl,
                        Math.Round(g.Average(r => r.Rating), 2),
                        g.Count()
                    );
                })
                .ToList();

            var highestRated = reviewsByProduct
                .Where(r => r.ReviewCount >= 1)
                .OrderByDescending(r => r.AverageRating)
                .ThenByDescending(r => r.ReviewCount)
                .Take(5)
                .ToList();

            var lowestRated = reviewsByProduct
                .Where(r => r.ReviewCount >= 1)
                .OrderBy(r => r.AverageRating)
                .ThenByDescending(r => r.ReviewCount)
                .Take(5)
                .ToList();

            ratingStats = new ProductRatingStatsDto(
                Math.Round(overallAvg, 2),
                allReviews.Count,
                highestRated,
                lowestRated
            );
        }

        // ═══ Section 9: Low Stock Warnings ═══
        var lowStockProducts = allProducts
            .Where(p => p.Status == ProductStatus.Approved && p.StockQuantity < lowStockThreshold)
            .OrderBy(p => p.StockQuantity)
            .Select(p => new LowStockProductDto(
                p.Id, p.Name, p.ImageUrl, p.StockQuantity, lowStockThreshold
            ))
            .ToList();

        // ═══ Section 10: AI Recommendations ═══
        var recommendations = GenerateRecommendations(
            trendingProducts, lowStockProducts, ratingStats,
            productSales, currencySymbol,
            monthlyRevenue, prevRevenue
        );

        return new FullAnalyticsDto(
            summary, dailySales, monthlySales, productSales, productPreviews,
            growth, trendingProducts, returningCustomers, ratingStats,
            lowStockProducts, recommendations, lowStockThreshold
        );
    }

    // ═══════════════════════════════════════════════════
    //  AI Recommendations (Rule-Based)
    // ═══════════════════════════════════════════════════

    private static List<AiRecommendationDto> GenerateRecommendations(
        List<TrendingProductDto> trending,
        List<LowStockProductDto> lowStock,
        ProductRatingStatsDto? ratings,
        List<TopSellingProductDto> topSelling,
        string currency,
        decimal currentMonthRev,
        decimal prevMonthRev)
    {
        var recs = new List<AiRecommendationDto>();

        // Trending products — sales increase
        foreach (var t in trending.Take(3))
        {
            recs.Add(new AiRecommendationDto(
                "sales_up",
                $"\"{t.ProductName}\" demand increased by {t.GrowthPercent:F0}% in the last 7 days. Consider increasing stock.",
                t.ProductName,
                t.GrowthPercent
            ));
        }

        // Low stock warnings
        foreach (var ls in lowStock.Take(3))
        {
            recs.Add(new AiRecommendationDto(
                "low_stock",
                $"\"{ls.ProductName}\" has very low stock ({ls.RemainingQuantity} remaining). Restock soon to avoid lost sales.",
                ls.ProductName,
                ls.RemainingQuantity
            ));
        }

        // High rated products
        if (ratings?.HighestRated != null)
        {
            foreach (var hr in ratings.HighestRated.Where(r => r.AverageRating >= 4.5).Take(2))
            {
                recs.Add(new AiRecommendationDto(
                    "rating_high",
                    $"\"{hr.ProductName}\" has excellent ratings ({hr.AverageRating:F1}★ from {hr.ReviewCount} reviews). Feature it prominently.",
                    hr.ProductName,
                    (decimal)hr.AverageRating
                ));
            }
        }

        // Low rated products
        if (ratings?.LowestRated != null)
        {
            foreach (var lr in ratings.LowestRated.Where(r => r.AverageRating <= 2.0 && r.ReviewCount >= 2).Take(2))
            {
                recs.Add(new AiRecommendationDto(
                    "rating_low",
                    $"\"{lr.ProductName}\" has low ratings ({lr.AverageRating:F1}★). Consider improving quality or updating the product.",
                    lr.ProductName,
                    (decimal)lr.AverageRating
                ));
            }
        }

        // Revenue trend
        if (prevMonthRev > 0)
        {
            var revChange = ((currentMonthRev - prevMonthRev) / prevMonthRev) * 100;
            if (revChange < -10)
            {
                recs.Add(new AiRecommendationDto(
                    "sales_down",
                    $"Monthly revenue decreased by {Math.Abs(revChange):F0}% compared to last month. Consider running promotions.",
                    null,
                    revChange
                ));
            }
            else if (revChange > 20)
            {
                recs.Add(new AiRecommendationDto(
                    "sales_up",
                    $"Monthly revenue grew by {revChange:F0}%! Great performance — consider expanding your product range.",
                    null,
                    revChange
                ));
            }
        }

        // Best seller recommendation
        var topProduct = topSelling.FirstOrDefault();
        if (topProduct != null && topProduct.QuantitySold >= 10)
        {
            recs.Add(new AiRecommendationDto(
                "stock",
                $"\"{topProduct.ProductName}\" is your best seller with {topProduct.QuantitySold} units sold ({currency}{topProduct.Revenue:F0} revenue). Ensure adequate stock.",
                topProduct.ProductName,
                topProduct.QuantitySold
            ));
        }

        return recs;
    }

    // ═══════════════════════════════════════════════════
    //  Monthly Report
    // ═══════════════════════════════════════════════════

    public async Task<MonthlyReportDto> GetMonthlyReportAsync(string storeUserId, int year, int month)
    {
        var storeUser = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == storeUserId)
            .Select(u => new { u.Country })
            .FirstOrDefaultAsync();

        var currencySymbol = GetCurrencySymbol(storeUser?.Country);

        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var prevMonthStart = monthStart.AddMonths(-1);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.StoreUserId == storeUserId
                        && o.Status != OrderStatus.Cancelled
                        && o.CreatedAtUtc >= prevMonthStart
                        && o.CreatedAtUtc < monthEnd)
            .Select(o => new
            {
                o.TotalAmount,
                o.CreatedAtUtc,
                Items = o.Items.Select(i => new
                {
                    i.ProductId,
                    i.Quantity,
                    i.UnitPrice
                }).ToList()
            })
            .ToListAsync();

        var currOrders = orders.Where(o => o.CreatedAtUtc >= monthStart && o.CreatedAtUtc < monthEnd).ToList();
        var prevOrders = orders.Where(o => o.CreatedAtUtc >= prevMonthStart && o.CreatedAtUtc < monthStart).ToList();

        var totalRevenue = currOrders.Sum(o => o.TotalAmount);
        var totalOrderCount = currOrders.Count;
        var avgOrderValue = totalOrderCount > 0 ? Math.Round(totalRevenue / totalOrderCount, 2) : 0m;

        var prevRevenue = prevOrders.Sum(o => o.TotalAmount);
        var prevOrderCount = prevOrders.Count;

        var revenueGrowth = prevRevenue > 0
            ? Math.Round(((totalRevenue - prevRevenue) / prevRevenue) * 100, 2)
            : totalRevenue > 0 ? 100m : 0m;
        var orderGrowth = prevOrderCount > 0
            ? Math.Round(((decimal)(totalOrderCount - prevOrderCount) / prevOrderCount) * 100, 2)
            : totalOrderCount > 0 ? 100m : 0m;

        // Product names lookup
        var productIdsInMonth = currOrders
            .SelectMany(o => o.Items)
            .Where(i => i.ProductId.HasValue)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        var productNames = await _db.Products
            .AsNoTracking()
            .Where(p => productIdsInMonth.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.ImageUrl })
            .ToDictionaryAsync(p => p.Id);

        var topProducts = currOrders
            .SelectMany(o => o.Items)
            .Where(i => i.ProductId.HasValue)
            .GroupBy(i => i.ProductId!.Value)
            .Select(g =>
            {
                productNames.TryGetValue(g.Key, out var pInfo);
                return new TopSellingProductDto(
                    g.Key,
                    pInfo?.Name ?? "Unknown",
                    pInfo?.ImageUrl,
                    g.Sum(i => i.Quantity),
                    g.Sum(i => i.Quantity * i.UnitPrice)
                );
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(10)
            .ToList();

        // Daily breakdown within the month
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var dailySales = Enumerable.Range(1, daysInMonth)
            .Select(d =>
            {
                var day = new DateTime(year, month, d, 0, 0, 0, DateTimeKind.Utc);
                var dayOrders = currOrders.Where(o => o.CreatedAtUtc.Date == day).ToList();
                return new DailySalesPointDto(
                    day.ToString("MMM dd"),
                    dayOrders.Sum(o => o.TotalAmount),
                    dayOrders.Count
                );
            })
            .ToList();

        return new MonthlyReportDto(
            year, month,
            CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
            currencySymbol,
            totalRevenue, totalOrderCount, avgOrderValue,
            revenueGrowth, orderGrowth,
            topProducts, dailySales
        );
    }

    // ═══════════════════════════════════════════════════
    //  Yearly Report
    // ═══════════════════════════════════════════════════

    public async Task<YearlyReportDto> GetYearlyReportAsync(string storeUserId, int year)
    {
        var storeUser = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == storeUserId)
            .Select(u => new { u.Country })
            .FirstOrDefaultAsync();

        var currencySymbol = GetCurrencySymbol(storeUser?.Country);

        var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);
        var prevYearStart = yearStart.AddYears(-1);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.StoreUserId == storeUserId
                        && o.Status != OrderStatus.Cancelled
                        && o.CreatedAtUtc >= prevYearStart
                        && o.CreatedAtUtc < yearEnd)
            .Select(o => new
            {
                o.TotalAmount,
                o.CreatedAtUtc,
                Items = o.Items.Select(i => new
                {
                    i.ProductId,
                    i.Quantity,
                    i.UnitPrice
                }).ToList()
            })
            .ToListAsync();

        var yearOrders = orders.Where(o => o.CreatedAtUtc >= yearStart && o.CreatedAtUtc < yearEnd).ToList();
        var totalRevenue = yearOrders.Sum(o => o.TotalAmount);
        var totalOrderCount = yearOrders.Count;
        var avgOrderValue = totalOrderCount > 0 ? Math.Round(totalRevenue / totalOrderCount, 2) : 0m;

        // Monthly breakdown
        var monthlyBreakdown = Enumerable.Range(1, 12)
            .Select(m =>
            {
                var mStart = new DateTime(year, m, 1, 0, 0, 0, DateTimeKind.Utc);
                var mEnd = mStart.AddMonths(1);
                var mOrders = yearOrders.Where(o => o.CreatedAtUtc >= mStart && o.CreatedAtUtc < mEnd).ToList();
                return new MonthlySalesPointDto(
                    CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(m) + " " + year,
                    mOrders.Sum(o => o.TotalAmount),
                    mOrders.Count
                );
            })
            .ToList();

        // Top products for the year
        var productIdsInYear = yearOrders
            .SelectMany(o => o.Items)
            .Where(i => i.ProductId.HasValue)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        var productNames = await _db.Products
            .AsNoTracking()
            .Where(p => productIdsInYear.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.ImageUrl })
            .ToDictionaryAsync(p => p.Id);

        var topProducts = yearOrders
            .SelectMany(o => o.Items)
            .Where(i => i.ProductId.HasValue)
            .GroupBy(i => i.ProductId!.Value)
            .Select(g =>
            {
                productNames.TryGetValue(g.Key, out var pInfo);
                return new TopSellingProductDto(
                    g.Key,
                    pInfo?.Name ?? "Unknown",
                    pInfo?.ImageUrl,
                    g.Sum(i => i.Quantity),
                    g.Sum(i => i.Quantity * i.UnitPrice)
                );
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(10)
            .ToList();

        // Growth trends per month (month-over-month)
        var growthTrends = new List<GrowthTrendDto>();
        for (int m = 1; m <= 12; m++)
        {
            var mStart = new DateTime(year, m, 1, 0, 0, 0, DateTimeKind.Utc);
            var mEnd = mStart.AddMonths(1);
            var pStart = mStart.AddMonths(-1);

            var currMOrders = orders.Where(o => o.CreatedAtUtc >= mStart && o.CreatedAtUtc < mEnd).ToList();
            var prevMOrders = orders.Where(o => o.CreatedAtUtc >= pStart && o.CreatedAtUtc < mStart).ToList();

            var currR = currMOrders.Sum(o => o.TotalAmount);
            var prevR = prevMOrders.Sum(o => o.TotalAmount);
            var currC = currMOrders.Count;
            var prevC = prevMOrders.Count;

            var rGrowth = prevR > 0 ? Math.Round(((currR - prevR) / prevR) * 100, 2) : currR > 0 ? 100m : 0m;
            var oGrowth = prevC > 0 ? Math.Round(((decimal)(currC - prevC) / prevC) * 100, 2) : currC > 0 ? 100m : 0m;

            growthTrends.Add(new GrowthTrendDto(
                CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(m),
                rGrowth, oGrowth
            ));
        }

        return new YearlyReportDto(
            year, currencySymbol,
            totalRevenue, totalOrderCount, avgOrderValue,
            monthlyBreakdown, topProducts, growthTrends
        );
    }
}
