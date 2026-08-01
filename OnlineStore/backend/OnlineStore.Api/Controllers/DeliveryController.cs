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
[Authorize(Roles = "Delivery")]
public class DeliveryController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly INotificationService _notif;
    private readonly IHubContext<OnlineStore.Api.Hubs.TrackingHub> _trackingHub;
    private readonly ILogger<DeliveryController> _logger;
    private readonly INotificationQueue _notifQueue;

    public DeliveryController(ApplicationDbContext db, UserManager<AppUser> userManager, INotificationService notif, IHubContext<OnlineStore.Api.Hubs.TrackingHub> trackingHub, ILogger<DeliveryController> logger, INotificationQueue notifQueue)
    {
        _db = db;
        _userManager = userManager;
        _notif = notif;
        _trackingHub = trackingHub;
        _logger = logger;
        _notifQueue = notifQueue;
    }

    /// <summary>Get available (unassigned) delivery jobs.</summary>
    [HttpGet("available")]
    public async Task<ActionResult<List<DeliveryJobDto>>> GetAvailable()
    {
        var jobs = await _db.DeliveryJobs
            .AsNoTracking()
            .Include(j => j.Order).ThenInclude(o => o.Items).ThenInclude(i => i.Product)
            .Where(j => j.Status == DeliveryStatus.Unassigned)
            .OrderByDescending(j => j.CreatedAtUtc)
            .ToListAsync();

        return Ok(await MapJobDtos(jobs));
    }

    /// <summary>Get my assigned delivery jobs.</summary>
    [HttpGet("my-jobs")]
    public async Task<ActionResult<List<DeliveryJobDto>>> GetMyJobs()
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var jobs = await _db.DeliveryJobs
            .AsNoTracking()
            .Include(j => j.Order).ThenInclude(o => o.Items).ThenInclude(i => i.Product)
            .Where(j => j.DeliveryUserId == userId)
            .OrderByDescending(j => j.CreatedAtUtc)
            .ToListAsync();

        return Ok(await MapJobDtos(jobs));
    }

    /// <summary>Accept a delivery job (atomic, first come first served).</summary>
    [HttpPost("{jobId:int}/accept")]
    public async Task<IActionResult> Accept(int jobId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        // Use optimistic concurrency: only update if still Unassigned
        var rowsAffected = await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE DeliveryJobs 
               SET DeliveryUserId = {userId}, 
                   Status = {(int)DeliveryStatus.Assigned}, 
                   AcceptedAtUtc = {DateTime.UtcNow}
               WHERE Id = {jobId} 
                 AND Status = {(int)DeliveryStatus.Unassigned} 
                 AND DeliveryUserId IS NULL");

        if (rowsAffected == 0)
            return BadRequest(new { error = "This delivery job is no longer available." });

        // Enqueue background notifications (<50ms response)
        await _notifQueue.QueueNotificationAsync(new NotificationWorkItem(
            NotificationTaskType.DeliveryAcceptJob,
            0, null, null, null, jobId
        ));

        return Ok(new MessageResponse("Delivery job accepted successfully."));
    }

    /// <summary>Update delivery status: Assigned→PickedUp→OnTheWay→Delivered.</summary>
    [HttpPut("{jobId:int}/status")]
    public async Task<IActionResult> UpdateStatus(int jobId, UpdateDeliveryStatusRequest req)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Unauthorized();

        var job = await _db.DeliveryJobs
            .AsTracking()
            .Include(j => j.Order)
            .FirstOrDefaultAsync(j => j.Id == jobId);
        if (job is null) return NotFound();
        if (job.DeliveryUserId != userId) return Forbid();

        if (!Enum.TryParse<DeliveryStatus>(req.Status, true, out var newStatus))
            return BadRequest(new { error = "Invalid status" });

        var allowedTransitions = new Dictionary<DeliveryStatus, DeliveryStatus[]>
        {
            [DeliveryStatus.Assigned] = [DeliveryStatus.PickedUp],
            [DeliveryStatus.PickedUp] = [DeliveryStatus.OnTheWay],
            [DeliveryStatus.OnTheWay] = [DeliveryStatus.Delivered]
        };

        if (!allowedTransitions.TryGetValue(job.Status, out var allowed) || !allowed.Contains(newStatus))
            return BadRequest(new { error = $"Cannot transition from {job.Status} to {newStatus}" });

        job.Status = newStatus;
        if (newStatus == DeliveryStatus.Delivered)
            job.DeliveredAtUtc = DateTime.UtcNow;

        // Sync order status
        if (job.Order is not null)
        {
            var groupedOrders = await LoadGroupedOrdersAsync(OrderGroupHelper.ResolveGroupId(job.Order));
            var mappedOrderStatus = newStatus switch
            {
                DeliveryStatus.PickedUp => OrderStatus.PickedUp,
                DeliveryStatus.OnTheWay => OrderStatus.OnTheWay,
                DeliveryStatus.Delivered => OrderStatus.Delivered,
                _ => job.Order.Status
            };

            foreach (var groupedOrder in groupedOrders)
            {
                groupedOrder.Status = mappedOrderStatus;
                groupedOrder.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        // Enqueue background notifications (<50ms response)
        await _notifQueue.QueueNotificationAsync(new NotificationWorkItem(
            NotificationTaskType.DeliveryStatusUpdate,
            job.OrderId,
            job.Order != null ? OrderGroupHelper.ResolveGroupId(job.Order) : null,
            job.Order?.UserId,
            newStatus.ToString(),
            jobId
        ));

        return Ok(new MessageResponse($"Delivery status updated to {newStatus}"));
    }

    private async Task<List<DeliveryJobDto>> MapJobDtos(List<DeliveryJob> jobs)
    {
        if (jobs.Count == 0) return new List<DeliveryJobDto>();

        var orderGroupIds = jobs
            .Where(job => job.Order is not null)
            .Select(job => OrderGroupHelper.ResolveGroupId(job.Order))
            .Distinct()
            .ToList();

        var groupedOrders = await _db.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .Where(order => order.OrderGroupId != null && orderGroupIds.Contains(order.OrderGroupId))
            .ToListAsync();

        var storeUserIds = groupedOrders.Select(order => order.StoreUserId).Distinct().ToList();
        var customerUserIds = groupedOrders.Select(order => order.UserId).Distinct().ToList();
        var allRelatedUserIds = storeUserIds.Concat(customerUserIds).Distinct().ToList();

        var shopProfilesList = await _db.ShopProfiles
            .AsNoTracking()
            .Where(shop => storeUserIds.Contains(shop.UserId))
            .ToListAsync();
        var shopsByUserId = shopProfilesList
            .GroupBy(shop => shop.UserId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var shop = group
                        .OrderByDescending(x => x.Id)
                        .First();
                    return new ShopSummary(shop.ShopName, shop.Location);
                }
            );

        var usersList = await _db.Users
            .AsNoTracking()
            .Where(user => allRelatedUserIds.Contains(user.Id))
            .ToListAsync();
        var coordinatesByUserId = usersList
            .ToDictionary(
                user => user.Id,
                user => (user.Latitude, user.Longitude, user.Country)
            );

        var groupedOrdersById = groupedOrders
            .GroupBy(OrderGroupHelper.ResolveGroupId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return jobs.Select(job =>
        {
            GroupedOrderDto? orderGroupDto = null;
            string? orderGroupId = null;

            if (job.Order is not null)
            {
                orderGroupId = OrderGroupHelper.ResolveGroupId(job.Order);

                if (groupedOrdersById.TryGetValue(orderGroupId, out var ordersInGroup))
                {
                    orderGroupDto = OrderGroupHelper.CreateGroupedOrderDto(ordersInGroup, shopsByUserId, coordinatesByUserId, job);
                }
            }

            return new DeliveryJobDto(
                job.Id,
                job.OrderId,
                orderGroupId,
                job.DeliveryUserId,
                job.Status.ToString(),
                job.CreatedAtUtc,
                job.AcceptedAtUtc,
                job.DeliveredAtUtc,
                orderGroupDto
            );
        }).ToList();
    }

    private async Task<List<Order>> LoadGroupedOrdersAsync(string orderGroupId)
    {
        if (orderGroupId.StartsWith("S-") && int.TryParse(orderGroupId[2..], out var singleOrderId))
        {
            var singleOrder = await _db.Orders
                .AsTracking()
                .Include(order => order.Items)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(order => order.Id == singleOrderId);

            return singleOrder is null ? new List<Order>() : new List<Order> { singleOrder };
        }

        return await _db.Orders
            .AsTracking()
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .Where(order => order.OrderGroupId == orderGroupId)
            .OrderBy(order => order.CreatedAtUtc)
            .ThenBy(order => order.Id)
            .ToListAsync();
    }

    private async Task BroadcastGroupedOrderStatusAsync(
        IReadOnlyCollection<Order> groupedOrders,
        DeliveryJob job)
    {
        if (groupedOrders.Count == 0) return;

        var orderGroupId = OrderGroupHelper.ResolveGroupId(groupedOrders.First());
        var groupedStatus = OrderGroupHelper.ComputeGroupedStatus(groupedOrders, job.Status);

        await _trackingHub.Clients.Group(OrderGroupHelper.BuildTrackingGroupName(orderGroupId))
            .SendAsync("order:status_updated", new
            {
                orderGroupId,
                status = groupedStatus,
                deliveryJobId = job.Id,
                deliveryStatus = job.Status.ToString()
            });

        foreach (var groupedOrder in groupedOrders)
        {
            await _trackingHub.Clients.Group($"order_{groupedOrder.Id}")
                .SendAsync("order:status_updated", new
                {
                    orderId = groupedOrder.Id,
                    orderGroupId,
                    status = groupedOrder.Status.ToString(),
                    deliveryJobId = job.Id,
                    deliveryStatus = job.Status.ToString()
                });
        }
    }
}
