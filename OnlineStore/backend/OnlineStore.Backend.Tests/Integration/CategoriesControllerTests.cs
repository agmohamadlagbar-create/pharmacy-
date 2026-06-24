using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using OnlineStore.Api.Services;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Backend.Tests.Integration;

public class CategoriesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public CategoriesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsTopLevelCategories()
    {
        await SeedCategoriesAsync();

        var response = await _client.GetAsync("/api/categories");
        var json = await response.Content.ReadAsStringAsync();
        var array = JsonNode.Parse(json)?.AsArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(array);
        Assert.NotEmpty(array);

        var names = array!
            .Select(node => node?["name"]?.GetValue<string>())
            .OfType<string>()
            .ToList();

        Assert.Contains("Test Grocery", names);
    }

    [Fact]
    public async Task GetAll_WithPagination_ReturnsPaginatedEnvelope()
    {
        await SeedCategoriesAsync();

        var response = await _client.GetAsync("/api/categories?page=1&limit=1");
        var json = await response.Content.ReadAsStringAsync();
        var body = JsonNode.Parse(json)?.AsObject();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotNull(body!["data"]);
        Assert.Equal(1, body["page"]?.GetValue<int>());
        Assert.Equal(1, body["limit"]?.GetValue<int>());
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForUnknownCategory()
    {
        var response = await _client.GetAsync("/api/categories/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task SeedCategoriesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<IStoreCacheService>();

        db.Categories.RemoveRange(db.Categories);
        db.Categories.AddRange(
            new Category { Name = "Test Grocery", Code = "TEST_GROCERY" },
            new Category { Name = "Test Bakery", Code = "TEST_BAKERY" },
            new Category { Name = "Test Drinks", Code = "TEST_DRINKS" }
        );

        await db.SaveChangesAsync();
        await cache.InvalidateCategoriesCacheAsync();
    }
}
