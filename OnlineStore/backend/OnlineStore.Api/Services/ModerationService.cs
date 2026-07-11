using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class ModerationService : IModerationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ModerationService> _logger;

    public ModerationService(ApplicationDbContext db, ILogger<ModerationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<object> GetFlaggedContentAsync(string? contentType = null)
    {
        var query = _db.ModerationFlags.AsQueryable();
        if (!string.IsNullOrWhiteSpace(contentType))
            query = query.Where(f => f.ContentType == contentType);

        var flags = await query.OrderByDescending(f => f.CreatedAtUtc).ToListAsync();

        return new
        {
            TotalFlags = flags.Count,
            Flags = flags.Select(f => new
            {
                f.ContentId, f.ContentType, f.Description, f.FlagType, f.Severity, f.CreatedAtUtc
            })
        };
    }

    public async Task<object> GetReviewModerationAsync()
    {
        var flags = new List<object>();

        var reviews = await _db.Reviews
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        var userReviewCounts = reviews
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (userId, userReviews) in userReviewCounts)
        {
            // Same user reviewing many products in short timeframe
            if (userReviews.Count >= 5)
            {
                var timeSpan = userReviews.Max(r => r.CreatedAtUtc) - userReviews.Min(r => r.CreatedAtUtc);
                if (timeSpan.TotalDays < 1)
                {
                    flags.Add(new
                    {
                        ReviewId = userReviews.First().Id,
                        ProductId = userReviews.First().ProductId,
                        UserId = userId,
                        UserName = userReviews.First().UserName,
                        Rating = userReviews.First().Rating,
                        Flags = new List<string> { $"Rapid reviews: {userReviews.Count} reviews in {(int)timeSpan.TotalHours}h" }
                    });
                }
            }

            // Identical review text
            var textGroups = userReviews.GroupBy(r => r.Comment?.Trim().ToLowerInvariant());
            foreach (var group in textGroups.Where(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key)))
            {
                foreach (var review in group)
                {
                    flags.Add(new
                    {
                        ReviewId = review.Id,
                        ProductId = review.ProductId,
                        UserId = userId,
                        UserName = review.UserName,
                        Comment = review.Comment,
                        Rating = review.Rating,
                        Flags = new List<string> { "Identical review text posted across multiple products" }
                    });
                }
            }
        }

        // Suspicious pattern: all 5-star reviews from new accounts on same store
        var storeReviews = reviews
            .Where(r => r.Rating == 5)
            .GroupBy(r => r.Product?.StoreUserId)
            .Where(g => g.Count() >= 5);

        foreach (var group in storeReviews)
        {
            var newAccountReviews = new List<Review>();
            foreach (var r in group)
            {
                var user = await _db.Users.FindAsync(r.UserId);
                if (user != null && (DateTime.UtcNow - user.CreatedAtUtc).TotalDays < 7)
                    newAccountReviews.Add(r);
            }

            if (newAccountReviews.Count >= 3)
            {
                flags.Add(new
                {
                    ReviewId = newAccountReviews.First().Id,
                    ProductId = newAccountReviews.First().ProductId,
                    UserId = string.Join(",", newAccountReviews.Select(r => r.UserId).Distinct()),
                    Flags = new List<string> { $"{newAccountReviews.Count} five-star reviews from accounts <7 days old on store {group.Key}" }
                });
            }
        }

        // Persist moderation flags
        foreach (var flag in flags)
        {
            var ft = flag.GetType();
            var reviewId = (int)ft.GetProperty("ReviewId")!.GetValue(flag)!;

            _db.ModerationFlags.Add(new ModerationFlag
            {
                ContentType = "review",
                ContentId = reviewId,
                FlagType = "suspicious_review_pattern",
                Description = string.Join("; ", (List<string>)ft.GetProperty("Flags")!.GetValue(flag)!),
                Severity = "medium",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return new { TotalFlags = flags.Count, Flags = flags };
    }

    public async Task<object> GetPricingAnomaliesAsync()
    {
        var anomalies = new List<object>();
        var categories = await _db.Categories.ToListAsync();

        foreach (var cat in categories)
        {
            var avgPrice = await _db.Products
                .Where(p => p.CategoryId == cat.Id && p.Status == ProductStatus.Approved)
                .AverageAsync(p => (decimal?)p.Price) ?? 0;

            if (avgPrice <= 0) continue;

            var outliers = await _db.Products
                .Where(p => p.CategoryId == cat.Id && p.Status == ProductStatus.Approved)
                .ToListAsync();

            foreach (var product in outliers)
            {
                var ratio = (double)(product.Price / avgPrice);
                if (ratio > 10 || ratio < 0.1)
                {
                    anomalies.Add(new
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Price = product.Price,
                        CategoryAverage = avgPrice,
                        Ratio = Math.Round(ratio, 1),
                        Severity = ratio > 10 ? "high" : "medium"
                    });
                }
            }
        }

        // Check for placeholder content
        var placeholderProducts = await _db.Products
            .Where(p => p.Status == ProductStatus.Approved &&
                (string.IsNullOrWhiteSpace(p.Description) ||
                 p.Description == "test" ||
                 p.Description == "asdf" ||
                 p.Description!.Length < 10))
            .Select(p => new { p.Id, p.Name, p.Description, p.Price })
            .ToListAsync();

        foreach (var pp in placeholderProducts)
        {
            _db.ModerationFlags.Add(new ModerationFlag
            {
                ContentType = "product",
                ContentId = pp.Id,
                FlagType = "inappropriate_content",
                Description = $"Product '{pp.Name}' has no meaningful description",
                Severity = "low",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return new
        {
            PricingAnomalies = anomalies,
            PlaceholderProducts = placeholderProducts
        };
    }
}
