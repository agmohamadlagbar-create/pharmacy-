using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OnlineStore.Api.Helpers;

namespace OnlineStore.Api.Hubs;

[Authorize]
public class TrackingHub : Hub
{
    /// <summary>
    /// Called by the delivery agent when they move the map marker.
    /// Broadcasts new coordinates instantly to the customer watching this order.
    /// No DB write needed — pure real-time stream.
    /// </summary>
    public async Task SendLocation(int orderId, double latitude, double longitude)
    {
        await Clients.Group($"order_{orderId}")
                     .SendAsync("location:updated", new
                     {
                         orderId,
                         latitude,
                         longitude
                     });
    }

    public async Task SendGroupLocation(string orderGroupId, double latitude, double longitude)
    {
        await Clients.Group(OrderGroupHelper.BuildTrackingGroupName(orderGroupId))
                     .SendAsync("location:updated", new
                     {
                         orderGroupId,
                         latitude,
                         longitude
                     });
    }

    /// <summary>
    /// Customer joins an order-specific group to receive live tracking updates.
    /// </summary>
    public async Task JoinOrderGroup(int orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
    }

    public async Task JoinGroupedOrderGroup(string orderGroupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroupHelper.BuildTrackingGroupName(orderGroupId));
    }

    /// <summary>
    /// Customer leaves the order group when they navigate away.
    /// </summary>
    public async Task LeaveOrderGroup(int orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
    }

    public async Task LeaveGroupedOrderGroup(string orderGroupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, OrderGroupHelper.BuildTrackingGroupName(orderGroupId));
    }
}
