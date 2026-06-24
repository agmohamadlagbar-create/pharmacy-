using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public interface IJwtTokenService
{
    Task<(string accessToken, string refreshToken, int expiresInSeconds)> CreateTokenPairAsync(AppUser user);
    Task<(string accessToken, string refreshToken, int expiresInSeconds)?> RefreshAsync(string accessToken, string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

/// <summary>
/// Handles creation, validation, and refreshing of JWT Access and Refresh Tokens.
/// 
/// SECURITY NOTE - Token Storage Best Practices:
/// - Access Tokens (Short-lived 15-30m): Store in memory or a secure local variable. 
///   Do NOT store in localStorage if possible, to prevent XSS attacks.
/// - Refresh Tokens (Long-lived): For Web, store in an HttpOnly, Secure, SameSite=Strict cookie.
///   For Mobile (Flutter/React Native), store in secure encrypted storage (e.g., flutter_secure_storage).
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    private readonly UserManager<AppUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration config, UserManager<AppUser> userManager, ApplicationDbContext db, ILogger<JwtTokenService> logger)
    {
        _config = config;
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    public async Task<(string accessToken, string refreshToken, int expiresInSeconds)> CreateTokenPairAsync(AppUser user)
    {
        _logger.LogInformation("Creating new token pair for user: {UserId}", user.Id);
        var (accessToken, expiresIn) = await CreateAccessTokenAsync(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);
        await _db.SaveChangesAsync(); // Single batched save for the refresh token insert
        return (accessToken, refreshToken, expiresIn);
    }

    public async Task<(string accessToken, string refreshToken, int expiresInSeconds)?> RefreshAsync(string accessToken, string refreshToken)
    {
        var principal = GetPrincipalFromExpiredToken(accessToken);
        if (principal is null)
        {
            _logger.LogWarning("Refresh failed: Could not get principal from access token.");
            return null;
        }

        var userId = principal.FindFirstValue("sub")
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return null;

        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.TokenHash == tokenHash);

        if (stored is null || !stored.IsActive)
        {
            _logger.LogCritical("FRAUD ALERT: Attempted refresh with invalid/revoked token for user {UserId}. Possible token theft.", userId);
            return null;
        }

        // SECURE ROTATION INVALDATION:
        _logger.LogInformation("Rotating refresh token for user {UserId}.", userId);
        stored.RevokedUtc = DateTime.UtcNow;

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var (newAccess, expiresIn) = await CreateAccessTokenAsync(user);
        var newRefresh = await CreateRefreshTokenAsync(user.Id);
        await _db.SaveChangesAsync();

        return (newAccess, newRefresh, expiresIn);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        if (stored is not null && stored.IsActive)
        {
            stored.RevokedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var jwt = _config.GetSection("Jwt");
        var key = jwt["Key"]!;

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false, // Allow expired tokens
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }

    // ── Private helpers ──

    private async Task<(string token, int expiresInSeconds)> CreateAccessTokenAsync(AppUser user)
    {
        var jwt = _config.GetSection("Jwt");
        var issuer = jwt["Issuer"]!;
        var audience = jwt["Audience"]!;
        var key = jwt["Key"]!;
        var expiresMinutes = int.TryParse(jwt["ExpiresMinutes"], out var m) ? m : 480; // Default to 8h

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new("sub", user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new("accountType", user.AccountType.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.SelectMany(r => new[]
        {
            new Claim("role", r),
            new Claim(ClaimTypes.Role, r)
        }));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return (encoded, expiresMinutes * 60);
    }

    private async Task<string> CreateRefreshTokenAsync(string userId)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var tokenValue = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            ExpiresUtc = DateTime.UtcNow.AddDays(7),
            CreatedUtc = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(refreshToken);
        // SaveChangesAsync is called by the caller (CreateTokenPairAsync / RefreshAsync)
        // to batch this insert with any other pending changes in a single round-trip.

        return tokenValue;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
