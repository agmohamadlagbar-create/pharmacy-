using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Hubs;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public interface INotificationService
{
    Task NotifyUserAsync(string userId, string title, string message);
    Task NotifyAvailableDriversAsync(string title, string message);
    Task NotifyOrderStatusChangedAsync(string userId, int orderId, string status);
    Task NotifyDriverLocationChangedAsync(string userId, int orderId, double lat, double lng);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IFcmService _fcmService;

    public NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub, IFcmService fcmService)
    {
        _db = db;
        _hub = hub;
        _fcmService = fcmService;
    }

    public async Task NotifyOrderStatusChangedAsync(string userId, int orderId, string status)
    {
        // Live SignalR update only (FCM push is handled by NotifyUserAsync)
        await _hub.Clients.Group($"user:{userId}")
            .SendAsync("OrderStatusChanged", new { OrderId = orderId, Status = status });
    }

    public async Task NotifyDriverLocationChangedAsync(string userId, int orderId, double lat, double lng)
    {
        await _hub.Clients.Group($"user:{userId}")
            .SendAsync("DriverLocationChanged", new { OrderId = orderId, Lat = lat, Lng = lng });
    }

    public async Task NotifyUserAsync(string userId, string title, string message)
    {
        // Persist notification
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        // Push via SignalR (in-app real-time)
        await _hub.Clients.Group($"user:{userId}")
            .SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                notification.CreatedAtUtc
            });

        // Push via FCM (outside-app OS system notification)
        try
        {
            await _fcmService.SendPushNotificationToUserAsync(userId, title, message);
        }
        catch { /* FCM push errors must never block main application logic */ }
    }

    public async Task NotifyAvailableDriversAsync(string title, string message)
    {
        // Push to all connected drivers (in-app real-time via SignalR)
        await _hub.Clients.Group("available-drivers")
            .SendAsync("ReceiveNotification", new
            {
                Id = 0,
                Title = title,
                Message = message,
                CreatedAtUtc = DateTime.UtcNow
            });

        // Push to all registered delivery driver devices via FCM (outside app OS notification)
        try
        {
            var driverUserIds = await (
                from token in _db.UserDeviceTokens
                join userRole in _db.UserRoles on token.UserId equals userRole.UserId
                join role in _db.Roles on userRole.RoleId equals role.Id
                where role.Name == "Delivery"
                select token.UserId
            ).Distinct().ToListAsync();

            foreach (var driverId in driverUserIds)
            {
                await _fcmService.SendPushNotificationToUserAsync(driverId, title, message, new Dictionary<string, string>
                {
                    ["type"] = "available_delivery_job",
                    ["title"] = title,
                    ["body"] = message
                });
            }
        }
        catch { /* FCM push errors must never block main application logic */ }
    }
}
