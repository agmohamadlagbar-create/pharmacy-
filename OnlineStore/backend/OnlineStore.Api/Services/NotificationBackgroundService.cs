using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OnlineStore.Api.Helpers;
using OnlineStore.Api.Hubs;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class NotificationBackgroundService : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(
        INotificationQueue queue,
        IServiceProvider serviceProvider,
        ILogger<NotificationBackgroundService> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationBackgroundService starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var item = await _queue.DequeueAsync(stoppingToken);
                await ProcessNotificationAsync(item);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued notification item.");
            }
        }
    }

    private async Task ProcessNotificationAsync(NotificationWorkItem item)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notif = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var trackingHub = scope.ServiceProvider.GetRequiredService<IHubContext<TrackingHub>>();

        try
        {
            switch (item.TaskType)
            {
                case NotificationTaskType.StoreOrderStatusUpdate:
                    await ProcessStoreOrderStatusUpdateAsync(db, notif, trackingHub, item);
                    break;
                case NotificationTaskType.DeliveryAcceptJob:
                    await ProcessDeliveryAcceptJobAsync(db, notif, trackingHub, item);
                    break;
                case NotificationTaskType.DeliveryStatusUpdate:
                    await ProcessDeliveryStatusUpdateAsync(db, notif, trackingHub, item);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process NotificationWorkItem {TaskType} for Order {OrderId}", item.TaskType, item.OrderId);
        }
    }

    private async Task ProcessStoreOrderStatusUpdateAsync(
        ApplicationDbContext db,
        INotificationService notif,
        IHubContext<TrackingHub> trackingHub,
        NotificationWorkItem item)
    {
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == item.OrderId);
        if (order == null || string.IsNullOrEmpty(item.Status)) return;

        if (Enum.TryParse<OrderStatus>(item.Status, true, out var newStatus))
        {
            await notif.NotifyUserAsync(order.UserId, "Order Update", $"Your order #{order.Id} is now {newStatus}.");
            await notif.NotifyOrderStatusChangedAsync(order.UserId, order.Id, newStatus.ToString());

            await trackingHub.Clients.Group($"order_{order.Id}")
                .SendAsync("order:status_updated", new
                {
                    orderId = order.Id,
                    orderGroupId = OrderGroupHelper.ResolveGroupId(order),
                    status = newStatus.ToString()
                });

            List<Order> groupedOrders = new List<Order> { order };
            if (!string.IsNullOrWhiteSpace(order.OrderGroupId))
            {
                groupedOrders = await db.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                    .Where(o => o.OrderGroupId == order.OrderGroupId)
                    .ToListAsync();
            }

            DeliveryJob? deliveryJob = null;
            if (item.JobId.HasValue)
            {
                deliveryJob = await db.DeliveryJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == item.JobId.Value);
            }

            if (newStatus == OrderStatus.ReadyForPickup && deliveryJob != null)
            {
                var orderGroupId = OrderGroupHelper.ResolveGroupId(order);
                await notif.NotifyUserAsync(order.UserId, "Order Ready", $"All stores in order #{orderGroupId} are ready. Waiting for a driver to accept the delivery.");
                await notif.NotifyAvailableDriversAsync("New Delivery Available", $"Order #{orderGroupId} from {groupedOrders.Count} stores is ready for pickup.");
            }

            await BroadcastGroupedOrderStatusAsync(trackingHub, groupedOrders, deliveryJob);
        }
    }

    private async Task ProcessDeliveryAcceptJobAsync(
        ApplicationDbContext db,
        INotificationService notif,
        IHubContext<TrackingHub> trackingHub,
        NotificationWorkItem item)
    {
        if (!item.JobId.HasValue) return;

        var job = await db.DeliveryJobs
            .AsNoTracking()
            .Include(j => j.Order)
            .FirstOrDefaultAsync(j => j.Id == item.JobId.Value);

        if (job?.Order != null)
        {
            var orderGroupId = OrderGroupHelper.ResolveGroupId(job.Order);
            var groupedOrders = await LoadGroupedOrdersAsync(db, orderGroupId);

            await notif.NotifyUserAsync(job.Order.UserId, "Delivery Assigned", $"A driver is collecting items for order #{orderGroupId} from {groupedOrders.Count} stores.");
            await notif.NotifyOrderStatusChangedAsync(job.Order.UserId, job.Order.Id, DeliveryStatus.Assigned.ToString());

            await BroadcastGroupedOrderStatusAsync(trackingHub, groupedOrders, job);
        }
    }

    private async Task ProcessDeliveryStatusUpdateAsync(
        ApplicationDbContext db,
        INotificationService notif,
        IHubContext<TrackingHub> trackingHub,
        NotificationWorkItem item)
    {
        if (!item.JobId.HasValue || string.IsNullOrEmpty(item.Status)) return;

        var job = await db.DeliveryJobs
            .AsNoTracking()
            .Include(j => j.Order)
            .FirstOrDefaultAsync(j => j.Id == item.JobId.Value);

        if (job?.Order != null && Enum.TryParse<DeliveryStatus>(item.Status, true, out var newStatus))
        {
            var orderGroupId = OrderGroupHelper.ResolveGroupId(job.Order);
            var groupedOrders = await LoadGroupedOrdersAsync(db, orderGroupId);

            var mappedOrderStatus = newStatus switch
            {
                DeliveryStatus.PickedUp => OrderStatus.PickedUp,
                DeliveryStatus.OnTheWay => OrderStatus.OnTheWay,
                DeliveryStatus.Delivered => OrderStatus.Delivered,
                _ => job.Order.Status
            };

            await notif.NotifyUserAsync(job.Order.UserId, "Delivery Update", $"Your order #{orderGroupId} is now {newStatus}.");
            await notif.NotifyOrderStatusChangedAsync(job.Order.UserId, job.Order.Id, mappedOrderStatus.ToString());

            await BroadcastGroupedOrderStatusAsync(trackingHub, groupedOrders, job);
        }
    }

    private static async Task<List<Order>> LoadGroupedOrdersAsync(ApplicationDbContext db, string orderGroupId)
    {
        if (orderGroupId.StartsWith("S-") && int.TryParse(orderGroupId[2..], out var singleOrderId))
        {
            var singleOrder = await db.Orders
                .AsNoTracking()
                .Include(order => order.Items)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(order => order.Id == singleOrderId);

            return singleOrder is null ? new List<Order>() : new List<Order> { singleOrder };
        }

        return await db.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .Where(order => order.OrderGroupId == orderGroupId)
            .OrderBy(order => order.CreatedAtUtc)
            .ThenBy(order => order.Id)
            .ToListAsync();
    }

    private static async Task BroadcastGroupedOrderStatusAsync(
        IHubContext<TrackingHub> trackingHub,
        IReadOnlyCollection<Order> groupedOrders,
        DeliveryJob? job)
    {
        if (groupedOrders.Count == 0) return;

        var orderGroupId = OrderGroupHelper.ResolveGroupId(groupedOrders.First());
        var groupedStatus = OrderGroupHelper.ComputeGroupedStatus(groupedOrders, job?.Status);

        await trackingHub.Clients.Group(OrderGroupHelper.BuildTrackingGroupName(orderGroupId))
            .SendAsync("order:status_updated", new
            {
                orderGroupId,
                status = groupedStatus,
                deliveryJobId = job?.Id,
                deliveryStatus = job?.Status.ToString()
            });

        foreach (var groupedOrder in groupedOrders)
        {
            await trackingHub.Clients.Group($"order_{groupedOrder.Id}")
                .SendAsync("order:status_updated", new
                {
                    orderId = groupedOrder.Id,
                    orderGroupId,
                    status = groupedOrder.Status.ToString(),
                    deliveryJobId = job?.Id,
                    deliveryStatus = job?.Status.ToString()
                });
        }
    }
}
