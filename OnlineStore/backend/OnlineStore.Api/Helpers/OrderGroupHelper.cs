using Microsoft.EntityFrameworkCore;
using OnlineStore.Api.Contracts;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Helpers;

public sealed record ShopSummary(string? ShopName, string? Location);

public static class OrderGroupHelper
{
    public static string ResolveGroupId(Order order) =>
        string.IsNullOrWhiteSpace(order.OrderGroupId) ? $"S-{order.Id}" : order.OrderGroupId!;

    public static async Task<(Dictionary<string, ShopSummary> ShopsByUserId, Dictionary<string, (double? Latitude, double? Longitude, string? Country)> CoordinatesByUserId)> LoadGroupMetadataAsync(
        ApplicationDbContext db,
        IReadOnlyCollection<Order> orders)
    {
        var storeUserIds = orders.Select(o => o.StoreUserId).Distinct().ToList();
        var customerUserIds = orders.Select(o => o.UserId).Distinct().ToList();
        var allRelatedUserIds = storeUserIds.Concat(customerUserIds).Distinct().ToList();

        var shopProfilesList = await db.ShopProfiles
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

        var usersList = await db.Users
            .AsNoTracking()
            .Where(user => allRelatedUserIds.Contains(user.Id))
            .ToListAsync();
        var coordinatesByUserId = usersList
            .ToDictionary(
                user => user.Id,
                user => (user.Latitude, user.Longitude, user.Country)
            );

        return (shopsByUserId, coordinatesByUserId);
    }

    public static string ComputeGroupedStatus(IReadOnlyCollection<Order> orders, DeliveryStatus? deliveryStatus = null)
    {
        if (orders.Count == 0) return OrderStatus.Pending.ToString();
        if (orders.All(o => o.Status == OrderStatus.Cancelled)) return OrderStatus.Cancelled.ToString();

        return deliveryStatus switch
        {
            DeliveryStatus.Assigned => "Assigned",
            DeliveryStatus.PickedUp => OrderStatus.PickedUp.ToString(),
            DeliveryStatus.OnTheWay => OrderStatus.OnTheWay.ToString(),
            DeliveryStatus.Delivered => OrderStatus.Delivered.ToString(),
            _ => ComputePreDeliveryStatus(orders)
        };
    }

    public static GroupedOrderDto CreateGroupedOrderDto(
        IReadOnlyCollection<Order> orders,
        IReadOnlyDictionary<string, ShopSummary> shopsByUserId,
        IReadOnlyDictionary<string, (double? Latitude, double? Longitude, string? Country)> coordinatesByUserId,
        DeliveryJob? deliveryJob = null)
    {
        var orderedOrders = orders
            .OrderBy(o => o.CreatedAtUtc)
            .ThenBy(o => o.Id)
            .ToList();

        var representative = orderedOrders[0];
        var orderGroupId = ResolveGroupId(representative);
        var customerCoords = coordinatesByUserId.GetValueOrDefault(representative.UserId);

        var stores = orderedOrders.Select(order =>
        {
            var storeCoords = coordinatesByUserId.GetValueOrDefault(order.StoreUserId);
            var shop = shopsByUserId.GetValueOrDefault(order.StoreUserId);
            var currencyCode = CurrencyHelper.GetCurrencyCode(storeCoords.Country);

            return new GroupedOrderStoreDto(
                order.Id,
                order.StoreUserId,
                shop?.ShopName,
                shop?.Location,
                order.Status.ToString(),
                order.TotalAmount,
                storeCoords.Latitude,
                storeCoords.Longitude,
                order.Items.Select(item => new OrderItemDto(
                    item.Id,
                    item.ProductId,
                    item.Product?.Name ?? string.Empty,
                    item.Quantity,
                    item.UnitPrice,
                    item.Product?.StoreUserId ?? order.StoreUserId,
                    currencyCode
                )).ToList(),
                currencyCode
            );
        }).ToList();

        return new GroupedOrderDto(
            orderGroupId,
            representative.Id,
            representative.UserId,
            ComputeGroupedStatus(orderedOrders, deliveryJob?.Status),
            orderedOrders.Sum(order => order.TotalAmount),
            representative.DeliveryAddress ?? string.Empty,
            representative.PaymentMethod,
            representative.OfferCode,
            stores,
            orderedOrders.Min(order => order.CreatedAtUtc),
            orderedOrders.Max(order => order.UpdatedAtUtc),
            customerCoords.Latitude,
            customerCoords.Longitude,
            deliveryJob?.Id,
            deliveryJob?.Status.ToString()
        );
    }

    private static string ComputePreDeliveryStatus(IReadOnlyCollection<Order> orders)
    {
        if (orders.All(o => o.Status == OrderStatus.Delivered)) return OrderStatus.Delivered.ToString();
        if (orders.Any(o => o.Status == OrderStatus.OnTheWay)) return OrderStatus.OnTheWay.ToString();
        if (orders.Any(o => o.Status == OrderStatus.PickedUp)) return OrderStatus.PickedUp.ToString();
        if (orders.All(o => o.Status == OrderStatus.ReadyForPickup)) return OrderStatus.ReadyForPickup.ToString();
        if (orders.Any(o => o.Status == OrderStatus.Preparing || o.Status == OrderStatus.ReadyForPickup))
            return OrderStatus.Preparing.ToString();
        if (orders.Any(o => o.Status == OrderStatus.Pending)) return OrderStatus.Pending.ToString();
        if (orders.Any(o => o.Status == OrderStatus.Cancelled)) return OrderStatus.Cancelled.ToString();

        return orders
            .OrderByDescending(o => o.UpdatedAtUtc)
            .First()
            .Status
            .ToString();
    }
}
