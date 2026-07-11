using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class CustomerInsightService : ICustomerInsightService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CustomerInsightService> _logger;

    public CustomerInsightService(ApplicationDbContext db, ILogger<CustomerInsightService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<object> GetCustomerBehaviorAnalysisAsync(string storeUserId)
    {
        var now = DateTime.UtcNow;
        var yearAgo = now.AddYears(-1);

        // All orders for this store
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.StoreUserId == storeUserId &&
                        o.CreatedAtUtc >= yearAgo &&
                        o.Status != OrderStatus.Cancelled)
            .Select(o => new { o.UserId, o.CreatedAtUtc, o.TotalAmount })
            .ToListAsync();

        if (!orders.Any())
        {
            return new
            {
                TotalOrders = 0,
                TotalCustomers = 0,
                NewCustomers = 0,
                ReturningCustomers = 0,
                AvgOrderValue = 0m,
                PurchaseFrequencyDays = 0.0,
                Segments = new { }
            };
        }

        var customerGroups = orders.GroupBy(o => o.UserId).ToList();

        // New vs returning: customers whose first order was within last 30 days
        var firstOrderDates = customerGroups
            .Select(g => new { UserId = g.Key, FirstOrder = g.Min(o => o.CreatedAtUtc) })
            .ToList();

        var thirtyDaysAgo = now.AddDays(-30);
        var newCustomers = firstOrderDates.Count(f => f.FirstOrder >= thirtyDaysAgo);
        var returningCustomers = firstOrderDates.Count - newCustomers;

        // Purchase frequency
        var avgDaysBetweenOrders = customerGroups
            .Where(g => g.Count() > 1)
            .Select(g =>
            {
                var dates = g.Select(o => o.CreatedAtUtc).OrderBy(d => d).ToList();
                var gaps = dates.Zip(dates.Skip(1), (a, b) => (b - a).TotalDays);
                return gaps.Average();
            })
            .DefaultIfEmpty(0)
            .Average();

        // Average order value
        var avgOrderValue = orders.Average(o => o.TotalAmount);

        // Customer segments by total spend
        var customerSpend = customerGroups
            .Select(g => new
            {
                UserId = g.Key,
                TotalSpend = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .ToList();

        var highValue = customerSpend.Count(c => c.TotalSpend >= 500);
        var mediumValue = customerSpend.Count(c => c.TotalSpend >= 100 && c.TotalSpend < 500);
        var lowValue = customerSpend.Count(c => c.TotalSpend < 100);

        return new
        {
            Period = "Last 12 months",
            TotalOrders = orders.Count,
            TotalCustomers = customerGroups.Count,
            NewCustomers30Days = newCustomers,
            ReturningCustomers = returningCustomers,
            NewCustomerPercent = customerGroups.Count > 0
                ? Math.Round(newCustomers * 100.0 / customerGroups.Count, 1)
                : 0,
            AvgOrderValue = Math.Round(avgOrderValue, 2),
            AvgDaysBetweenOrders = Math.Round(avgDaysBetweenOrders, 1),
            CustomerSegments = new
            {
                HighValue = new { Count = highValue, Threshold = "$500+" },
                MediumValue = new { Count = mediumValue, Threshold = "$100 - $499" },
                LowValue = new { Count = lowValue, Threshold = "Under $100" }
            }
        };
    }

    public async Task<object> GetCustomerSatisfactionStatsAsync(string storeUserId)
    {
        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.StoreUserId == storeUserId)
            .Select(p => p.Id)
            .ToListAsync();

        if (!products.Any())
        {
            return new
            {
                TotalReviews = 0,
                AverageRating = 0.0,
                RatingDistribution = new { },
                CommonPositiveKeywords = new List<string>(),
                CommonComplaintKeywords = new List<string>(),
                SatisfactionTrend = "No data"
            };
        }

        var reviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => products.Contains(r.ProductId))
            .Select(r => new { r.Rating, r.Comment, r.CreatedAtUtc })
            .ToListAsync();

        if (!reviews.Any())
        {
            return new
            {
                TotalReviews = 0,
                AverageRating = 0.0,
                RatingDistribution = new { OneStar = 0, TwoStars = 0, ThreeStars = 0, FourStars = 0, FiveStars = 0 },
                CommonPositiveKeywords = new List<string>(),
                CommonComplaintKeywords = new List<string>(),
                SatisfactionTrend = "No reviews yet"
            };
        }

        var avgRating = reviews.Average(r => r.Rating);
        var distribution = new
        {
            OneStar = reviews.Count(r => r.Rating == 1),
            TwoStars = reviews.Count(r => r.Rating == 2),
            ThreeStars = reviews.Count(r => r.Rating == 3),
            FourStars = reviews.Count(r => r.Rating == 4),
            FiveStars = reviews.Count(r => r.Rating == 5)
        };

        // Simple keyword analysis from comments
        var complaintKeywords = new[] { "bad", "poor", "terrible", "awful", "horrible", "damage", "broken",
            "late", "slow", "wrong", "missing", "expired", "rotten", "disappointed", "çürük", "kötü",
            "geç", "bozuk", "eksi", "şikayet" };
        var praiseKeywords = new[] { "great", "excellent", "amazing", "good", "love", "perfect", "fresh",
            "fast", "delicious", "beautiful", "harika", "mükemmel", "güzel", "taze", "hızlı", "lezzetli" };

        var commentsWithText = reviews.Where(r => !string.IsNullOrWhiteSpace(r.Comment))
                                      .Select(r => r.Comment!.ToLowerInvariant())
                                      .ToList();

        var foundComplaints = complaintKeywords
            .Where(kw => commentsWithText.Any(c => c.Contains(kw)))
            .Distinct()
            .Take(5)
            .ToList();

        var foundPraise = praiseKeywords
            .Where(kw => commentsWithText.Any(c => c.Contains(kw)))
            .Distinct()
            .Take(5)
            .ToList();

        // Trend: compare last 3 months vs previous 3 months
        var now = DateTime.UtcNow;
        var recent3Months = now.AddMonths(-3);
        var prior3Months = now.AddMonths(-6);

        var recentAvg = reviews.Where(r => r.CreatedAtUtc >= recent3Months)
                               .DefaultIfEmpty()
                               .Average(r => r?.Rating ?? 0);

        var priorAvg = reviews.Where(r => r.CreatedAtUtc >= prior3Months && r.CreatedAtUtc < recent3Months)
                              .DefaultIfEmpty()
                              .Average(r => r?.Rating ?? 0);

        var trend = (recentAvg, priorAvg) switch
        {
            (0, _) => "Insufficient recent data",
            (_, 0) => "Baseline established — continue monitoring",
            var (r, p) when r > p => "Improving",
            var (r, p) when r < p => "Declining",
            _ => "Stable"
        };

        return new
        {
            TotalReviews = reviews.Count,
            AverageRating = Math.Round(avgRating, 2),
            RatingDistribution = distribution,
            CommonPositiveKeywords = foundPraise,
            CommonComplaintKeywords = foundComplaints,
            SatisfactionTrend = trend,
            Recent3MonthAvg = Math.Round(recentAvg, 2),
            Prior3MonthAvg = Math.Round(priorAvg, 2)
        };
    }

    public async Task<object> GetTopCustomersAsync(string storeUserId)
    {
        var now = DateTime.UtcNow;
        var yearAgo = now.AddYears(-1);

        var customerData = await _db.Orders
            .AsNoTracking()
            .Where(o => o.StoreUserId == storeUserId &&
                        o.CreatedAtUtc >= yearAgo &&
                        o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalSpent = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count(),
                LastOrderDate = g.Max(o => o.CreatedAtUtc),
                FirstOrderDate = g.Min(o => o.CreatedAtUtc)
            })
            .OrderByDescending(c => c.TotalSpent)
            .Take(20)
            .ToListAsync();

        if (!customerData.Any())
            return new { TopCustomers = new List<object>(), Count = 0 };

        // Get user names
        var userIds = customerData.Select(c => c.UserId).ToList();
        var users = await _db.Users.Cast<AppUser>()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToListAsync();

        var userLookup = users.ToDictionary(u => u.Id, u => u.FullName ?? u.Email ?? "Unknown");

        var topCustomers = customerData.Select(c => new
        {
            UserId = c.UserId,
            Name = userLookup.GetValueOrDefault(c.UserId, "Unknown"),
            TotalSpent = Math.Round(c.TotalSpent, 2),
            OrderCount = c.OrderCount,
            AvgOrderValue = c.OrderCount > 0 ? Math.Round(c.TotalSpent / c.OrderCount, 2) : 0,
            LastOrderDate = c.LastOrderDate.ToString("yyyy-MM-dd"),
            FirstOrderDate = c.FirstOrderDate.ToString("yyyy-MM-dd"),
            CustomerSince = (now - c.FirstOrderDate).Days / 30
        }).ToList();

        return new
        {
            Period = "Last 12 months",
            TopSpenders = topCustomers.Take(10),
            MostFrequent = topCustomers.OrderByDescending(c => c.OrderCount).Take(10),
            Count = customerData.Count
        };
    }
}
