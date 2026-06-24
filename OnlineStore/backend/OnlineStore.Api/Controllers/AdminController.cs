using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Helpers;
using OnlineStore.Api.Services;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly INotificationService _notif;
    private readonly IFileStorageService _fileStorage;
    private readonly IStoreCacheService _cache;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<OnlineStore.Api.Hubs.TrackingHub> _trackingHub;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext db, UserManager<AppUser> userManager, INotificationService notif, IFileStorageService fileStorage, IStoreCacheService cache, Microsoft.AspNetCore.SignalR.IHubContext<OnlineStore.Api.Hubs.TrackingHub> trackingHub, ILogger<AdminController> logger)
    {
        _db = db;
        _userManager = userManager;
        _notif = notif;
        _fileStorage = fileStorage;
        _cache = cache;
        _trackingHub = trackingHub;
        _logger = logger;
    }

    // ──────────────── Products ────────────────

    /// <summary>Get all products, optionally filtered by status. Supports pagination.</summary>
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int limit = 9)
    {
        var query = _db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

        if (Enum.TryParse<ProductStatus>(status, true, out var s))
            query = query.Where(p => p.Status == s);

        var total = await query.CountAsync();

        var products = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        var storeUserIds = products.Select(p => p.StoreUserId).Distinct().ToList();
        var storeNames = await _db.ShopProfiles
            .AsNoTracking()
            .Where(sp => storeUserIds.Contains(sp.UserId))
            .ToDictionaryAsync(sp => sp.UserId, sp => sp.ShopName);

        var userInfo = await _db.Users
            .AsNoTracking()
            .Where(u => storeUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new { u.Country, u.City });

        var dtos = products.Select(p => {
            var loc = userInfo.GetValueOrDefault(p.StoreUserId);
            return new ProductDto(
                p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.AdditionalImageUrls ?? new List<string>(), p.StockQuantity, p.OriginalQuantity,
                p.CategoryId, p.Category?.Name,
                p.Status.ToString(), p.RejectionReason,
                p.StoreUserId, storeNames.GetValueOrDefault(p.StoreUserId),
                loc?.Country, loc?.City,
                p.CreatedAtUtc
            );
        }).ToList();

        return Ok(PaginatedResponse<ProductDto>.Create(dtos, page, limit, total));
    }

    /// <summary>Approve a product.</summary>
    [HttpPut("products/{id:int}/approve")]
    public async Task<IActionResult> ApproveProduct(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        product.Status = ProductStatus.Approved;
        product.RejectionReason = null;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Admin changed product so cache might be stale
        await _cache.InvalidateProductsCacheAsync();

        await _notif.NotifyUserAsync(product.StoreUserId, "Product Approved", $"Your product \"{product.Name}\" has been approved and is now visible.");

        return Ok(new MessageResponse("Product approved"));
    }

    /// <summary>Reject a product with a reason.</summary>
    [HttpPut("products/{id:int}/reject")]
    public async Task<IActionResult> RejectProduct(int id, AdminRejectRequest req)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        product.Status = ProductStatus.Rejected;
        product.RejectionReason = req.Reason;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Product was rejected (might have been approved before)
        await _cache.InvalidateProductsCacheAsync();

        await _notif.NotifyUserAsync(product.StoreUserId, "Product Rejected", $"Your product \"{product.Name}\" was rejected. Reason: {req.Reason}");

        return Ok(new MessageResponse("Product rejected"));
    }

    /// <summary>Delete a product. Removes cart items and nullifies order item references first.</summary>
    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

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
        return NoContent();
    }

    /// <summary>Admin – create a product (auto-approved). Accepts storeName to resolve store user ID.</summary>
    [HttpPost("products")]
    public async Task<ActionResult<ProductDto>> CreateProduct(AdminCreateProductRequest req)
    {
        var category = await _db.Categories.FindAsync(req.CategoryId);
        if (category is null) return BadRequest(new { error = "Invalid category" });

        // Resolve store user ID: accept either direct storeUserId or storeName
        var storeUserId = req.StoreUserId;
        if (string.IsNullOrWhiteSpace(storeUserId) && !string.IsNullOrWhiteSpace(req.StoreName))
        {
            var shop = await _db.ShopProfiles.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShopName == req.StoreName);
            if (shop is null)
                return BadRequest(new { error = $"No store found with name '{req.StoreName}'" });
            storeUserId = shop.UserId;
        }
        else if (!string.IsNullOrWhiteSpace(storeUserId))
        {
            // Verify the storeUserId exists
            var shopExists = await _db.ShopProfiles.AsNoTracking().AnyAsync(s => s.UserId == storeUserId);
            if (!shopExists)
                return BadRequest(new { error = "Invalid store user ID" });
        }
        else
        {
            return BadRequest(new { error = "Store name is required" });
        }

        var product = new Product
        {
            StoreUserId = storeUserId!,
            CategoryId = req.CategoryId,
            Name = req.Name,
            Description = req.Description,
            Price = req.Price,
            ImageUrl = req.ImageUrl,
            AdditionalImageUrls = req.AdditionalImageUrls ?? new List<string>(),
            Status = ProductStatus.Approved,
            StockQuantity = req.StockQuantity,
            OriginalQuantity = req.StockQuantity,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        
        await _cache.InvalidateProductsCacheAsync();

        product.Category = category;
        var storeProfile = await _db.ShopProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == storeUserId);
        var storeUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == storeUserId);
        
        return Ok(new ProductDto(product.Id, product.Name, product.Description, product.Price, product.ImageUrl, product.AdditionalImageUrls ?? new List<string>(), product.StockQuantity, product.OriginalQuantity,
            product.CategoryId, category.Name, product.Status.ToString(), null, product.StoreUserId, storeProfile?.ShopName, storeUser?.Country, storeUser?.City, product.CreatedAtUtc));
    }

    /// <summary>List all stores for admin autocomplete.</summary>
    [HttpGet("stores")]
    public async Task<ActionResult<List<AdminStoreDto>>> GetStores([FromQuery] string? q)
    {
        var query = _db.ShopProfiles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.ShopName.Contains(q));

        var stores = await query.OrderBy(s => s.ShopName).Take(20)
            .Select(s => new AdminStoreDto(s.UserId, s.ShopName, s.StoreType, s.Location))
            .ToListAsync();

        return Ok(stores);
    }

    /// <summary>Admin – update any product.</summary>
    [HttpPut("products/{id:int}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, AdminUpdateProductRequest req)
    {
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();

        if (req.Name is not null) product.Name = req.Name;
        if (req.Description is not null) product.Description = req.Description;
        if (req.Price.HasValue) product.Price = req.Price.Value;
        if (req.ImageUrl is not null) product.ImageUrl = req.ImageUrl;
        if (req.AdditionalImageUrls is not null) product.AdditionalImageUrls = req.AdditionalImageUrls;
        if (req.CategoryId.HasValue)
        {
            var cat = await _db.Categories.FindAsync(req.CategoryId.Value);
            if (cat is null) return BadRequest(new { error = "Invalid category" });
            product.CategoryId = req.CategoryId.Value;
            product.Category = cat;
        }
        if (req.Status is not null && Enum.TryParse<ProductStatus>(req.Status, true, out var s))
            product.Status = s;
        if (req.StockQuantity.HasValue)
        {
            product.StockQuantity = req.StockQuantity.Value;
            product.OriginalQuantity = req.StockQuantity.Value;
        }

        product.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _cache.InvalidateProductsCacheAsync();

        var shop = await _db.ShopProfiles.AsNoTracking().FirstOrDefaultAsync(sp => sp.UserId == product.StoreUserId);
        var storeUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == product.StoreUserId);
        
        return Ok(new ProductDto(product.Id, product.Name, product.Description, product.Price, product.ImageUrl, product.AdditionalImageUrls ?? new List<string>(), product.StockQuantity, product.OriginalQuantity,
            product.CategoryId, product.Category?.Name, product.Status.ToString(), product.RejectionReason,
            product.StoreUserId, shop?.ShopName, storeUser?.Country, storeUser?.City, product.CreatedAtUtc));
    }

    /// <summary>Admin – upload product image.</summary>
    [HttpPost("products/upload-image")]
    public async Task<IActionResult> UploadProductImage(IFormFile file, [FromQuery] int? productId)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        var country = user?.Country ?? "Unknown";
        var city = user?.City ?? "Unknown";
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

    // ──────────────── Users ────────────────

    /// <summary>List all users. Supports pagination.</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 9)
    {
        var query = _userManager.Users.AsNoTracking();

        var total = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(u => new UserDto(
                u.Id, u.Email ?? "", u.AccountType.ToString(), u.FullName,
                u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
            ))
            .ToListAsync();

        return Ok(PaginatedResponse<UserDto>.Create(users, page, limit, total));
    }

    /// <summary>Disable a user account (sets lockout far in the future).</summary>
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DisableUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        return Ok(new MessageResponse("User account disabled"));
    }

    /// <summary>Re-enable a disabled user account.</summary>
    [HttpPut("users/{id}/enable")]
    public async Task<IActionResult> EnableUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        return Ok(new MessageResponse("User account enabled"));
    }

    // ──────────────── Orders ────────────────

    /// <summary>View all orders.</summary>
    [HttpGet("orders")]
    public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int limit = 9)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Product);

        var total = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        var storeUserIds = orders.Select(o => o.StoreUserId).Distinct().ToList();
        var storeNames = await _db.ShopProfiles
            .AsNoTracking()
            .Where(s => storeUserIds.Contains(s.UserId))
            .ToDictionaryAsync(s => s.UserId, s => s.ShopName);

        var dtos = orders.Select(o => new OrderDto(
            o.Id, o.UserId, o.StoreUserId, storeNames.GetValueOrDefault(o.StoreUserId),
            o.Status.ToString(), o.TotalAmount, o.DeliveryAddress ?? "", o.PaymentMethod, o.OfferCode,
            o.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.Product?.Name ?? "", i.Quantity, i.UnitPrice, i.Product?.StoreUserId ?? o.StoreUserId)).ToList(),
            o.CreatedAtUtc, o.UpdatedAtUtc, o.OrderGroupId
        )).ToList();

        return Ok(PaginatedResponse<OrderDto>.Create(dtos, page, limit, total));
    }

    // ──────────────── Delivery – Manual Assignment ────────────────

    /// <summary>Manually assign a driver to a delivery job.</summary>
    [HttpPost("delivery/{jobId:int}/assign")]
    public async Task<IActionResult> AssignDelivery(int jobId, AssignDeliveryRequest req)
    {
        var job = await _db.DeliveryJobs.Include(j => j.Order).FirstOrDefaultAsync(j => j.Id == jobId);
        if (job is null) return NotFound();

        var driver = await _userManager.FindByIdAsync(req.DeliveryUserId);
        if (driver is null || driver.AccountType != AccountType.Delivery)
            return BadRequest(new { error = "Invalid delivery user" });

        job.DeliveryUserId = req.DeliveryUserId;
        job.Status = DeliveryStatus.Assigned;
        job.AcceptedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _notif.NotifyUserAsync(req.DeliveryUserId, "Delivery Assigned", $"You have been assigned to delivery job #{jobId}.");
        if (job.Order is not null)
        {
            var orderGroupId = OrderGroupHelper.ResolveGroupId(job.Order);

            await _notif.NotifyUserAsync(job.Order.UserId, "Delivery Assigned", $"A driver is collecting items for order #{orderGroupId}.");
            await _notif.NotifyOrderStatusChangedAsync(job.Order.UserId, job.Order.Id, DeliveryStatus.Assigned.ToString());

            _logger.LogInformation("[SignalR] Sending order:status_updated → group: order_{OrderId}, status: {Status}", job.OrderId, OrderStatus.PickedUp);
            await _trackingHub.Clients.Group(OrderGroupHelper.BuildTrackingGroupName(orderGroupId))
                .SendAsync("order:status_updated", new
                {
                    orderGroupId,
                    status = DeliveryStatus.Assigned.ToString(),
                    deliveryJobId = job.Id,
                    deliveryStatus = job.Status.ToString()
                });
        }

        return Ok(new MessageResponse("Driver assigned successfully"));
    }
}
