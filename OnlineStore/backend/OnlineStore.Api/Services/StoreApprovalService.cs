using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class StoreApprovalService : IStoreApprovalService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<StoreApprovalService> _logger;

    public StoreApprovalService(
        ApplicationDbContext db,
        UserManager<AppUser> userManager,
        ILogger<StoreApprovalService> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<object> GetPendingStoresAsync()
    {
        var storeUsers = await _userManager.GetUsersInRoleAsync("Shop");
        var storeUserIds = storeUsers.Select(u => u.Id).ToHashSet();

        var profiles = await _db.ShopProfiles
            .Where(sp => storeUserIds.Contains(sp.UserId))
            .OrderByDescending(sp => sp.CreatedAtUtc)
            .ToListAsync();

        return profiles.Select(p => new
        {
            p.UserId, p.ShopName, p.Location, p.OwnerName, p.Phone, p.CreatedAtUtc
        });
    }

    public async Task<object> GetStoreRiskScoreAsync(string storeUserId)
    {
        var profile = await _db.ShopProfiles.FirstOrDefaultAsync(sp => sp.UserId == storeUserId);
        var user = await _userManager.FindByIdAsync(storeUserId);

        if (profile == null || user == null)
            return new { Error = "Store not found" };

        var riskFactors = new List<string>();
        int score = 0;

        // Profile completeness
        if (string.IsNullOrWhiteSpace(profile.ShopName)) { score += 20; riskFactors.Add("Missing shop name"); }
        if (string.IsNullOrWhiteSpace(profile.Location)) { score += 15; riskFactors.Add("Missing location"); }
        if (string.IsNullOrWhiteSpace(profile.OwnerName)) { score += 10; riskFactors.Add("Missing owner name"); }
        if (string.IsNullOrWhiteSpace(profile.Phone)) { score += 10; riskFactors.Add("Missing phone number"); }
        if (string.IsNullOrWhiteSpace(profile.Description)) { score += 10; riskFactors.Add("Missing store description"); }

        // Owner verification
        if (!user.EmailConfirmed) { score += 15; riskFactors.Add("Email not confirmed"); }
        if (string.IsNullOrWhiteSpace(user.PhoneNumber)) { score += 5; riskFactors.Add("Phone number not provided"); }

        // Product quality
        var productCount = await _db.Products.CountAsync(p => p.StoreUserId == storeUserId);
        if (productCount == 0) { score += 10; riskFactors.Add("No products listed"); }
        else
        {
            var noDesc = await _db.Products.CountAsync(p => p.StoreUserId == storeUserId && (string.IsNullOrWhiteSpace(p.Description) || p.Description.Length < 20));
            if (noDesc > 0) { score += 5; riskFactors.Add($"{noDesc} product(s) have missing or short descriptions"); }
        }

        // Rapid uploads
        var firstProduct = await _db.Products
            .Where(p => p.StoreUserId == storeUserId)
            .OrderBy(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync();
        if (firstProduct != null && productCount >= 100)
        {
            var lastProduct = await _db.Products
                .Where(p => p.StoreUserId == storeUserId)
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstOrDefaultAsync();
            if (lastProduct != null && (lastProduct.CreatedAtUtc - firstProduct.CreatedAtUtc).TotalHours < 1)
            {
                score += 20;
                riskFactors.Add($"100+ products uploaded in under 1 hour (suspicious bulk import)");
            }
        }

        var rating = score switch
        {
            <= 10 => "low",
            <= 30 => "medium",
            <= 50 => "high",
            _ => "critical"
        };

        return new
        {
            UserId = storeUserId,
            ShopName = profile.ShopName,
            Score = score,
            Rating = rating,
            RiskFactors = riskFactors
        };
    }

    public async Task<object> GetStoreComplianceCheckAsync(string storeUserId)
    {
        var profile = await _db.ShopProfiles.FirstOrDefaultAsync(sp => sp.UserId == storeUserId);
        var user = await _userManager.FindByIdAsync(storeUserId);

        if (profile == null || user == null)
            return new { Error = "Store not found" };

        var emailConfirmed = user.EmailConfirmed;
        var phoneProvided = !string.IsNullOrWhiteSpace(profile.Phone);
        var hasDescription = !string.IsNullOrWhiteSpace(profile.Description);
        var hasLocation = !string.IsNullOrWhiteSpace(profile.Location);

        var productCount = await _db.Products.CountAsync(p => p.StoreUserId == storeUserId);
        var hasProducts = productCount > 0;

        var firstProduct = await _db.Products
            .Where(p => p.StoreUserId == storeUserId)
            .OrderBy(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync();
        bool rapidUploadDetected = false;
        if (firstProduct != null && productCount >= 50)
        {
            var lastProduct = await _db.Products
                .Where(p => p.StoreUserId == storeUserId)
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstOrDefaultAsync();
            if (lastProduct != null && (lastProduct.CreatedAtUtc - firstProduct.CreatedAtUtc).TotalMinutes < 30)
                rapidUploadDetected = true;
        }

        var warnings = new List<string>();
        if (!emailConfirmed) warnings.Add("Owner email not confirmed");
        if (!phoneProvided) warnings.Add("No phone number provided");
        if (!hasDescription) warnings.Add("Store has no description");
        if (!hasLocation) warnings.Add("Store location not set");
        if (!hasProducts) warnings.Add("Store has no products");
        if (rapidUploadDetected) warnings.Add("Suspicious rapid product upload pattern detected");

        return new
        {
            EmailConfirmed = emailConfirmed,
            PhoneProvided = phoneProvided,
            HasDescription = hasDescription,
            HasLocation = hasLocation,
            HasProducts = hasProducts,
            RapidUploadDetected = rapidUploadDetected,
            Warnings = warnings
        };
    }
}
