using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Api.Services;

public class FcmService : IFcmService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<FcmService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IWebHostEnvironment _env;

    private static string? _cachedAccessToken;
    private static DateTime _tokenExpiresAtUtc = DateTime.MinValue;
    private static readonly SemaphoreSlim _tokenLock = new(1, 1);

    public FcmService(
        ApplicationDbContext db,
        IConfiguration config,
        ILogger<FcmService> logger,
        HttpClient httpClient,
        IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _logger = logger;
        _httpClient = httpClient;
        _env = env;
    }

    // Fake token patterns that should never be stored — they can never receive real push notifications
    private static bool IsFakeToken(string token) =>
        token.StartsWith("web_fcm_token_", StringComparison.OrdinalIgnoreCase) ||
        token.StartsWith("mobile_fcm_token_", StringComparison.OrdinalIgnoreCase);

    public async Task RegisterDeviceTokenAsync(string userId, string token, string platform)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return;

        if (IsFakeToken(token))
        {
            _logger.LogWarning("Rejected fake/fallback FCM token for user {UserId}: {Token}", userId, token);
            return;
        }

        try
        {
            var existing = await _db.UserDeviceTokens
                .FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceToken == token);

            if (existing != null)
            {
                existing.LastUsedAtUtc = DateTime.UtcNow;
                existing.Platform = string.IsNullOrWhiteSpace(platform) ? existing.Platform : platform;
            }
            else
            {
                _db.UserDeviceTokens.Add(new UserDeviceToken
                {
                    UserId = userId,
                    DeviceToken = token,
                    Platform = string.IsNullOrWhiteSpace(platform) ? "Web" : platform,
                    CreatedAtUtc = DateTime.UtcNow,
                    LastUsedAtUtc = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("FCM device token registered for user {UserId} (Platform: {Platform})", userId, platform);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register FCM device token for user {UserId}", userId);
        }
    }

    public async Task UnregisterDeviceTokenAsync(string userId, string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return;

        try
        {
            var tokens = await _db.UserDeviceTokens
                .Where(x => x.UserId == userId && x.DeviceToken == token)
                .ToListAsync();

            if (tokens.Count > 0)
            {
                _db.UserDeviceTokens.RemoveRange(tokens);
                await _db.SaveChangesAsync();
                _logger.LogInformation("FCM device token unregistered for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to unregister FCM device token for user {UserId}", userId);
        }
    }

    public async Task SendPushNotificationToUserAsync(string userId, string title, string message, IDictionary<string, string>? data = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        List<string> userTokens;
        try
        {
            userTokens = await _db.UserDeviceTokens
                .Where(x => x.UserId == userId)
                .Select(x => x.DeviceToken)
                .Distinct()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query UserDeviceTokens for user {UserId}", userId);
            userTokens = new List<string>();
        }

        if (userTokens.Count == 0)
        {
            _logger.LogWarning("No registered FCM tokens found for user {UserId}. Push notification will NOT be delivered.", userId);
            return;
        }

        var projectId = _config["Firebase:ProjectId"] ?? "online-store-cbba7";
        var accessToken = await GetGoogleOAuth2AccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogInformation("[FCM v1 Mock Mode] User: {UserId} | Title: {Title} | Body: {Message} | Active Tokens: {Count}",
                userId, title, message, userTokens.Count);
            return;
        }

        var fcmV1Url = $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";
        var expiredTokens = new List<string>();

        foreach (var token in userTokens)
        {
            try
            {
                var payload = new
                {
                    message = new
                    {
                        token = token,
                        // Data-only message: no "notification" or "webpush.notification" fields.
                        // This prevents the browser from auto-displaying notifications,
                        // giving full control to our service worker (single display point).
                        data = new Dictionary<string, string>(data ?? new Dictionary<string, string>())
                        {
                            ["title"] = title,
                            ["body"] = message,
                            ["icon"] = "/logo192.png",
                            ["badge"] = "/favicon.ico"
                        },
                        webpush = new
                        {
                            headers = new { Urgency = "high" }
                        }
                    }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, fcmV1Url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode || responseBody.Contains("UNREGISTERED") || responseBody.Contains("INVALID_ARGUMENT"))
                {
                    _logger.LogWarning("FCM token failed delivery for user {UserId}: {StatusCode} - {Body}", userId, response.StatusCode, responseBody);
                    if (responseBody.Contains("UNREGISTERED") || responseBody.Contains("Requested entity was not found"))
                    {
                        expiredTokens.Add(token);
                    }
                }
                else
                {
                    _logger.LogInformation("FCM HTTP v1 push notification sent successfully to user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching FCM v1 push notification to user {UserId}", userId);
            }
        }

        if (expiredTokens.Count > 0)
        {
            try
            {
                var staleEntries = await _db.UserDeviceTokens
                    .Where(x => x.UserId == userId && expiredTokens.Contains(x.DeviceToken))
                    .ToListAsync();

                if (staleEntries.Count > 0)
                {
                    _db.UserDeviceTokens.RemoveRange(staleEntries);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove expired FCM tokens for user {UserId}", userId);
            }
        }
    }

    private async Task<string?> GetGoogleOAuth2AccessTokenAsync()
    {
        if (_cachedAccessToken != null && DateTime.UtcNow < _tokenExpiresAtUtc)
        {
            return _cachedAccessToken;
        }

        await _tokenLock.WaitAsync();
        try
        {
            if (_cachedAccessToken != null && DateTime.UtcNow < _tokenExpiresAtUtc)
            {
                return _cachedAccessToken;
            }

            var serviceAccountRelPath = _config["Firebase:ServiceAccountPath"] ?? "firebase-service-account.json";
            var fullPath = Path.IsPathRooted(serviceAccountRelPath)
                ? serviceAccountRelPath
                : Path.Combine(_env.ContentRootPath, serviceAccountRelPath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Firebase Service Account file not found at '{Path}'. Falling back to mock push mode.", fullPath);
                return null;
            }

            var jsonContent = await File.ReadAllTextAsync(fullPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var clientEmail = root.GetProperty("client_email").GetString();
            var privateKeyPem = root.GetProperty("private_key").GetString();
            var tokenUri = root.GetProperty("token_uri").GetString() ?? "https://oauth2.googleapis.com/token";

            if (string.IsNullOrWhiteSpace(clientEmail) || string.IsNullOrWhiteSpace(privateKeyPem))
            {
                _logger.LogWarning("Invalid Service Account JSON format. Missing client_email or private_key.");
                return null;
            }

            // Clean PEM key string
            var cleanKey = privateKeyPem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();

            var privateKeyBytes = Convert.FromBase64String(cleanKey);
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

            var rsaSecurityKey = new RsaSecurityKey(rsa);
            var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

            var now = DateTime.UtcNow;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = clientEmail,
                Audience = tokenUri,
                Claims = new Dictionary<string, object>
                {
                    { "scope", "https://www.googleapis.com/auth/firebase.messaging" }
                },
                IssuedAt = now,
                Expires = now.AddHours(1),
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.CreateEncodedJwt(tokenDescriptor);

            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new KeyValuePair<string, string>("assertion", jwtToken)
            });

            var response = await _httpClient.PostAsync(tokenUri, requestContent);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to obtain OAuth 2.0 access token from Google: {StatusCode} - {Body}", response.StatusCode, responseJson);
                return null;
            }

            using var tokenDoc = JsonDocument.Parse(responseJson);
            var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = tokenDoc.RootElement.TryGetProperty("expires_in", out var expProp) ? expProp.GetInt32() : 3600;

            _cachedAccessToken = accessToken;
            _tokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn - 300); // Refresh 5 minutes early

            _logger.LogInformation("Successfully obtained Google OAuth 2.0 access token for FCM HTTP v1 API.");
            return accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception generating Google OAuth 2.0 token for FCM v1");
            return null;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}
