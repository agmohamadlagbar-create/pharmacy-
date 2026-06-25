using OnlineStore.Api.Helpers;
using OnlineStore.Domain.Entities;

namespace OnlineStore.Backend.Tests;

public class OrderGroupHelperTests
{
    [Fact]
    public void ResolveGroupId_FallsBackToSingleOrderId()
    {
        var order = BuildOrder(id: 17, groupId: null, status: OrderStatus.Pending);

        var groupId = OrderGroupHelper.ResolveGroupId(order);

        Assert.Equal("S-17", groupId);
    }

    [Fact]
    public void ComputeGroupedStatus_UsesDeliveryStatusWhenAssigned()
    {
        var orders = new[]
        {
            BuildOrder(id: 1, groupId: "G-100", status: OrderStatus.ReadyForPickup),
            BuildOrder(id: 2, groupId: "G-100", status: OrderStatus.ReadyForPickup)
        };

        var groupedStatus = OrderGroupHelper.ComputeGroupedStatus(orders, DeliveryStatus.Assigned);

        Assert.Equal("Assigned", groupedStatus);
    }

    [Fact]
    public void ComputeGroupedStatus_ReturnsPreparingWhileStoresAreMixed()
    {
        var orders = new[]
        {
            BuildOrder(id: 1, groupId: "G-200", status: OrderStatus.Pending),
            BuildOrder(id: 2, groupId: "G-200", status: OrderStatus.Preparing),
            BuildOrder(id: 3, groupId: "G-200", status: OrderStatus.ReadyForPickup)
        };

        var groupedStatus = OrderGroupHelper.ComputeGroupedStatus(orders);

        Assert.Equal(OrderStatus.Preparing.ToString(), groupedStatus);
    }

    [Fact]
    public void CreateGroupedOrderDto_AggregatesTotalsAndStoreDetails()
    {
        var orders = new[]
        {
            BuildOrder(
                id: 10,
                groupId: "G-300",
                status: OrderStatus.Preparing,
                totalAmount: 25m,
                storeUserId: "store-a",
                itemName: "Apples",
                quantity: 2
            ),
            BuildOrder(
                id: 11,
                groupId: "G-300",
                status: OrderStatus.ReadyForPickup,
                totalAmount: 40m,
                storeUserId: "store-b",
                itemName: "Bread",
                quantity: 1
            )
        };

        var shops = new Dictionary<string, ShopSummary>
        {
            ["store-a"] = new("Fresh Market", "North Street"),
            ["store-b"] = new("Bakery House", "East Avenue")
        };

        var coords = new Dictionary<string, (double? Latitude, double? Longitude, string? Country)>
        {
            ["user-1"] = (41.0, 29.0, "Turkey"),
            ["store-a"] = (41.1, 29.1, "Turkey"),
            ["store-b"] = (41.2, 29.2, "Turkey")
        };

        var deliveryJob = new DeliveryJob
        {
            Id = 55,
            Status = DeliveryStatus.Assigned
        };

        var grouped = OrderGroupHelper.CreateGroupedOrderDto(orders, shops, coords, deliveryJob);

        Assert.Equal("G-300", grouped.OrderGroupId);
        Assert.Equal(65m, grouped.TotalAmount);
        Assert.Equal(2, grouped.Stores.Count);
        Assert.Equal("Fresh Market", grouped.Stores[0].StoreName);
        Assert.Equal(55, grouped.DeliveryJobId);
        Assert.Equal("Assigned", grouped.Status);
    }

    private static Order BuildOrder(
        int id,
        string? groupId,
        OrderStatus status,
        decimal totalAmount = 10m,
        string userId = "user-1",
        string storeUserId = "store-1",
        string itemName = "Item",
        int quantity = 1)
    {
        return new Order
        {
            Id = id,
            UserId = userId,
            StoreUserId = storeUserId,
            Status = status,
            TotalAmount = totalAmount,
            DeliveryAddress = "Main Street",
            PaymentMethod = "Credit Card",
            OfferCode = "PROMO",
            OrderGroupId = groupId,
            CreatedAtUtc = new DateTime(2026, 1, 1).AddMinutes(id),
            UpdatedAtUtc = new DateTime(2026, 1, 1).AddMinutes(id + 5),
            Items =
            [
                new OrderItem
                {
                    Id = id * 10,
                    ProductId = id * 100,
                    Quantity = quantity,
                    UnitPrice = totalAmount / Math.Max(quantity, 1),
                    Product = new Product
                    {
                        Id = id * 100,
                        Name = itemName,
                        StoreUserId = storeUserId
                    }
                }
            ]
        };
    }
}
