using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Helpers;
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
    //  Helper structures for unified querying
    // ═══════════════════════════════════════════════════
    private class PeriodAnalyticsData
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public Dictionary<int, ProductSalesDto> ProductSales { get; set; } = new();
        public Dictionary<string, int> CustomerOrderCounts { get; set; } = new();
        public Dictionary<DateTime, (decimal Revenue, int OrderCount)> DailySales { get; set; } = new();
    }

    private async Task<PeriodAnalyticsData> LoadPeriodAnalyticsDataAsync(string storeUserId, DateTime start, DateTime endExclusive)
    {
        var data = new PeriodAnalyticsData();

        // 1. Load aggregated daily records in the date range
        var dailyRecords = await _db.StoreAnalyticsDailies
            .AsNoTracking()
            .Where(d => d.StoreUserId == storeUserId && d.Date >= start && d.Date < endExclusive)
            .ToListAsync();

        var aggregatedDates = new HashSet<DateTime>(dailyRecords.Select(r => r.Date.Date));

        // Combine aggregated daily records
        foreach (var row in dailyRecords)
        {
            var dateKey = row.Date.Date;
            data.TotalRevenue += row.TotalSales;
            data.TotalOrders += row.TotalOrders;
            
            data.DailySales[dateKey] = (row.TotalSales, row.TotalOrders);

            // Combine product sales
            var prodSales = JsonSerializer.Deserialize<List<ProductSalesDto>>(row.ProductSalesJson) ?? new();
            foreach (var ps in prodSales)
            {
                if (data.ProductSales.TryGetValue(ps.ProductId, out var existing))
                {
                    existing.Quantity += ps.Quantity;
                    existing.Revenue += ps.Revenue;
                }
                else
                {
                    data.ProductSales[ps.ProductId] = new ProductSalesDto
                    {
                        ProductId = ps.ProductId,
                        Quantity = ps.Quantity,
                        Revenue = ps.Revenue
                    };
                }
            }

            // Combine customer order counts
            var custOrders = JsonSerializer.Deserialize<Dictionary<string, int>>(row.CustomersJson) ?? new();
            foreach (var co in custOrders)
            {
                if (data.CustomerOrderCounts.TryGetValue(co.Key, out var existingCount))
                {
                    data.CustomerOrderCounts[co.Key] = existingCount + co.Value;
                }
                else
                {
                    data.CustomerOrderCounts[co.Key] = co.Value;
                }
            }
        }

        // 2. Load raw events for dates within the range that are NOT yet aggregated
        var rawEvents = await _db.StoreAnalyticsEvents
            .AsNoTracking()
            .Where(e => e.StoreUserId == storeUserId && e.CreatedAtUtc >= start && e.CreatedAtUtc < endExclusive)
            .ToListAsync();

        var unaggregatedEvents = rawEvents
            .Where(e => !aggregatedDates.Contains(e.CreatedAtUtc.Date))
            .ToList();

        if (unaggregatedEvents.Count > 0)
        {
            // Group by Date to populate DailySales
            var eventsByDate = unaggregatedEvents.GroupBy(e => e.CreatedAtUtc.Date);
            foreach (var dateGroup in eventsByDate)
            {
                var date = dateGroup.Key;
                var dateEvents = dateGroup.ToList();
                var dateRevenue = dateEvents.Sum(e => e.TotalAmount);
                var dateOrders = dateEvents.Select(e => e.OrderId).Distinct().Count();
                
                if (data.DailySales.TryGetValue(date, out var existingVal))
                {
                    data.DailySales[date] = (existingVal.Revenue + dateRevenue, existingVal.OrderCount + dateOrders);
                }
                else
                {
                    data.DailySales[date] = (dateRevenue, dateOrders);
                }

                data.TotalRevenue += dateRevenue;
                data.TotalOrders += dateOrders;
            }

            // Combine product sales from raw events
            var eventsByProduct = unaggregatedEvents.GroupBy(e => e.ProductId);
            foreach (var prodGroup in eventsByProduct)
            {
                var productId = prodGroup.Key;
                var qty = prodGroup.Sum(e => e.Quantity);
                var rev = prodGroup.Sum(e => e.TotalAmount);

                if (data.ProductSales.TryGetValue(productId, out var existing))
                {
                    existing.Quantity += qty;
                    existing.Revenue += rev;
                }
                else
                {
                    data.ProductSales[productId] = new ProductSalesDto
                    {
                        ProductId = productId,
                        Quantity = qty,
                        Revenue = rev
                    };
                }
            }

            // Combine customer order counts from raw events
            var eventsByCustomer = unaggregatedEvents.GroupBy(e => e.UserId);
            foreach (var custGroup in eventsByCustomer)
            {
                var userId = custGroup.Key;
                var orderCount = custGroup.Select(e => e.OrderId).Distinct().Count();

                if (data.CustomerOrderCounts.TryGetValue(userId, out var existingCount))
                {
                    data.CustomerOrderCounts[userId] = existingCount + orderCount;
                }
                else
                {
                    data.CustomerOrderCounts[userId] = orderCount;
                }
            }
        }

        return data;
    }

    // ═══════════════════════════════════════════════════
    //  Full Dashboard
    // ═══════════════════════════════════════════════════

    public async Task<FullAnalyticsDto> GetFullAnalyticsAsync(string storeUserId, DateTime? startDate, DateTime? endDate)
    {
        var now = DateTime.UtcNow;
        var rangeStart = (startDate ?? now.AddYears(-1)).Date;
        var selectedEnd = endDate ?? now;
        var rangeEndExclusive = selectedEnd.TimeOfDay == TimeSpan.Zero
            ? selectedEnd.Date.AddDays(1)
            : selectedEnd;

        if (rangeEndExclusive <= rangeStart)
        {
            rangeEndExclusive = rangeStart.AddDays(1);
        }

        var rangeEndDateExclusive = rangeEndExclusive.TimeOfDay == TimeSpan.Zero
            ? rangeEndExclusive.Date
            : rangeEndExclusive.Date.AddDays(1);

        // Load store profile (for currency, threshold)
        var shopProfile = await _db.ShopProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == storeUserId);

        var storeUser = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == storeUserId)
            .Select(u => new { u.Country })
            .FirstOrDefaultAsync();

        var currencySymbol = CurrencyHelper.GetCurrencySymbol(storeUser?.Country);
        var lowStockThreshold = shopProfile?.LowStockThreshold ?? 10;

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

        // ── Load analytics data for both current and previous periods ──
        var currData = await LoadPeriodAnalyticsDataAsync(storeUserId, rangeStart, rangeEndExclusive);

        var periodLength = rangeEndExclusive - rangeStart;
        if (periodLength <= TimeSpan.Zero)
        {
            periodLength = TimeSpan.FromDays(1);
        }
        var prevPeriodStart = rangeStart - periodLength;
        var prevPeriodEnd = rangeStart;

        var prevData = await LoadPeriodAnalyticsDataAsync(storeUserId, prevPeriodStart, prevPeriodEnd);

        // ═══ Section 1: Summary Cards ═══
        var totalRevenue = currData.TotalRevenue;
        var totalOrders = currData.TotalOrders;
        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0m;

        var daysInPeriod = Math.Max(1, (rangeEndDateExclusive - rangeStart.Date).Days);
        var averageDailyRevenue = totalRevenue / daysInPeriod;

        var monthsInPeriod = Math.Max(1, ((rangeEndDateExclusive.Year - rangeStart.Year) * 12) + rangeEndDateExclusive.Month - rangeStart.Month);
        var averageMonthlyRevenue = totalRevenue / monthsInPeriod;

        var peakDayRevenue = currData.DailySales.Values
            .Select(v => v.Revenue)
            .DefaultIfEmpty(0m)
            .Max();

        var summary = new AnalyticsSummaryDto(
            totalRevenue,
            Math.Round(averageDailyRevenue, 2),
            Math.Round(averageMonthlyRevenue, 2),
            Math.Round(peakDayRevenue, 2),
            totalOrders,
            Math.Round(avgOrderValue, 2),
            currencySymbol
        );

        // ═══ Section 2: Sales Charts ═══
        // Daily sales chart points
        var selectedDayCount = Math.Max(1, (rangeEndDateExclusive - rangeStart.Date).Days);
        var dailyStart = selectedDayCount <= 31
            ? rangeStart.Date
            : rangeEndDateExclusive.AddDays(-31);
        var dailyDayCount = Math.Max(1, (rangeEndDateExclusive - dailyStart).Days);

        var dailySales = Enumerable.Range(0, dailyDayCount)
            .Select(i => dailyStart.AddDays(i))
            .Select(day =>
            {
                currData.DailySales.TryGetValue(day.Date, out var val);
                return new DailySalesPointDto(
                    day.ToString("MMM dd"),
                    val.Revenue,
                    val.OrderCount
                );
            })
            .ToList();

        // Monthly sales chart points
        var monthChartStart = new DateTime(rangeStart.Year, rangeStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastSelectedDay = rangeEndDateExclusive.AddDays(-1);
        var monthChartEnd = new DateTime(lastSelectedDay.Year, lastSelectedDay.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        var monthCount = Math.Max(1, ((monthChartEnd.Year - monthChartStart.Year) * 12) + monthChartEnd.Month - monthChartStart.Month);
        if (monthCount > 12)
        {
            monthChartStart = monthChartEnd.AddMonths(-12);
            monthCount = 12;
        }

        var monthlySales = Enumerable.Range(0, monthCount)
            .Select(i =>
            {
                var mStart = monthChartStart.AddMonths(i);
                var mEnd = mStart.AddMonths(1);

                // Sum daily sales falling in this month
                var mRevenue = currData.DailySales
                    .Where(kvp => kvp.Key >= mStart && kvp.Key < mEnd)
                    .Sum(kvp => kvp.Value.Revenue);
                var mOrders = currData.DailySales
                    .Where(kvp => kvp.Key >= mStart && kvp.Key < mEnd)
                    .Sum(kvp => kvp.Value.OrderCount);

                return new MonthlySalesPointDto(
                    mStart.ToString("MMM yyyy"),
                    mRevenue,
                    mOrders
                );
            })
            .ToList();

        // ═══ Section 3: Top Selling Products ═══
        var productSales = currData.ProductSales.Values
            .Select(p =>
            {
                productDict.TryGetValue(p.ProductId, out var product);
                return new TopSellingProductDto(
                    p.ProductId,
                    product?.Name ?? "Unknown",
                    product?.ImageUrl,
                    p.Quantity,
                    p.Revenue
                );
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(10)
            .ToList();

        // ═══ Section 4: Product Previews ═══
        var productPreviews = allProducts
            .Where(p => p.Status == ProductStatus.Approved)
            .Select(p => new ProductPreviewDto(
                p.Id,
                p.Name,
                p.ImageUrl,
                p.StockQuantity,
                p.Price,
                currData.ProductSales.TryGetValue(p.Id, out var sales) ? sales.Quantity : 0
            ))
            .OrderByDescending(p => p.TotalSoldCount)
            .Take(12)
            .ToList();

        // ═══ Section 5: Growth Percentage ═══
        var prevRevenue = prevData.TotalRevenue;
        var currRevenue = currData.TotalRevenue;
        var prevOrderCount = prevData.TotalOrders;
        var currOrderCount = currData.TotalOrders;

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
        var trendingProducts = currData.ProductSales
            .Select(kvp =>
            {
                var productId = kvp.Key;
                var currentQty = kvp.Value.Quantity;
                prevData.ProductSales.TryGetValue(productId, out var prevProductSales);
                var prevQty = prevProductSales?.Quantity ?? 0;

                var growthPct = prevQty > 0
                    ? Math.Round(((decimal)(currentQty - prevQty) / prevQty) * 100, 2)
                    : currentQty > 0 ? 100m : 0m;

                productDict.TryGetValue(productId, out var product);
                return new TrendingProductDto(
                    productId,
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
        var totalCustomers = currData.CustomerOrderCounts.Count;
        var returningCount = currData.CustomerOrderCounts.Count(kvp => kvp.Value > 1);
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
            currRevenue, prevRevenue
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
                    $"Revenue decreased by {Math.Abs(revChange):F0}% compared to the previous period. Consider running promotions.",
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

        var currencySymbol = CurrencyHelper.GetCurrencySymbol(storeUser?.Country);

        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var prevMonthStart = monthStart.AddMonths(-1);

        var currMonthData = await LoadPeriodAnalyticsDataAsync(storeUserId, monthStart, monthEnd);
        var prevMonthData = await LoadPeriodAnalyticsDataAsync(storeUserId, prevMonthStart, monthStart);

        var totalRevenue = currMonthData.TotalRevenue;
        var totalOrderCount = currMonthData.TotalOrders;
        var avgOrderValue = totalOrderCount > 0 ? Math.Round(totalRevenue / totalOrderCount, 2) : 0m;

        var prevRevenue = prevMonthData.TotalRevenue;
        var prevOrderCount = prevMonthData.TotalOrders;

        var revenueGrowth = prevRevenue > 0
            ? Math.Round(((totalRevenue - prevRevenue) / prevRevenue) * 100, 2)
            : totalRevenue > 0 ? 100m : 0m;
        var orderGrowth = prevOrderCount > 0
            ? Math.Round(((decimal)(totalOrderCount - prevOrderCount) / prevOrderCount) * 100, 2)
            : totalOrderCount > 0 ? 100m : 0m;

        // Product names lookup
        var productIdsInMonth = currMonthData.ProductSales.Keys.ToList();

        var productNames = await _db.Products
            .AsNoTracking()
            .Where(p => productIdsInMonth.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.ImageUrl })
            .ToDictionaryAsync(p => p.Id);

        var topProducts = currMonthData.ProductSales.Values
            .Select(p =>
            {
                productNames.TryGetValue(p.ProductId, out var pInfo);
                return new TopSellingProductDto(
                    p.ProductId,
                    pInfo?.Name ?? "Unknown",
                    pInfo?.ImageUrl,
                    p.Quantity,
                    p.Revenue
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
                currMonthData.DailySales.TryGetValue(day.Date, out var val);
                return new DailySalesPointDto(
                    day.ToString("MMM dd"),
                    val.Revenue,
                    val.OrderCount
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

        var currencySymbol = CurrencyHelper.GetCurrencySymbol(storeUser?.Country);

        var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);
        var prevYearStart = yearStart.AddYears(-1);

        var currYearData = await LoadPeriodAnalyticsDataAsync(storeUserId, yearStart, yearEnd);
        var prevYearData = await LoadPeriodAnalyticsDataAsync(storeUserId, prevYearStart, yearStart);

        var totalRevenue = currYearData.TotalRevenue;
        var totalOrderCount = currYearData.TotalOrders;
        var avgOrderValue = totalOrderCount > 0 ? Math.Round(totalRevenue / totalOrderCount, 2) : 0m;

        // Monthly breakdown
        var monthlyBreakdown = Enumerable.Range(1, 12)
            .Select(m =>
            {
                var mStart = new DateTime(year, m, 1, 0, 0, 0, DateTimeKind.Utc);
                var mEnd = mStart.AddMonths(1);
                var mRevenue = currYearData.DailySales.Where(kvp => kvp.Key >= mStart && kvp.Key < mEnd).Sum(kvp => kvp.Value.Revenue);
                var mOrders = currYearData.DailySales.Where(kvp => kvp.Key >= mStart && kvp.Key < mEnd).Sum(kvp => kvp.Value.OrderCount);
                
                return new MonthlySalesPointDto(
                    CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(m) + " " + year,
                    mRevenue,
                    mOrders
                );
            })
            .ToList();

        // Top products for the year
        var productIdsInYear = currYearData.ProductSales.Keys.ToList();

        var productNames = await _db.Products
            .AsNoTracking()
            .Where(p => productIdsInYear.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.ImageUrl })
            .ToDictionaryAsync(p => p.Id);

        var topProducts = currYearData.ProductSales.Values
            .Select(p =>
            {
                productNames.TryGetValue(p.ProductId, out var pInfo);
                return new TopSellingProductDto(
                    p.ProductId,
                    pInfo?.Name ?? "Unknown",
                    pInfo?.ImageUrl,
                    p.Quantity,
                    p.Revenue
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

            var currR = currYearData.DailySales.Where(kvp => kvp.Key >= mStart && kvp.Key < mEnd).Sum(kvp => kvp.Value.Revenue);
            var currC = currYearData.DailySales.Where(kvp => kvp.Key >= mStart && kvp.Key < mEnd).Sum(kvp => kvp.Value.OrderCount);

            decimal prevR = 0;
            int prevC = 0;
            if (m == 1)
            {
                prevR = prevYearData.DailySales.Where(kvp => kvp.Key >= pStart && kvp.Key < mStart).Sum(kvp => kvp.Value.Revenue);
                prevC = prevYearData.DailySales.Where(kvp => kvp.Key >= pStart && kvp.Key < mStart).Sum(kvp => kvp.Value.OrderCount);
            }
            else
            {
                prevR = currYearData.DailySales.Where(kvp => kvp.Key >= pStart && kvp.Key < mStart).Sum(kvp => kvp.Value.Revenue);
                prevC = currYearData.DailySales.Where(kvp => kvp.Key >= pStart && kvp.Key < mStart).Sum(kvp => kvp.Value.OrderCount);
            }

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
