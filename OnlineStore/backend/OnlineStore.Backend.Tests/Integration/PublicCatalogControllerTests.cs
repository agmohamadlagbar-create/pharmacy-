using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineStore.Api.Services;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Backend.Tests.Integration;

public class PublicCatalogControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string StoreUserId = "test-store-public";
    private const string CustomerUserId = "test-customer-public";

    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public PublicCatalogControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAds_ReturnsBannersOrderedBySortOrder()
    {
        await SeedAdsAsync();

        var response = await _client.GetAsync("/api/ads");
        var json = await response.Content.ReadAsStringAsync();
        var ads = JsonNode.Parse(json)?.AsArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(ads);
        Assert.Equal(3, ads!.Count);
        Assert.Equal("Hero Sale", ads[0]?["title"]?.GetValue<string>());
        Assert.Equal("New Arrivals", ads[1]?["title"]?.GetValue<string>());
        Assert.Equal("Late Promo", ads[2]?["title"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetPublicShopInfo_UsesUserAddressAndProfileFields()
    {
        await SeedShopAsync();

        var response = await _client.GetAsync($"/api/shops/{StoreUserId}");
        var json = await response.Content.ReadAsStringAsync();
        var shop = JsonNode.Parse(json)?.AsObject();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(shop);
        Assert.Equal(StoreUserId, shop!["userId"]?.GetValue<string>());
        Assert.Equal("Fresh Test Market", shop["shopName"]?.GetValue<string>());
        Assert.Equal("Market", shop["storeType"]?.GetValue<string>());
        Assert.Equal("Istiklal Street, 42, Istanbul, Turkey", shop["location"]?.GetValue<string>());
        Assert.Equal("/media/profile.png", shop["profilePictureUrl"]?.GetValue<string>());
        Assert.Equal("08:00", shop["openingTime"]?.GetValue<string>());
        Assert.Equal("22:00", shop["closingTime"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetPublicShopInfo_ReturnsNotFoundForUnknownShop()
    {
        var response = await _client.GetAsync("/api/shops/missing-shop-user");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetApprovedProducts_ReturnsOnlyApprovedProductsWithRatings()
    {
        await SeedCatalogAsync();

        var response = await _client.GetAsync("/api/products/approved?page=1&limit=10");
        var json = await response.Content.ReadAsStringAsync();
        var body = JsonNode.Parse(json)?.AsObject();
        var data = body?["data"]?.AsArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotNull(data);
        Assert.Equal(2, body!["total"]?.GetValue<int>());
        Assert.Equal(1, body["page"]?.GetValue<int>());
        Assert.Equal(10, body["limit"]?.GetValue<int>());

        var names = data!
            .Select(node => node?["name"]?.GetValue<string>())
            .OfType<string>()
            .ToList();

        Assert.Contains("Test Fresh Milk", names);
        Assert.Contains("Test Whole Wheat Bread", names);
        Assert.DoesNotContain("Test Pending Apples", names);

        var milk = data.First(node => node?["name"]?.GetValue<string>() == "Test Fresh Milk")!;
        Assert.Equal("Fresh Test Market", milk["storeName"]?.GetValue<string>());
        Assert.Equal("Turkey", milk["storeCountry"]?.GetValue<string>());
        Assert.Equal("Istanbul", milk["storeCity"]?.GetValue<string>());
        Assert.Equal(4.5, milk["averageRating"]?.GetValue<double>());
        Assert.Equal(2, milk["reviewCount"]?.GetValue<int>());
    }

    [Fact]
    public async Task GetApprovedProducts_AppliesSearchAndPriceFilters()
    {
        await SeedCatalogAsync();

        var response = await _client.GetAsync("/api/products/approved?search=milk&minPrice=10&maxPrice=20&page=1&limit=10");
        var json = await response.Content.ReadAsStringAsync();
        var body = JsonNode.Parse(json)?.AsObject();
        var data = body?["data"]?.AsArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotNull(data);
        Assert.Equal(1, body!["total"]?.GetValue<int>());
        Assert.Single(data!);
        Assert.Equal("Test Fresh Milk", data[0]?["name"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetProductDetails_ReturnsStatsReviewsAndRelatedProducts()
    {
        var seeded = await SeedCatalogAsync();

        var response = await _client.GetAsync($"/api/products/{seeded.MilkProductId}/details");
        var json = await response.Content.ReadAsStringAsync();
        var details = JsonNode.Parse(json)?.AsObject();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(details);
        Assert.Equal("Test Fresh Milk", details!["product"]?["name"]?.GetValue<string>());
        Assert.Equal(5, details["totalSales"]?.GetValue<int>());
        Assert.Equal(12, details["inStock"]?.GetValue<int>());
        Assert.Equal(4.5, details["averageRating"]?.GetValue<double>());
        Assert.Equal(2, details["totalReviews"]?.GetValue<int>());

        var reviews = details["latestReviews"]?.AsArray();
        Assert.NotNull(reviews);
        Assert.Equal(2, reviews!.Count);
        Assert.Equal("Great milk", reviews[0]?["comment"]?.GetValue<string>());

        var related = details["relatedProducts"]?.AsArray();
        Assert.NotNull(related);
        Assert.Single(related!);
        Assert.Equal("Test Whole Wheat Bread", related[0]?["name"]?.GetValue<string>());

        var dailySales = details["dailySales"]?.AsArray();
        Assert.NotNull(dailySales);
        Assert.Equal(5, dailySales!.Count);
    }

    [Fact]
    public async Task GetProductDetails_ReturnsNotFoundForPendingProduct()
    {
        var seeded = await SeedCatalogAsync();

        var response = await _client.GetAsync($"/api/products/{seeded.PendingProductId}/details");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task SeedAdsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<IStoreCacheService>();

        db.AdBanners.RemoveRange(db.AdBanners);
        db.AdBanners.AddRange(
            new AdBanner
            {
                ImageUrl = "/media/late.png",
                Title = "Late Promo",
                LinkUrl = "/promo",
                SortOrder = 3,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3)
            },
            new AdBanner
            {
                ImageUrl = "/media/hero.png",
                Title = "Hero Sale",
                LinkUrl = "/sale",
                SortOrder = 1,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2)
            },
            new AdBanner
            {
                ImageUrl = "/media/new.png",
                Title = "New Arrivals",
                LinkUrl = "/new",
                SortOrder = 2,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1)
            }
        );

        await db.SaveChangesAsync();
        await cache.InvalidateAdsCacheAsync();
    }

    private async Task SeedShopAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await ClearPublicCatalogDataAsync(db);
        AddStoreUserAndShop(db);

        await db.SaveChangesAsync();
    }

    private async Task<SeededCatalog> SeedCatalogAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<IStoreCacheService>();
        var memCache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await ClearPublicCatalogDataAsync(db);
        AddStoreUserAndShop(db);
        db.Users.Add(new AppUser
        {
            Id = CustomerUserId,
            UserName = "test-customer-public@example.com",
            NormalizedUserName = "TEST-CUSTOMER-PUBLIC@EXAMPLE.COM",
            Email = "test-customer-public@example.com",
            NormalizedEmail = "TEST-CUSTOMER-PUBLIC@EXAMPLE.COM",
            AccountType = AccountType.User,
            FullName = "Test Customer"
        });

        var category = new Category
        {
            Name = "Test Public Grocery",
            Code = "TEST_PUBLIC_GROCERY",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var milk = new Product
        {
            StoreUserId = StoreUserId,
            CategoryId = category.Id,
            Name = "Test Fresh Milk",
            Description = "Organic milk bottle",
            Price = 12.50m,
            ImageUrl = "/media/milk.png",
            AdditionalImageUrls = new List<string> { "/media/milk-side.png" },
            StockQuantity = 12,
            OriginalQuantity = 20,
            Status = ProductStatus.Approved,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        var bread = new Product
        {
            StoreUserId = StoreUserId,
            CategoryId = category.Id,
            Name = "Test Whole Wheat Bread",
            Description = "Bakery loaf",
            Price = 7.25m,
            ImageUrl = "/media/bread.png",
            StockQuantity = 6,
            OriginalQuantity = 6,
            Status = ProductStatus.Approved,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
        };
        var pending = new Product
        {
            StoreUserId = StoreUserId,
            CategoryId = category.Id,
            Name = "Test Pending Apples",
            Description = "Waiting for approval",
            Price = 5.75m,
            StockQuantity = 30,
            OriginalQuantity = 30,
            Status = ProductStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-3)
        };

        db.Products.AddRange(milk, bread, pending);
        await db.SaveChangesAsync();

        db.Reviews.AddRange(
            new Review
            {
                ProductId = milk.Id,
                UserId = CustomerUserId,
                UserName = "Customer One",
                Rating = 5,
                Comment = "Great milk",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10)
            },
            new Review
            {
                ProductId = milk.Id,
                UserId = "another-customer",
                UserName = "Customer Two",
                Rating = 4,
                Comment = "Good value",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-20)
            }
        );

        var deliveredOrder = new Order
        {
            UserId = CustomerUserId,
            StoreUserId = StoreUserId,
            Status = OrderStatus.Delivered,
            TotalAmount = 62.50m,
            DeliveryAddress = "Istanbul",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = milk.Id,
                    Quantity = 5,
                    UnitPrice = 12.50m
                }
            }
        };

        var cancelledOrder = new Order
        {
            UserId = CustomerUserId,
            StoreUserId = StoreUserId,
            Status = OrderStatus.Cancelled,
            TotalAmount = 25.00m,
            DeliveryAddress = "Istanbul",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = milk.Id,
                    Quantity = 2,
                    UnitPrice = 12.50m
                }
            }
        };

        db.Orders.AddRange(deliveredOrder, cancelledOrder);
        await db.SaveChangesAsync();

        await cache.InvalidateProductsCacheAsync();
        await cache.InvalidateCategoriesCacheAsync();
        await memCache.RemoveAsync($"product_details_{milk.Id}");
        await memCache.RemoveAsync($"product_details_{pending.Id}");

        return new SeededCatalog(milk.Id, pending.Id);
    }

    private static void AddStoreUserAndShop(ApplicationDbContext db)
    {
        db.Users.Add(new AppUser
        {
            Id = StoreUserId,
            UserName = "test-store-public@example.com",
            NormalizedUserName = "TEST-STORE-PUBLIC@EXAMPLE.COM",
            Email = "test-store-public@example.com",
            NormalizedEmail = "TEST-STORE-PUBLIC@EXAMPLE.COM",
            AccountType = AccountType.Shop,
            Country = "Turkey",
            City = "Istanbul",
            Street = "Istiklal Street",
            BuildingNumber = "42",
            ProfilePictureUrl = "/media/profile.png"
        });

        db.ShopProfiles.Add(new ShopProfile
        {
            UserId = StoreUserId,
            ShopName = "Fresh Test Market",
            StoreType = "Market",
            RegionCode = "TR-34",
            Location = "Fallback Location",
            Description = "Local test groceries",
            OpeningTime = "08:00",
            ClosingTime = "22:00",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        });
    }

    private static async Task ClearPublicCatalogDataAsync(ApplicationDbContext db)
    {
        var productIds = await db.Products
            .Where(p => p.StoreUserId == StoreUserId)
            .Select(p => p.Id)
            .ToListAsync();

        db.OrderItems.RemoveRange(db.OrderItems.Where(oi => oi.ProductId.HasValue && productIds.Contains(oi.ProductId.Value)));
        db.Orders.RemoveRange(db.Orders.Where(o => o.StoreUserId == StoreUserId || o.UserId == CustomerUserId));
        db.Reviews.RemoveRange(db.Reviews.Where(r => productIds.Contains(r.ProductId)));
        db.Products.RemoveRange(db.Products.Where(p => p.StoreUserId == StoreUserId));
        db.Categories.RemoveRange(db.Categories.Where(c => c.Code.StartsWith("TEST_PUBLIC_")));
        db.ShopProfiles.RemoveRange(db.ShopProfiles.Where(s => s.UserId == StoreUserId));
        db.Users.RemoveRange(db.Users.Where(u => u.Id == StoreUserId || u.Id == CustomerUserId));

        await db.SaveChangesAsync();
    }

    private sealed record SeededCatalog(int MilkProductId, int PendingProductId);
}
