using Microsoft.AspNetCore.SignalR;
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
        await _hub.Clients.Group($"user:{userId}")
            .SendAsync("OrderStatusChanged", new { OrderId = orderId, Status = status });

        // Trigger push notification outside app for status change
        await _fcmService.SendPushNotificationToUserAsync(
            userId,
            "Order Status Updated",
            $"Order #{orderId} status changed to {status}",
            new Dictionary<string, string> { { "orderId", orderId.ToString() }, { "status", status } });
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
        // Push to all connected drivers (no persistence – drivers query available jobs via API)
        await _hub.Clients.Group("available-drivers")
            .SendAsync("ReceiveNotification", new
            {
                Id = 0,
                Title = title,
                Message = message,
                CreatedAtUtc = DateTime.UtcNow
            });
    }
}
