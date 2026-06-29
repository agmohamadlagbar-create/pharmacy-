using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly OnlineStore.Api.Services.IFileStorageService _fileStorage;
    private readonly OnlineStore.Api.Services.IStoreCacheService _cache;
    private readonly OnlineStore.Api.Services.ICacheService _memCache;

    public ProductsController(ApplicationDbContext db, UserManager<AppUser> userManager, OnlineStore.Api.Services.IFileStorageService fileStorage, OnlineStore.Api.Services.IStoreCacheService cache, OnlineStore.Api.Services.ICacheService memCache)
    {
        _db = db;
        _userManager = userManager;
        _fileStorage = fileStorage;
        _cache = cache;
        _memCache = memCache;
    }

    /// <summary>Public – returns only Approved products. Optional filter by categoryId. Supports pagination.</summary>
    [AllowAnonymous]
    [HttpGet]
    [HttpGet("approved")]
    public async Task<IActionResult> GetApproved(
        [FromQuery] int? categoryId, 
        [FromQuery] string? storeUserId = null,
        [FromQuery] string? search = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 9)
    {
        string? country = null;
        string? city = null;

        var userId = _userManager.GetUserId(User);
        if (userId != null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                country = user.Country;
                city = user.City;
            }
        }

        var paginatedResponse = await _cache.GetApprovedProductsPaginatedAsync(categoryId, storeUserId, search, minPrice, maxPrice, country, city, page, limit);
        return Ok(paginatedResponse);
    }

    /// <summary>Store only – view own products with all statuses. Supports pagination and filtering.</summary>
    [Authorize(Roles = "Shop")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyProducts(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int? productId = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 9)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.StoreUserId == userId);

        if (productId.HasValue)
        {
            query = query.Where(p => p.Id == productId.Value);
        }

        // Filter by status if provided (skip for "All")
        if (!string.IsNullOrEmpty(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse<ProductStatus>(status, true, out var s))
        {
            query = query.Where(p => p.Status == s);
        }

        // Filter by search term
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        var orderedQuery = query.OrderByDescending(p => p.CreatedAtUtc);

        var total = await orderedQuery.CountAsync();

        var products = await orderedQuery
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        var shop = await _db.ShopProfiles.AsNoTracking().FirstOrDefaultAsync(s2 => s2.UserId == userId);
        var storeUser = await _userManager.FindByIdAsync(userId);
        var pageDtos = products.Select(p => ToDto(p, shop?.ShopName, storeUser?.Country, storeUser?.City)).ToList();

        return Ok(PaginatedResponse<ProductDto>.Create(pageDtos, page, limit, total));
    }

    /// <summary>Store only – create a product (status = Pending).</summary>
    [Authorize(Roles = "Shop")]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest req)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var category = await _db.Categories.FindAsync(req.CategoryId);
        if (category is null) return BadRequest(new { error = "Invalid category" });

        var product = new Product
        {
            StoreUserId = userId,
            CategoryId = req.CategoryId,
            Name = req.Name,
            Description = req.Description,
            Price = req.Price,
            ImageUrl = req.ImageUrl,
            AdditionalImageUrls = req.AdditionalImageUrls ?? new List<string>(),
            StockQuantity = req.StockQuantity,
            OriginalQuantity = req.StockQuantity,
            Status = ProductStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        await _cache.InvalidateProductsCacheAsync(); // Invalidate cache
        await _memCache.RemoveAsync($"product_details_{product.Id}");

        var shop = await _db.ShopProfiles.AsNoTracking().FirstOrDefaultAsync(s2 => s2.UserId == userId);
        var storeUser = await _userManager.FindByIdAsync(userId);
        product.Category = category;
        return CreatedAtAction(nameof(GetApproved), null, ToDto(product, shop?.ShopName, storeUser?.Country, storeUser?.City));
    }

    /// <summary>Store only – upload a product image. Returns the storage key.</summary>
    [Authorize(Roles = "Shop")]
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] int? categoryId, [FromQuery] int? productId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var country = user.Country ?? "Unknown";
        var city = user.City ?? "Unknown";
        var pId = productId ?? 0;

        try
        {
            var storageKey = await _fileStorage.SaveProductImageAsync(country, city, pId, userId, file);
            return Ok(new { imageUrl = "/media/" + storageKey });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Store only – update own product. Approved products go back to Pending.</summary>
    [Authorize(Roles = "Shop")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductRequest req)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var product = await _db.Products.AsTracking().Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();
        if (product.StoreUserId != userId) return Forbid();

        // Check what fields actually changed (excluding StockQuantity)
        bool nameChanged = req.Name is not null && req.Name != product.Name;
        bool descriptionChanged = req.Description is not null && req.Description != product.Description;
        bool priceChanged = req.Price.HasValue && req.Price.Value != product.Price;
        bool imageUrlChanged = req.ImageUrl is not null && req.ImageUrl != product.ImageUrl;
        bool categoryChanged = req.CategoryId.HasValue && req.CategoryId.Value != product.CategoryId;

        bool additionalImagesChanged = false;
        if (req.AdditionalImageUrls is not null)
        {
            var oldImages = product.AdditionalImageUrls ?? new List<string>();
            var newImages = req.AdditionalImageUrls;
            if (oldImages.Count != newImages.Count || !System.Linq.Enumerable.SequenceEqual(oldImages, newImages))
            {
                additionalImagesChanged = true;
            }
        }

        bool contentChanged = nameChanged || descriptionChanged || priceChanged || imageUrlChanged || categoryChanged || additionalImagesChanged;

        Console.WriteLine($"DEBUG UPDATE: id={id}");
        Console.WriteLine($"  nameChanged={nameChanged} (req={req.Name}, prod={product.Name})");
        Console.WriteLine($"  descriptionChanged={descriptionChanged} (req={req.Description}, prod={product.Description})");
        Console.WriteLine($"  priceChanged={priceChanged} (req={req.Price}, prod={product.Price})");
        Console.WriteLine($"  imageUrlChanged={imageUrlChanged} (req={req.ImageUrl}, prod={product.ImageUrl})");
        Console.WriteLine($"  categoryChanged={categoryChanged} (req={req.CategoryId}, prod={product.CategoryId})");
        Console.WriteLine($"  additionalImagesChanged={additionalImagesChanged}");
        Console.WriteLine($"  contentChanged={contentChanged}");
        Console.WriteLine($"  oldStatus={product.Status}");

        if (req.Name is not null) product.Name = req.Name;
        if (req.Description is not null) product.Description = req.Description;
        if (req.Price.HasValue) product.Price = req.Price.Value;
        if (req.ImageUrl is not null) product.ImageUrl = req.ImageUrl;
        if (req.AdditionalImageUrls is not null) product.AdditionalImageUrls = req.AdditionalImageUrls;
        if (req.StockQuantity.HasValue)
        {
            product.StockQuantity = req.StockQuantity.Value;
            product.OriginalQuantity = Math.Max(product.OriginalQuantity, req.StockQuantity.Value);
        }
        if (req.CategoryId.HasValue)
        {
            var cat = await _db.Categories.FindAsync(req.CategoryId.Value);
            if (cat is null) return BadRequest(new { error = "Invalid category" });
            product.CategoryId = req.CategoryId.Value;
            product.Category = cat;
        }

        // Re-approval required if details (other than quantity) were changed
        if (contentChanged)
        {
            product.Status = ProductStatus.Pending;
            product.RejectionReason = null; // Clear rejection reason on edit
        }

        product.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _cache.InvalidateProductsCacheAsync(); // Invalidate cache
        await _memCache.RemoveAsync($"product_details_{id}");
        
        var shop = await _db.ShopProfiles.AsNoTracking().FirstOrDefaultAsync(s2 => s2.UserId == userId);
        var storeUser = await _userManager.FindByIdAsync(userId);
        return Ok(ToDto(product, shop?.ShopName, storeUser?.Country, storeUser?.City));
    }

    /// <summary>Store (own) or Admin – delete product.</summary>
    [Authorize(Roles = "Shop,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && product.StoreUserId != userId) return Forbid();

        // Remove cart items referencing this product
        var cartItems = await _db.CartItems.Where(ci => ci.ProductId == id).ToListAsync();
        if (cartItems.Any()) _db.CartItems.RemoveRange(cartItems);

        // Nullify order item references so order history is preserved but product can be deleted
        var orderItems = await _db.OrderItems.Where(oi => oi.ProductId == id).ToListAsync();
        foreach (var oi in orderItems)
            oi.ProductId = null;

        // Remove reviews
        var reviews = await _db.Reviews.Where(r => r.ProductId == id).ToListAsync();
        if (reviews.Any()) _db.Reviews.RemoveRange(reviews);

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        await _cache.InvalidateProductsCacheAsync();
        await _memCache.RemoveAsync($"product_details_{id}");
        
        return NoContent();
    }

    /// <summary>Public – Get product details including statistics, reviews, and related products.</summary>
    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<ProductDetailsDto>> GetDetails(int id)
    {
        var cacheKey = $"product_details_{id}";
        var cachedDetails = await _memCache.GetAsync<ProductDetailsDto>(cacheKey);
        if (cachedDetails != null)
        {
            return Ok(cachedDetails);
        }

        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.Status == ProductStatus.Approved);

        if (product is null) return NotFound();

        // Combine product + shop profile lookup: reuse the already-loaded StoreUserId
        var shop = await _db.ShopProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == product.StoreUserId);
        var storeUser = await _userManager.FindByIdAsync(product.StoreUserId);
        var productDto = ToDto(product, shop?.ShopName, storeUser?.Country, storeUser?.City);

        // Review stats via single aggregation query instead of loading all reviews
        var reviewStats = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == id)
            .GroupBy(r => r.ProductId)
            .Select(g => new { Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync();

        var avgRating = reviewStats?.Avg ?? 0;
        var totalReviews = reviewStats?.Count ?? 0;

        // Only load top 10 reviews, not all
        var topReviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(10)
            .Select(r => new ReviewDto(r.Id, r.UserName, r.Rating, r.Comment, r.CreatedAtUtc))
            .ToListAsync();

        // Stats: Sales based on OrderItems
        var totalSales = await _db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
            .Where(oi => oi.ProductId == id && oi.Order.Status != OrderStatus.Cancelled)
            .SumAsync(oi => oi.Quantity);

        // Daily Sales for the last 5 days (including today)
        var now = DateTime.UtcNow;
        var last5Days = Enumerable.Range(0, 5)
            .Select(i => now.Date.AddDays(-i))
            .OrderBy(d => d)
            .ToList();

        var salesData = await _db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
            .Where(oi => oi.ProductId == id && oi.Order.Status != OrderStatus.Cancelled && oi.Order.CreatedAtUtc >= last5Days.First())
            .GroupBy(oi => oi.Order.CreatedAtUtc.Date)
            .Select(g => new { Date = g.Key, Count = g.Sum(oi => oi.Quantity) })
            .ToListAsync();

        var dailySales = last5Days.Select(d => {
            // Normalize d to be kind-agnostic for comparison
            var normalD = new DateTime(d.Year, d.Month, d.Day);
            var count = salesData.FirstOrDefault(s => {
                var sDate = s.Date;
                return sDate.Year == normalD.Year && sDate.Month == normalD.Month && sDate.Day == normalD.Day;
            })?.Count ?? 0;
            
            return new DailySaleDto(
                d.ToString("ddd"), // "Mon", "Tue", etc.
                count
            );
        }).ToList();

        // Related Products
        var related = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != id && p.Status == ProductStatus.Approved)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(4)
            .ToListAsync();

        // Resolve store names for related products
        var relatedStoreIds = related.Select(p => p.StoreUserId).Distinct().ToList();
        var relatedStoreNames = await _db.ShopProfiles
            .AsNoTracking()
            .Where(s => relatedStoreIds.Contains(s.UserId))
            .ToDictionaryAsync(s => s.UserId, s => s.ShopName);

        // Also fetch countries to resolve currency codes
        var relatedStoreUsers = await _userManager.Users
            .AsNoTracking()
            .Where(u => relatedStoreIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Country);

        var relatedDtos = related
            .Select(p => ToDto(p, relatedStoreNames.GetValueOrDefault(p.StoreUserId), relatedStoreUsers.GetValueOrDefault(p.StoreUserId), null))
            .ToList();

        var detailsDto = new ProductDetailsDto(
            productDto,
            totalSales,
            product.StockQuantity,
            avgRating,
            totalReviews,
            topReviews,
            relatedDtos,
            dailySales
        );

        await _memCache.SetAsync(
            cacheKey,
            detailsDto,
            absoluteExpiration: TimeSpan.FromMinutes(10),
            slidingExpiration: TimeSpan.FromMinutes(5)
        );

        return Ok(detailsDto);
    }

    /// <summary>Authenticated user – Add a review for a product.</summary>
    [Authorize]
    [HttpPost("{id:int}/reviews")]
    public async Task<ActionResult<ReviewDto>> AddReview(int id, CreateReviewRequest req)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        var user = await _userManager.FindByIdAsync(userId);

        var review = new Review
        {
            ProductId = id,
            UserId = userId,
            UserName = user?.FullName ?? user?.Email ?? "Anonymous",
            Rating = req.Rating < 1 ? 1 : (req.Rating > 5 ? 5 : req.Rating),
            Comment = req.Comment,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        await _cache.InvalidateProductsCacheAsync(); // Invalidate cache so ratings update
        await _memCache.RemoveAsync($"product_details_{id}");

        return Ok(new ReviewDto(review.Id, review.UserName, review.Rating, review.Comment, review.CreatedAtUtc));
    }

    private static ProductDto ToDto(Product p, string? storeName, string? storeCountry = null, string? storeCity = null, double avgRating = 0, int reviewCount = 0) => new(
        p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.AdditionalImageUrls ?? new List<string>(), p.StockQuantity, p.OriginalQuantity,
        p.CategoryId, p.Category?.Name,
        p.Status.ToString(), p.RejectionReason,
        p.StoreUserId, storeName, storeCountry, storeCity,
        p.CreatedAtUtc,
        avgRating,
        reviewCount,
        Helpers.CurrencyHelper.GetCurrencyCode(storeCountry)
    );
}
