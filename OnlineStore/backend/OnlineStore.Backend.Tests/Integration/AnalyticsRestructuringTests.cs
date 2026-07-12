using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Services;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;
using Xunit;

namespace OnlineStore.Backend.Tests.Integration;

public class AnalyticsRestructuringTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AnalyticsRestructuringTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnalyticsFlow_WorksCorrectly()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

        // Ensure database is clean
        db.StoreAnalyticsEvents.RemoveRange(db.StoreAnalyticsEvents);
        db.StoreAnalyticsDailies.RemoveRange(db.StoreAnalyticsDailies);
        db.StoreAnalyticsMonthlies.RemoveRange(db.StoreAnalyticsMonthlies);
        db.StoreAnalyticsYearlies.RemoveRange(db.StoreAnalyticsYearlies);
        
        // Remove orders and products first to avoid foreign keys on user deletion
        db.OrderItems.RemoveRange(db.OrderItems);
        db.Orders.RemoveRange(db.Orders);
        db.Products.RemoveRange(db.Products);
        db.Categories.RemoveRange(db.Categories);
        db.ShopProfiles.RemoveRange(db.ShopProfiles);
        
        var testUserIds = new[] { "store_test_owner_1", "customer_test_user_1" };
        var existingUsers = db.Users.Where(u => testUserIds.Contains(u.Id));
        db.Users.RemoveRange(existingUsers);
        
        await db.SaveChangesAsync();

        var storeUserId = "store_test_owner_1";
        var customerUserId = "customer_test_user_1";

        // Seed users and shop profile
        var storeUser = new AppUser
        {
            Id = storeUserId,
            UserName = "test-store-analytics@example.com",
            NormalizedUserName = "TEST-STORE-ANALYTICS@EXAMPLE.COM",
            Email = "test-store-analytics@example.com",
            NormalizedEmail = "TEST-STORE-ANALYTICS@EXAMPLE.COM",
            AccountType = AccountType.Shop,
            Country = "Turkey",
            City = "Istanbul"
        };
        var customerUser = new AppUser
        {
            Id = customerUserId,
            UserName = "test-customer-analytics@example.com",
            NormalizedUserName = "TEST-CUSTOMER-ANALYTICS@EXAMPLE.COM",
            Email = "test-customer-analytics@example.com",
            NormalizedEmail = "TEST-CUSTOMER-ANALYTICS@EXAMPLE.COM",
            AccountType = AccountType.User,
            Country = "Turkey",
            City = "Istanbul"
        };
        db.Users.AddRange(storeUser, customerUser);

        var shopProfile = new ShopProfile
        {
            UserId = storeUserId,
            ShopName = "Test Shop",
            Location = "Test Location",
            StoreType = "Grocery",
            RegionCode = "TR-34",
            OpeningTime = "08:00",
            ClosingTime = "22:00",
            LowStockThreshold = 10
        };
        db.ShopProfiles.Add(shopProfile);

        // Seed category
        var category = new Category
        {
            Name = "Test Analytics Grocery",
            Code = "TEST_ANALYTICS_GROCERY"
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        // Seed a product
        var product = new Product
        {
            StoreUserId = storeUserId,
            CategoryId = category.Id,
            Name = "Test Aggregation Product",
            Price = 10.00m,
            StockQuantity = 100,
            OriginalQuantity = 100,
            Status = ProductStatus.Approved
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // 1. Verify Order Creation generates Raw Events
        var order1 = new Order
        {
            StoreUserId = storeUserId,
            UserId = customerUserId,
            Status = OrderStatus.Pending,
            TotalAmount = 20.00m,
            CreatedAtUtc = DateTime.UtcNow.Date.AddDays(-8),
            UpdatedAtUtc = DateTime.UtcNow.Date.AddDays(-8),
            OrderGroupId = "G-GROUP1"
        };
        order1.Items.Add(new OrderItem
        {
            ProductId = product.Id,
            Quantity = 2,
            UnitPrice = 10.00m
        });

        db.Orders.Add(order1);
        await db.SaveChangesAsync();

        var rawEvents = db.StoreAnalyticsEvents.ToList();
        Assert.Single(rawEvents);
        Assert.Equal(20.00m, rawEvents[0].TotalAmount);
        Assert.Equal(storeUserId, rawEvents[0].StoreUserId);

        // 2. Verify Order Cancellation removes Raw Events (if not aggregated yet)
        var orderToDelete = new Order
        {
            StoreUserId = storeUserId,
            UserId = customerUserId,
            Status = OrderStatus.Pending,
            TotalAmount = 30.00m,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            OrderGroupId = "G-GROUP2"
        };
        orderToDelete.Items.Add(new OrderItem
        {
            ProductId = product.Id,
            Quantity = 3,
            UnitPrice = 10.00m
        });
        db.Orders.Add(orderToDelete);
        await db.SaveChangesAsync();

        var eventsBeforeCancel = db.StoreAnalyticsEvents.Count(e => e.OrderId == orderToDelete.Id);
        Assert.Equal(1, eventsBeforeCancel);

        // Now cancel it
        orderToDelete.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync();

        var eventsAfterCancel = db.StoreAnalyticsEvents.Count(e => e.OrderId == orderToDelete.Id);
        Assert.Equal(0, eventsAfterCancel);

        // 3. Setup raw events for another day (2 days ago - within 7 days limit)
        var order2 = new Order
        {
            StoreUserId = storeUserId,
            UserId = customerUserId,
            Status = OrderStatus.Pending,
            TotalAmount = 50.00m,
            CreatedAtUtc = DateTime.UtcNow.Date.AddDays(-2),
            UpdatedAtUtc = DateTime.UtcNow.Date.AddDays(-2),
            OrderGroupId = "G-GROUP3"
        };
        order2.Items.Add(new OrderItem
        {
            ProductId = product.Id,
            Quantity = 5,
            UnitPrice = 10.00m
        });
        db.Orders.Add(order2);
        await db.SaveChangesAsync();

        // 4. Manually run the background aggregation job cycle
        var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AnalyticsAggregationBackgroundService>>();
        var backgroundService = new AnalyticsAggregationBackgroundService(scope.ServiceProvider, logger);

        await backgroundService.RunAggregationCycleAsync(CancellationToken.None);

        // Assert:
        // - Daily record for 8 days ago was generated
        // - Daily record for 2 days ago was generated
        var dailyRecords = db.StoreAnalyticsDailies.OrderBy(d => d.Date).ToList();
        Assert.Equal(2, dailyRecords.Count);

        var daily8DaysAgo = dailyRecords[0];
        Assert.Equal(20.00m, daily8DaysAgo.TotalSales);
        Assert.Equal(1, daily8DaysAgo.TotalOrders);

        var daily2DaysAgo = dailyRecords[1];
        Assert.Equal(50.00m, daily2DaysAgo.TotalSales);
        Assert.Equal(1, daily2DaysAgo.TotalOrders);

        // Assert Monthly & Yearly aggregation rows exist
        var monthlyRecords = db.StoreAnalyticsMonthlies.ToList();
        Assert.NotEmpty(monthlyRecords);
        Assert.Equal(dailyRecords.Sum(d => d.TotalSales), monthlyRecords.Sum(m => m.TotalSales));

        var yearlyRecords = db.StoreAnalyticsYearlies.ToList();
        Assert.NotEmpty(yearlyRecords);
        Assert.Equal(dailyRecords.Sum(d => d.TotalSales), yearlyRecords.Sum(y => y.TotalSales));

        // Assert:
        // - The raw event for 8 days ago was purged (older than 7 days and aggregated)
        // - The raw event for 2 days ago is NOT purged (newer than 7 days)
        var remainingEvents = db.StoreAnalyticsEvents.ToList();
        Assert.Single(remainingEvents);
        Assert.Equal(order2.Id, remainingEvents[0].OrderId);

        // 5. Verify AnalyticsService combines raw and aggregated data
        // Let's create an order for TODAY (so it's completely unaggregated raw events)
        var orderToday = new Order
        {
            StoreUserId = storeUserId,
            UserId = customerUserId,
            Status = OrderStatus.Pending,
            TotalAmount = 100.00m,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            OrderGroupId = "G-GROUP4"
        };
        orderToday.Items.Add(new OrderItem
        {
            ProductId = product.Id,
            Quantity = 10,
            UnitPrice = 10.00m
        });
        db.Orders.Add(orderToday);
        await db.SaveChangesAsync();

        // Query the analytics for the entire range (from 9 days ago to today)
        var analytics = await analyticsService.GetFullAnalyticsAsync(storeUserId, DateTime.UtcNow.AddDays(-9), DateTime.UtcNow);

        // Total sales:
        // Aggregated: 20.00m (8 days ago) + 50.00m (2 days ago) = 70.00m
        // Raw today: 100.00m
        // Total expected = 170.00m
        Assert.Equal(170.00m, analytics.Summary.TotalRevenue);
        Assert.Equal(3, analytics.Summary.TotalOrders);
    }
}
