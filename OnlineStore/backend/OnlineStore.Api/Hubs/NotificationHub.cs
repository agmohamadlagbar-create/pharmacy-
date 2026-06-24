using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OnlineStore.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    /// <summary>
    /// When a client connects, add them to a user-specific group so we can push notifications to them.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        // Delivery drivers also join the "drivers" group for broadcast notifications
        var accountType = Context.User?.FindFirst("accountType")?.Value;
        if (accountType == "Delivery")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "available-drivers");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        var accountType = Context.User?.FindFirst("accountType")?.Value;
        if (accountType == "Delivery")
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "available-drivers");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
