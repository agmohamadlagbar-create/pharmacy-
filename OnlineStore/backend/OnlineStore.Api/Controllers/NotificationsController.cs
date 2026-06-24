using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public NotificationsController(ApplicationDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>Get current user's notifications.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] bool unreadOnly = false, [FromQuery] int page = 1, [FromQuery] int limit = 9)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync();

        var notifications = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.IsRead, n.CreatedAtUtc))
            .ToListAsync();

        return Ok(PaginatedResponse<NotificationDto>.Create(notifications, page, limit, total));
    }

    /// <summary>Get unread notification count.</summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> UnreadCount()
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var count = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        return Ok(new { count });
    }

    /// <summary>Mark a notification as read.</summary>
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notif is null) return NotFound();

        notif.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok(new MessageResponse("Notification marked as read"));
    }

    /// <summary>Mark all notifications as read.</summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, true));

        return Ok(new MessageResponse("All notifications marked as read"));
    }
}
