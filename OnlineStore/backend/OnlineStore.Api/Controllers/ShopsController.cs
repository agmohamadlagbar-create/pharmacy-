using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Auth;
namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public ShopsController(ApplicationDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [AllowAnonymous]
    [HttpGet("{userId}")]
    public async Task<ActionResult<PublicShopDto>> GetPublicShopInfo(string userId)
    {
        var shop = await _db.ShopProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (shop == null)
            return NotFound(new { error = "Shop not found" });

        // Get the avatar from AppUser
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        var locationParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(user?.Street)) locationParts.Add(user.Street);
        if (!string.IsNullOrWhiteSpace(user?.BuildingNumber)) locationParts.Add(user.BuildingNumber);
        if (!string.IsNullOrWhiteSpace(user?.City)) locationParts.Add(user.City);
        if (!string.IsNullOrWhiteSpace(user?.Country)) locationParts.Add(user.Country);
        
        var fullLocation = locationParts.Count > 0 ? string.Join(", ", locationParts) : shop.Location;

        return Ok(new PublicShopDto(
            shop.UserId,
            shop.ShopName,
            shop.StoreType,
            fullLocation,
            shop.Description,
            user?.ProfilePictureUrl,
            shop.OpeningTime,
            shop.ClosingTime
        ));
    }

    [Authorize]
    [HttpGet("my-delivered-shops")]
    public async Task<ActionResult<PaginatedResponse<PublicShopDto>>> GetMyDeliveredShops([FromQuery] int page = 1, [FromQuery] int limit = 9)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var deliveredOrders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered)
            .Select(o => o.StoreUserId)
            .Distinct()
            .ToListAsync();

        var shops = await _db.ShopProfiles
            .AsNoTracking()
            .Where(s => deliveredOrders.Contains(s.UserId))
            .ToListAsync();

        // Get users to construct full location and get avatar
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => deliveredOrders.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        var dtos = shops.Select(shop => {
            var user = users.GetValueOrDefault(shop.UserId);
            var locationParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(user?.Street)) locationParts.Add(user.Street);
            if (!string.IsNullOrWhiteSpace(user?.BuildingNumber)) locationParts.Add(user.BuildingNumber);
            if (!string.IsNullOrWhiteSpace(user?.City)) locationParts.Add(user.City);
            if (!string.IsNullOrWhiteSpace(user?.Country)) locationParts.Add(user.Country);
            
            var fullLocation = locationParts.Count > 0 ? string.Join(", ", locationParts) : shop.Location;

            return new PublicShopDto(
                shop.UserId,
                shop.ShopName,
                shop.StoreType,
                fullLocation,
                shop.Description,
                user?.ProfilePictureUrl,
                shop.OpeningTime,
                shop.ClosingTime
            );
        }).ToList();

        return Ok(PaginatedResponse<PublicShopDto>.Create(dtos, page, limit));
    }
}
