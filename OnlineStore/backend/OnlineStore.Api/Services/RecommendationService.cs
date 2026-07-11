using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class RecommendationService : IRecommendationService
{
    private readonly ApplicationDbContext _db;
    private const int MaxResults = 5;

    public RecommendationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<RecommendationResultDto> GetRecommendationsAsync(string userId, int? productId = null)
    {
        var result = new RecommendationResultDto();

        // Fetch purchased IDs once to avoid redundant DB queries in sub-methods
        var purchasedIds = await GetUserPurchasedProductIdsAsync(userId);

        // Run strategies sequentially because DbContext is not thread-safe
        result.BasedOnHistory = await GetBasedOnHistoryAsync(userId, purchasedIds);
        
        result.FrequentlyBoughtTogether = productId.HasValue
            ? await GetFrequentlyBoughtTogetherAsync(productId.Value)
            : new List<RecommendedProductDto>();
            
        result.RelatedByCategory = productId.HasValue
            ? await GetRelatedByCategoryAsync(productId.Value)
            : await GetRelatedByCategoryFromHistoryAsync(userId, purchasedIds);
            
        result.SmartUpsells = productId.HasValue
            ? await GetSmartUpsellsAsync(productId.Value)
            : await GetSmartUpsellsFromHistoryAsync(userId, purchasedIds);

        result.TrendingNow = await GetTrendingNowAsync(purchasedIds);
        result.Promotions = await GetSeasonalPromotionsAsync(purchasedIds);

        return result;
    }

    /// <summary>
    /// Products frequently purchased in the same order as the given product.
    /// Uses co-occurrence analysis across all orders.
    /// </summary>
    private async Task<List<RecommendedProductDto>> GetFrequentlyBoughtTogetherAsync(int productId)
    {
        // Single subquery: find co-purchased products without materializing orderIds in memory
        var coProducts = await _db.OrderItems
            .Where(oi => _db.OrderItems
                .Where(inner => inner.ProductId == productId)
                .Select(inner => inner.OrderId)
                .Contains(oi.OrderId) && oi.ProductId != productId && oi.ProductId != null)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key!.Value, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(MaxResults)
            .ToListAsync();

        if (!coProducts.Any())
            return new List<RecommendedProductDto>();

        var productIds = coProducts.Select(c => c.ProductId).ToList();

        var products = await GetProductDetailsAsync(productIds);

        return products
            .Select(p => MapToDto(p, "Frequently bought together"))
            .ToList();
    }

    /// <summary>
    /// Products in the same category as the given product.
    /// </summary>
    private async Task<List<RecommendedProductDto>> GetRelatedByCategoryAsync(int productId)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return new List<RecommendedProductDto>();

        var products = await GetProductDetailsAsync(null,
            query: q => q.Where(p =>
                p.CategoryId == product.CategoryId &&
                p.Id != productId &&
                p.Status == ProductStatus.Approved &&
                p.StockQuantity > 0)
            .OrderByDescending(p => p.StockQuantity)
            .Take(MaxResults));

        return products
            .Select(p => MapToDto(p, $"Related in {p.CategoryName}"))
            .ToList();
    }

    /// <summary>
    /// Related products inferred from the user's most-purchased categories.
    /// </summary>
    private async Task<List<RecommendedProductDto>> GetRelatedByCategoryFromHistoryAsync(string userId, HashSet<int> purchasedIds)
    {
        // Get the user's top purchased category IDs
        var topCategoryIds = await GetUserTopCategoryIdsAsync(userId, 3);
        if (!topCategoryIds.Any()) return new List<RecommendedProductDto>();

        var products = await GetProductDetailsAsync(null,
            query: q => q.Where(p =>
                topCategoryIds.Contains(p.CategoryId) &&
                !purchasedIds.Contains(p.Id) &&
                p.Status == ProductStatus.Approved &&
                p.StockQuantity > 0)
            .OrderByDescending(p => p.StockQuantity)
            .Take(MaxResults));

        return products
            .Select(p => MapToDto(p, $"Popular in {p.CategoryName}"))
            .ToList();
    }

    /// <summary>
    /// Higher-tier products in the same category (price > current product price).
    /// </summary>
    private async Task<List<RecommendedProductDto>> GetSmartUpsellsAsync(int productId)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return new List<RecommendedProductDto>();

        var products = await GetProductDetailsAsync(null,
            query: q => q.Where(p =>
                p.CategoryId == product.CategoryId &&
                p.Id != productId &&
                p.Price > product.Price &&
                p.Status == ProductStatus.Approved &&
                p.StockQuantity > 0)
            .OrderBy(p => p.Price)
            .Take(MaxResults));

        return products
            .Select(p => MapToDto(p, "Premium alternative"))
            .ToList();
    }

    /// <summary>
    /// Smart upsells inferred from the user's average spend per category.
    /// Suggests products slightly above their typical price point.
    /// </summary>
    private async Task<List<RecommendedProductDto>> GetSmartUpsellsFromHistoryAsync(string userId, HashSet<int> purchasedIds)
    {
        // Calculate user's average order item price
        var avgPrice = await _db.OrderItems
            .Where(oi => oi.Order.UserId == userId)
            .Select(oi => (decimal?)oi.UnitPrice)
            .AverageAsync();

        if (avgPrice == null) return new List<RecommendedProductDto>();

        // Suggest products between 1x and 2x their average spend
        var lowerBound = avgPrice.Value;
        var upperBound = avgPrice.Value * 2;

        var products = await GetProductDetailsAsync(null,
            query: q => q.Where(p =>
                p.Price >= lowerBound &&
                p.Price <= upperBound &&
                !purchasedIds.Contains(p.Id) &&
                p.Status == ProductStatus.Approved &&
                p.StockQuantity > 0)
            .OrderBy(p => p.Price)
            .Take(MaxResults));

        return products
            .Select(p => MapToDto(p, "Upgrade pick for you"))
            .ToList();
    }

    /// <summary>
    /// Products from categories the user buys most, that they haven't bought yet.
    /// </summary>
    private async Task<List<RecommendedProductDto>> GetBasedOnHistoryAsync(string userId, HashSet<int> purchasedIds)
    {
        var topCategoryIds = await GetUserTopCategoryIdsAsync(userId, 5);
        if (!topCategoryIds.Any()) return new List<RecommendedProductDto>();

        var products = await GetProductDetailsAsync(null,
            query: q => q.Where(p =>
                topCategoryIds.Contains(p.CategoryId) &&
                !purchasedIds.Contains(p.Id) &&
                p.Status == ProductStatus.Approved &&
                p.StockQuantity > 0)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(MaxResults));

        return products
            .Select(p => MapToDto(p, "Based on your purchase history"))
            .ToList();
    }

    /// <summary>
    /// Products with the most orders in the last 7 days (trending).
    /// </summary>
    private async Task<List<RecommendedProductDto>> GetTrendingNowAsync(HashSet<int> purchasedIds)
    {
        var since = DateTime.UtcNow.AddDays(-7);

        var trending = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order!.CreatedAtUtc >= since &&
                         oi.Order.Status != OrderStatus.Cancelled &&
                         oi.ProductId != null &&
                         oi.Product!.Status == ProductStatus.Approved &&
                         oi.Product.StockQuantity > 0)
            .GroupBy(oi => new { oi.ProductId, oi.Product!.Name, oi.Product.Price, oi.Product.ImageUrl,
                CategoryName = oi.Product.Category!.Name, oi.Product.StockQuantity })
            .Select(g => new
            {
                ProductId = g.Key.ProductId!.Value,
                ProductName = g.Key.Name,
                Price = g.Key.Price,
                StockQuantity = g.Key.StockQuantity,
                ImageUrl = g.Key.ImageUrl,
                CategoryName = g.Key.CategoryName,
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(MaxResults)
            .ToListAsync();

        return trending.Select(p => new RecommendedProductDto
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Price = p.Price,
            Stock = p.StockQuantity,
            ImageUrl = p.ImageUrl,
            CategoryName = p.CategoryName,
            Reason = "Trending this week"
        }).ToList();
    }

    /// <summary>
    /// Products from categories trending in the current season (seasonal/promotional).
    /// </summary>
    private async Task<List<RecommendedProductDto>> GetSeasonalPromotionsAsync(HashSet<int> purchasedIds)
    {
        var (seasonStart, seasonEnd) = GetCurrentSeasonRange();

        // Find categories with the most orders in the current season
        var topSeasonCategoryIds = await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order!.CreatedAtUtc >= seasonStart &&
                         oi.Order.CreatedAtUtc <= seasonEnd &&
                         oi.Order.Status != OrderStatus.Cancelled &&
                         oi.ProductId != null)
            .Join(_db.Products, oi => oi.ProductId, p => p.Id, (oi, p) => p.CategoryId)
            .GroupBy(catId => catId)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key)
            .ToListAsync();

        if (!topSeasonCategoryIds.Any())
            return new List<RecommendedProductDto>();

        var products = await GetProductDetailsAsync(null,
            query: q => q.Where(p =>
                topSeasonCategoryIds.Contains(p.CategoryId) &&
                !purchasedIds.Contains(p.Id) &&
                p.Status == ProductStatus.Approved &&
                p.StockQuantity > 0)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(MaxResults));

        return products
            .Select(p => MapToDto(p, $"Seasonal pick — popular this season"))
            .ToList();
    }

    /// <summary>
    /// Determine the current meteorological season date range.
    /// </summary>
    private static (DateTime start, DateTime end) GetCurrentSeasonRange()
    {
        var now = DateTime.UtcNow;
        var year = now.Year;

        return now.Month switch
        {
            3 or 4 or 5 => (new DateTime(year, 3, 1), new DateTime(year, 5, 31)),          // Spring
            6 or 7 or 8 => (new DateTime(year, 6, 1), new DateTime(year, 8, 31)),          // Summer
            9 or 10 or 11 => (new DateTime(year, 9, 1), new DateTime(year, 11, 30)),       // Fall
            _ => (new DateTime(year, 12, 1), new DateTime(year, 12, 31))                   // Winter
        };
    }

    // ── Shared helpers ──────────────────────────────────────────────────

    private async Task<List<int>> GetUserTopCategoryIdsAsync(string userId, int count)
    {
        return await _db.OrderItems
            .Where(oi => oi.Order.UserId == userId && oi.ProductId != null)
            .Join(_db.Products, oi => oi.ProductId, p => p.Id, (oi, p) => p.CategoryId)
            .GroupBy(catId => catId)
            .OrderByDescending(g => g.Count())
            .Take(count)
            .Select(g => g.Key)
            .ToListAsync();
    }

    private async Task<HashSet<int>> GetUserPurchasedProductIdsAsync(string userId)
    {
        var ids = await _db.OrderItems
            .Where(oi => oi.Order.UserId == userId && oi.ProductId != null)
            .Select(oi => oi.ProductId!.Value)
            .Distinct()
            .ToListAsync();

        return ids.ToHashSet();
    }

    private record ProductDetail(
        int Id, string Name, decimal Price, int StockQuantity, string? ImageUrl,
        int CategoryId, string CategoryName, string StoreName);

    private async Task<List<ProductDetail>> GetProductDetailsAsync(
        List<int>? productIds = null,
        Func<IQueryable<Product>, IQueryable<Product>>? query = null)
    {
        IQueryable<Product> baseQuery = _db.Products
            .Include(p => p.Category)
            .AsNoTracking();

        if (productIds != null)
        {
            baseQuery = baseQuery.Where(p => productIds.Contains(p.Id)
                && p.Status == ProductStatus.Approved
                && p.StockQuantity > 0);
        }

        if (query != null)
        {
            baseQuery = query(baseQuery);
        }

        var raw = await (from p in baseQuery
                         join s in _db.ShopProfiles on p.StoreUserId equals s.UserId into stores
                         from s in stores.DefaultIfEmpty()
                         select new ProductDetail(
                             p.Id, p.Name, p.Price, p.StockQuantity, p.ImageUrl,
                             p.CategoryId, p.Category.Name, s != null ? s.ShopName : "Unknown Store"))
                         .ToListAsync();

        return raw;
    }

    private static RecommendedProductDto MapToDto(ProductDetail p, string reason) => new()
    {
        ProductId = p.Id,
        ProductName = p.Name,
        Price = p.Price,
        Stock = p.StockQuantity,
        ImageUrl = p.ImageUrl,
        CategoryName = p.CategoryName,
        StoreName = p.StoreName,
        Reason = reason
    };
}
