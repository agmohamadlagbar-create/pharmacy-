using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

/// <summary>
/// Handles business logic for fetching store data (Products/Categories) with caching.
/// Depends on ICacheService (abstraction) to make it Redis-ready for production.
/// </summary>
public interface IStoreCacheService
{
    Task<PaginatedResponse<ProductDto>> GetApprovedProductsPaginatedAsync(int? categoryId, string? storeUserId, string? search = null, decimal? minPrice = null, decimal? maxPrice = null, string? country = null, string? city = null, int page = 1, int limit = 9);
    Task<List<CategoryDto>> GetTopLevelCategoriesAsync();
    Task<List<CategoryDto>> GetSubcategoriesAsync(int parentId);
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<List<AdBannerDto>> GetAdBannersAsync();
    
    // Invalidation methods to be called after Create/Update/Delete operations
    Task InvalidateProductsCacheAsync();
    Task InvalidateCategoriesCacheAsync();
    Task InvalidateAdsCacheAsync();
}

public class StoreCacheService : IStoreCacheService
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogger<StoreCacheService> _logger;
    
    private const string ApprovedProductsCacheKey = "store_approved_products_v3";
    private const string CategoriesCacheKey = "store_categories_all";
    private const string CategoryTreeCacheKey = "store_category_tree";
    private const string AdsCacheKey = "store_ads_all";

    private static readonly SemaphoreSlim _productsLock = new(1, 1);
    private static readonly SemaphoreSlim _categoriesLock = new(1, 1);
    private static readonly SemaphoreSlim _categoryTreeLock = new(1, 1);
    private static readonly SemaphoreSlim _adsLock = new(1, 1);

    public StoreCacheService(ApplicationDbContext db, ICacheService cache, ILogger<StoreCacheService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PaginatedResponse<ProductDto>> GetApprovedProductsPaginatedAsync(int? categoryId, string? storeUserId, string? search = null, decimal? minPrice = null, decimal? maxPrice = null, string? country = null, string? city = null, int page = 1, int limit = 9)
    {
        var allApprovedProducts = await _cache.GetAsync<List<ProductDto>>(ApprovedProductsCacheKey);

        if (allApprovedProducts == null)
        {
            await _productsLock.WaitAsync();
            try
            {
                // Double-check locking
                allApprovedProducts = await _cache.GetAsync<List<ProductDto>>(ApprovedProductsCacheKey);
                if (allApprovedProducts == null)
                {
                    var productsList = await _db.Products
                        .AsNoTracking()
                        .Include(p => p.Category)
                        .Where(p => p.Status == ProductStatus.Approved)
                        .OrderByDescending(p => p.CreatedAtUtc)
                        .ToListAsync();

                    var storeUserIds = productsList.Select(p => p.StoreUserId).Distinct().ToList();
                    
                    var shopInfo = await _db.ShopProfiles
                        .AsNoTracking()
                        .Where(s => storeUserIds.Contains(s.UserId))
                        .Select(s => new { s.UserId, s.ShopName })
                        .ToDictionaryAsync(s => s.UserId, s => s.ShopName);

                    var userInfo = await _db.Users
                        .AsNoTracking()
                        .Where(u => storeUserIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.Country, u.City })
                        .ToDictionaryAsync(u => u.Id, u => new { u.Country, u.City });

                    var summaries = await _db.Reviews
                        .AsNoTracking()
                        .GroupBy(r => r.ProductId)
                        .Select(g => new { 
                            ProductId = g.Key, 
                            Avg = g.Average(r => (double)r.Rating), 
                            Count = g.Count() 
                        })
                        .ToListAsync();

                    var reviewsSummary = summaries.ToDictionary(x => x.ProductId);

                    _logger.LogInformation("Found review summaries for {Count} products.", reviewsSummary.Count);

                    allApprovedProducts = productsList
                        .Select(p => {
                            var hasReview = reviewsSummary.TryGetValue(p.Id, out var summary);
                            var storeLoc = userInfo.GetValueOrDefault(p.StoreUserId);
                            return ToDto(
                                p, 
                                shopInfo.GetValueOrDefault(p.StoreUserId),
                                storeLoc?.Country,
                                storeLoc?.City,
                                hasReview ? summary!.Avg : 0.0,
                                hasReview ? summary!.Count : 0
                            );
                        })
                        .ToList();

                    await _cache.SetAsync(
                        ApprovedProductsCacheKey, 
                        allApprovedProducts, 
                        absoluteExpiration: TimeSpan.FromMinutes(10),
                        slidingExpiration: TimeSpan.FromMinutes(5)
                    );
                }
            }
            finally
            {
                _productsLock.Release();
            }
        }

        // Apply filters in-memory to avoid cache fragmentation
        IEnumerable<ProductDto> filtered = allApprovedProducts;

        if (!string.IsNullOrEmpty(storeUserId))
        {
            filtered = filtered.Where(p => p.StoreUserId == storeUserId);
        }

        if (!string.IsNullOrEmpty(country))
        {
            filtered = filtered.Where(p => p.StoreCountry != null && p.StoreCountry.Equals(country, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(city))
        {
            filtered = filtered.Where(p => p.StoreCity != null && p.StoreCity.Equals(city, StringComparison.OrdinalIgnoreCase));
        }

        if (categoryId.HasValue)
        {
            var catId = categoryId.Value;
            // Use cached category tree instead of hitting the database
            var categoryTree = await GetCategoryTreeAsync();
            var allCatIds = new HashSet<int> { catId };
            if (categoryTree.TryGetValue(catId, out var subIds))
            {
                foreach (var id in subIds) allCatIds.Add(id);
            }

            filtered = filtered.Where(p => allCatIds.Contains(p.CategoryId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLowerInvariant();
            filtered = filtered.Where(p => p.Name.ToLowerInvariant().Contains(s) || (p.Description != null && p.Description.ToLowerInvariant().Contains(s)));
        }

        if (minPrice.HasValue)
        {
            filtered = filtered.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            filtered = filtered.Where(p => p.Price <= maxPrice.Value);
        }

        var total = filtered.Count();
        var data = filtered.Skip((page - 1) * limit).Take(limit).ToList();

        return PaginatedResponse<ProductDto>.Create(data, page, limit, total);
    }

    public async Task<List<CategoryDto>> GetTopLevelCategoriesAsync()
    {
        var allCats = await GetAllCategoriesAsync();
        return allCats.Where(c => c.ParentId == null).ToList();
    }

    public async Task<List<CategoryDto>> GetSubcategoriesAsync(int parentId)
    {
        var allCats = await GetAllCategoriesAsync();
        return allCats.Where(c => c.ParentId == parentId).ToList();
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        var allCats = await GetAllCategoriesAsync();
        return allCats.FirstOrDefault(c => c.Id == id);
    }

    public async Task InvalidateProductsCacheAsync()
    {
        await _cache.RemoveAsync(ApprovedProductsCacheKey);
    }

    public async Task InvalidateCategoriesCacheAsync()
    {
        await _cache.RemoveAsync(CategoriesCacheKey);
        await _cache.RemoveAsync(CategoryTreeCacheKey);
    }

    public async Task InvalidateAdsCacheAsync()
    {
        await _cache.RemoveAsync(AdsCacheKey);
    }

    public async Task<List<AdBannerDto>> GetAdBannersAsync()
    {
        var ads = await _cache.GetAsync<List<AdBannerDto>>(AdsCacheKey);

        // Automatically clear stale cache if it contains test/dummy placeholder data like /media/hero.png
        if (ads != null && ads.Any(a => a.ImageUrl != null && (a.ImageUrl.Equals("/media/hero.png", StringComparison.OrdinalIgnoreCase) || a.ImageUrl.Equals("/media/new.png", StringComparison.OrdinalIgnoreCase))))
        {
            await _cache.RemoveAsync(AdsCacheKey);
            ads = null;
        }

        if (ads == null)
        {
            await _adsLock.WaitAsync();
            try
            {
                ads = await _cache.GetAsync<List<AdBannerDto>>(AdsCacheKey);
                if (ads == null)
                {
                    ads = await _db.AdBanners
                        .AsNoTracking()
                        .OrderBy(a => a.SortOrder)
                        .ThenByDescending(a => a.CreatedAtUtc)
                        .Select(a => new AdBannerDto(a.Id, a.ImageUrl, a.Title, a.LinkUrl, a.SortOrder))
                        .ToListAsync();

                    await _cache.SetAsync(
                        AdsCacheKey,
                        ads,
                        absoluteExpiration: TimeSpan.FromMinutes(10),
                        slidingExpiration: TimeSpan.FromMinutes(5)
                    );
                }
            }
            finally
            {
                _adsLock.Release();
            }
        }

        return ads;
    }

    // ──────────────── Private Helpers ────────────────

    private async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        var allCats = await _cache.GetAsync<List<CategoryDto>>(CategoriesCacheKey);

        if (allCats == null)
        {
            await _categoriesLock.WaitAsync();
            try
            {
                allCats = await _cache.GetAsync<List<CategoryDto>>(CategoriesCacheKey);
                if (allCats == null)
                {
                    allCats = await _db.Categories
                        .AsNoTracking()
                        .OrderBy(c => c.Name)
                        .Select(c => new CategoryDto(c.Id, c.Name, c.Code, c.ImageUrl, c.ParentId))
                        .ToListAsync();

                    await _cache.SetAsync(
                        CategoriesCacheKey, 
                        allCats, 
                        absoluteExpiration: TimeSpan.FromMinutes(30),
                        slidingExpiration: TimeSpan.FromMinutes(10)
                    );
                }
            }
            finally
            {
                _categoriesLock.Release();
            }
        }

        return allCats;
    }

    /// <summary>
    /// Returns a cached dictionary mapping each category ID to its direct child category IDs.
    /// Built from the full category list so category filtering never hits the database.
    /// </summary>
    private async Task<Dictionary<int, List<int>>> GetCategoryTreeAsync()
    {
        var tree = await _cache.GetAsync<Dictionary<int, List<int>>>(CategoryTreeCacheKey);

        if (tree == null)
        {
            await _categoryTreeLock.WaitAsync();
            try
            {
                tree = await _cache.GetAsync<Dictionary<int, List<int>>>(CategoryTreeCacheKey);
                if (tree == null)
                {
                    var allCats = await GetAllCategoriesAsync();
                    tree = allCats
                        .Where(c => c.ParentId.HasValue)
                        .GroupBy(c => c.ParentId!.Value)
                        .ToDictionary(g => g.Key, g => g.Select(c => c.Id).ToList());

                    await _cache.SetAsync(
                        CategoryTreeCacheKey,
                        tree,
                        absoluteExpiration: TimeSpan.FromMinutes(30),
                        slidingExpiration: TimeSpan.FromMinutes(10)
                    );

                    _logger.LogInformation("Built category tree cache with {Count} parent categories.", tree.Count);
                }
            }
            finally
            {
                _categoryTreeLock.Release();
            }
        }

        return tree;
    }

    private static ProductDto ToDto(Product p, string? storeName, string? storeCountry, string? storeCity, double avgRating = 0, int reviewCount = 0) => new(
        p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.AdditionalImageUrls ?? new List<string>(), p.StockQuantity, p.OriginalQuantity,
        p.CategoryId, p.Category?.Name,
        p.Status.ToString(), p.RejectionReason,
        p.StoreUserId, storeName, storeCountry, storeCity,
        p.CreatedAtUtc,
        avgRating,
        reviewCount,
        OnlineStore.Api.Helpers.CurrencyHelper.GetCurrencyCode(storeCountry)
    );
}
