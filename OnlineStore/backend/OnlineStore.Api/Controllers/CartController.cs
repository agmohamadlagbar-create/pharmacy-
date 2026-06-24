using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Helpers;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;
using OnlineStore.Api.Services;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "User")]
public class CartController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cartService;

    public CartController(ApplicationDbContext db, ICartService cartService)
    {
        _db = db;
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null) return Unauthorized();

        var cart = await GetOrCreateCart(userId);
        var (storeNames, storeCities) = await GetStoreInfo(cart);
        return Ok(MapCartDto(cart, storeNames, storeCities));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] OrderItemRequest req)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null) return Unauthorized();
        if (req.Quantity <= 0)
            return BadRequest(new { error = "Quantity must be greater than zero." });

        var result = await _cartService.AddToCartAsync(userId, req.ProductId, req.Quantity);
        if (result.StartsWith("Error:"))
        {
            return BadRequest(new { error = result });
        }

        // Reload cart to reflect changes
        var cart = await GetOrCreateCart(userId);
        var (storeNames, storeCities) = await GetStoreInfo(cart);
        return Ok(MapCartDto(cart, storeNames, storeCities));
    }

    [HttpPut("items/{productId:int}")]
    public async Task<ActionResult<CartDto>> UpdateQuantity(int productId, [FromBody] int quantity)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null) return Unauthorized();

        // Use CartService for consistent validation and error handling
        var result = await _cartService.UpdateQuantityAsync(userId, productId, quantity);
        if (result.StartsWith("Error:"))
        {
            return BadRequest(new { error = result });
        }

        // Reload cart to get updated state with product data
        var cart = await GetOrCreateCart(userId);
        var (storeNames, storeCities) = await GetStoreInfo(cart);
        return Ok(MapCartDto(cart, storeNames, storeCities));
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<ActionResult<CartDto>> RemoveItem(int productId)
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null) return Unauthorized();

        var cart = await GetOrCreateCart(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item != null)
        {
            cart.Items.Remove(item);
            cart.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        var (storeNames, storeCities) = await GetStoreInfo(cart);
        return Ok(MapCartDto(cart, storeNames, storeCities));
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var userId = User.GetAuthenticatedUserId();
        if (userId == null) return Unauthorized();

        var cart = await _db.Carts
            .AsTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart != null)
        {
            cart.Items.Clear();
            cart.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Ok(new MessageResponse("Cart cleared"));
    }

    private async Task<Cart> GetOrCreateCart(string userId)
    {
        var cart = await _db.Carts
            .AsTracking()
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        return cart;
    }

    private CartDto MapCartDto(Cart cart, Dictionary<string, string>? storeNames = null, Dictionary<string, string?>? storeCities = null)
    {
        var items = cart.Items.Select(i => new CartItemDto(
            i.ProductId,
            i.Product?.Name ?? "Unknown Product",
            i.Product?.ImageUrl,
            i.Product?.Price ?? 0,
            i.Quantity,
            i.Product?.StoreUserId ?? "",
            i.Product?.StoreUserId != null && storeNames != null
                ? storeNames.GetValueOrDefault(i.Product.StoreUserId)
                : null,
            i.Product?.StoreUserId != null && storeCities != null
                ? storeCities.GetValueOrDefault(i.Product.StoreUserId)
                : null
        )).ToList();

        return new CartDto(items, items.Sum(i => i.Price * i.Quantity));
    }

    private async Task<(Dictionary<string, string> Names, Dictionary<string, string?> Cities)> GetStoreInfo(Cart cart)
    {
        var storeUserIds = cart.Items
            .Where(i => i.Product?.StoreUserId != null)
            .Select(i => i.Product!.StoreUserId)
            .Distinct()
            .ToList();

        if (!storeUserIds.Any())
            return (new Dictionary<string, string>(), new Dictionary<string, string?>());

        var names = await _db.ShopProfiles
            .AsNoTracking()
            .Where(s => storeUserIds.Contains(s.UserId))
            .ToDictionaryAsync(s => s.UserId, s => s.ShopName);

        var cities = await _db.Users
            .AsNoTracking()
            .Where(u => storeUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.City);

        return (names, cities);
    }
}
