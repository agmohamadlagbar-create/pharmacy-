using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class CustomerSupportService : ICustomerSupportService
{
    private readonly ApplicationDbContext _db;

    public CustomerSupportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<OrderDetailsDto?> GetOrderDetailsAsync(string userId, int orderId)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null) return null;

        return new OrderDetailsDto
        {
            OrderId = order.Id,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            DeliveryAddress = order.DeliveryAddress ?? "Pickup/In-Store",
            CreatedAt = order.CreatedAtUtc,
            Items = order.Items.Select(i => new SupportOrderItemDto
            {
                ProductId = i.ProductId ?? 0,
                ProductName = i.Product?.Name ?? "Unknown Product",
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        };
    }

    public async Task<OrderTrackingDto?> TrackOrderAsync(string userId, int orderId)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null) return null;

        var deliveryJob = await _db.DeliveryJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.OrderId == orderId);

        return new OrderTrackingDto
        {
            OrderId = order.Id,
            OrderStatus = order.Status.ToString(),
            DeliveryStatus = deliveryJob?.Status.ToString() ?? "Not Assigned",
            UpdatedAt = order.UpdatedAtUtc
        };
    }

    public async Task<DeliveryEtaDto?> GetDeliveryEtaAsync(string userId, int orderId)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null) return null;

        if (order.Status == OrderStatus.Delivered)
        {
            return new DeliveryEtaDto
            {
                OrderId = order.Id,
                EstimatedDeliveryTime = "Already Delivered",
                IsDelivered = true
            };
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return new DeliveryEtaDto
            {
                OrderId = order.Id,
                EstimatedDeliveryTime = "Order Cancelled",
                IsDelivered = false
            };
        }

        var deliveryJob = await _db.DeliveryJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.OrderId == orderId);

        string etaMessage = "Calculating ETA...";
        
        if (deliveryJob?.AcceptedAtUtc != null)
        {
            // Simple mockup: delivery takes roughly 45 mins from acceptance
            var estimatedTime = deliveryJob.AcceptedAtUtc.Value.AddMinutes(45);
            etaMessage = $"Estimated arrival by {estimatedTime.ToLocalTime():t}";
        }
        else if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Preparing)
        {
            etaMessage = "Order is being prepared. ETA will be available once a driver picks it up.";
        }
        else
        {
            etaMessage = "Waiting for driver assignment.";
        }

        return new DeliveryEtaDto
        {
            OrderId = order.Id,
            EstimatedDeliveryTime = etaMessage,
            IsDelivered = false
        };
    }

    public async Task<RefundStatusDto?> GetRefundStatusAsync(string userId, int orderId)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null) return null;

        var dto = new RefundStatusDto { OrderId = order.Id };

        if (order.Status == OrderStatus.Cancelled)
        {
            dto.IsRefunded = true; // Treating cancelled orders as refunded for this logic
            dto.RefundStatusMessage = "Your order was cancelled. The refund has been initiated and should appear in your account shortly.";
        }
        else
        {
            dto.IsRefunded = false;
            dto.RefundStatusMessage = "This order is active or completed. No refund has been requested.";
        }

        return dto;
    }
}
