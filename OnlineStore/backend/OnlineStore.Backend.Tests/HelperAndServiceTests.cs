using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using OnlineStore.Api.Contracts;
using OnlineStore.Api.Helpers;
using OnlineStore.Api.Services;

namespace OnlineStore.Backend.Tests;

public class HelperAndServiceTests
{
    [Fact]
    public void AuthenticatedUserExtensions_PrefersNameIdentifierClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "jwt-sub"),
            new Claim(ClaimTypes.NameIdentifier, "name-id")
        }));

        var userId = principal.GetAuthenticatedUserId();

        Assert.Equal("name-id", userId);
    }

    [Fact]
    public void AuthenticatedUserExtensions_FallsBackToJwtSubClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "jwt-sub")
        }));

        var userId = principal.GetAuthenticatedUserId();

        Assert.Equal("jwt-sub", userId);
    }

    [Fact]
    public async Task ImageHelper_AcceptsPngHeaderAndRejectsUnknownFormat()
    {
        var png = BuildFormFile("image.png", new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D
        });
        var text = BuildFormFile("note.txt", "not an image"u8.ToArray());

        var valid = await ImageHelper.ValidateImageAsync(png, maxSizeBytes: 1024);
        var invalid = await ImageHelper.ValidateImageAsync(text, maxSizeBytes: 1024);

        Assert.True(valid.IsValid);
        Assert.Equal("image/png", valid.MimeType);
        Assert.False(invalid.IsValid);
        Assert.Contains("Invalid image format", invalid.Error);
    }

    [Fact]
    public void InMemoryVerificationCodeStore_RedeemsCodeOnlyOnce()
    {
        var store = new InMemoryVerificationCodeStore(
            NullLogger<InMemoryVerificationCodeStore>.Instance);
        var code = store.StoreCode("person@example.com", "serialized-user");

        Assert.True(store.HasValidCode("person@example.com"));
        Assert.Equal("serialized-user", store.RedeemCode("person@example.com", code));
        Assert.False(store.HasValidCode("person@example.com"));
        Assert.Null(store.RedeemCode("person@example.com", code));
    }

    [Fact]
    public void InMemoryVerificationCodeStore_InvalidatesAfterThreeBadAttempts()
    {
        var store = new InMemoryVerificationCodeStore(
            NullLogger<InMemoryVerificationCodeStore>.Instance);
        store.StoreCode("person@example.com", "serialized-user");

        Assert.Null(store.RedeemCode("person@example.com", "111111"));
        Assert.Null(store.RedeemCode("person@example.com", "222222"));
        Assert.Null(store.RedeemCode("person@example.com", "333333"));

        Assert.False(store.HasValidCode("person@example.com"));
    }

    [Fact]
    public async Task LocalCacheService_StoresReadsAndRemovesValues()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new LocalCacheService(
            memoryCache,
            NullLogger<LocalCacheService>.Instance);

        await cache.SetAsync("featured", "milk");

        Assert.Equal("milk", await cache.GetAsync<string>("featured"));

        await cache.RemoveAsync("featured");

        Assert.Null(await cache.GetAsync<string>("featured"));
    }

    [Fact]
    public void PaginatedResponse_CreateSlicesItemsAndSetsHasMore()
    {
        var response = PaginatedResponse<int>.Create([1, 2, 3, 4, 5], page: 2, limit: 2);

        Assert.Equal([3, 4], response.Data);
        Assert.Equal(2, response.Page);
        Assert.Equal(2, response.Limit);
        Assert.Equal(5, response.Total);
        Assert.True(response.HasMore);
    }

    private static IFormFile BuildFormFile(string fileName, byte[] bytes)
    {
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
    }
}
