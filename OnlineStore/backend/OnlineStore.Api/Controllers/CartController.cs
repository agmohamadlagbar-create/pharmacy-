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
        var (storeNames, storeCities, storeCountries) = await GetStoreInfo(cart);
        return Ok(MapCartDto(cart, storeNames, storeCities, storeCountries));
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
        var (storeNames, storeCities, storeCountries) = await GetStoreInfo(cart);
        return Ok(MapCartDto(cart, storeNames, storeCities, storeCountries));
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
        var (storeNames, storeCities, storeCountries) = await GetStoreInfo(cart);
        return Ok(MapCartDto(cart, storeNames, storeCities, storeCountries));
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

        var (storeNames, storeCities, storeCountries) = await GetStoreInfo(cart);
        return Ok(MapCartDto(cart, storeNames, storeCities, storeCountries));
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

    private CartDto MapCartDto(
        Cart cart,
        Dictionary<string, string>? storeNames = null,
        Dictionary<string, string?>? storeCities = null,
        Dictionary<string, string?>? storeCountries = null)
    {
        var items = cart.Items.Select(i => {
            var storeUserId = i.Product?.StoreUserId ?? "";
            var country = storeCountries?.GetValueOrDefault(storeUserId);
            var currencyCode = Helpers.CurrencyHelper.GetCurrencyCode(country);
            return new CartItemDto(
                i.ProductId,
                i.Product?.Name ?? "Unknown Product",
                i.Product?.ImageUrl,
                i.Product?.Price ?? 0,
                i.Quantity,
                storeUserId,
                i.Product?.StoreUserId != null && storeNames != null
                    ? storeNames.GetValueOrDefault(i.Product.StoreUserId)
                    : null,
                i.Product?.StoreUserId != null && storeCities != null
                    ? storeCities.GetValueOrDefault(i.Product.StoreUserId)
                    : null,
                currencyCode
            );
        }).ToList();

        return new CartDto(items, items.Sum(i => i.Price * i.Quantity));
    }

    private async Task<(Dictionary<string, string> Names, Dictionary<string, string?> Cities, Dictionary<string, string?> Countries)> GetStoreInfo(Cart cart)
    {
        var storeUserIds = cart.Items
            .Where(i => i.Product?.StoreUserId != null)
            .Select(i => i.Product!.StoreUserId)
            .Distinct()
            .ToList();

        if (!storeUserIds.Any())
            return (new Dictionary<string, string>(), new Dictionary<string, string?>(), new Dictionary<string, string?>());

        var names = await _db.ShopProfiles
            .AsNoTracking()
            .Where(s => storeUserIds.Contains(s.UserId))
            .ToDictionaryAsync(s => s.UserId, s => s.ShopName);

        var usersInfo = await _db.Users
            .AsNoTracking()
            .Where(u => storeUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.City, u.Country })
            .ToDictionaryAsync(u => u.Id);

        var cities = usersInfo.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.City);
        var countries = usersInfo.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Country);

        return (names, cities, countries);
    }
}
