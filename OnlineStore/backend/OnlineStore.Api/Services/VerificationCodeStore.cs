using System.Collections.Concurrent;

namespace OnlineStore.Api.Services;

/// <summary>
/// Interface for verification code storage. 
/// Structured for easy migration to a database or Redis in production.
/// </summary>
public interface IVerificationCodeStore
{
    string StoreCode(string key, string data);
    string? RedeemCode(string key, string code);
    bool HasValidCode(string key);
}

/// <summary>
/// In-memory implementation with 10-minute TTL, 3-attempt rate limit, and one-time use logic.
/// </summary>
public class InMemoryVerificationCodeStore : IVerificationCodeStore
{
    private readonly ConcurrentDictionary<string, CodeEntry> _store = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<InMemoryVerificationCodeStore> _logger;
    private static readonly Random _rng = new();

    public InMemoryVerificationCodeStore(ILogger<InMemoryVerificationCodeStore> logger)
    {
        _logger = logger;
    }

    public string StoreCode(string key, string data)
    {
        var code = _rng.Next(100000, 999999).ToString();

        _store[key] = new CodeEntry
        {
            Code = code,
            Data = data,
            CreatedUtc = DateTime.UtcNow,
            Attempts = 0
        };

        _logger.LogDebug("Verification code stored for key: {Key}. Valid for 10 minutes.", key);
        return code;
    }

    public string? RedeemCode(string key, string code)
    {
        if (!_store.TryGetValue(key, out var entry))
        {
            _logger.LogWarning("Redeem attempt for missing or expired key: {Key}", key);
            return null;
        }

        // Expire after 10 minutes (TTL)
        if ((DateTime.UtcNow - entry.CreatedUtc).TotalMinutes > 10)
        {
            _logger.LogInformation("Verification code expired for key: {Key}", key);
            _store.TryRemove(key, out _);
            return null;
        }

        // Rate limiting: max 3 attempts
        if (entry.Code != code)
        {
            entry.Attempts++;
            _logger.LogWarning("Invalid code attempt ({Attempts}/3) for key: {Key}", entry.Attempts, key);
            
            if (entry.Attempts >= 3)
            {
                _logger.LogError("Max attempts reached for key: {Key}. Invalidating code.", key);
                _store.TryRemove(key, out _);
            }
            return null;
        }

        // One-time use: Invalidate immediately after successful use
        _logger.LogInformation("Verification code successfully redeemed for key: {Key}", key);
        _store.TryRemove(key, out _);
        
        return entry.Data;
    }

    public bool HasValidCode(string key)
    {
        if (!_store.TryGetValue(key, out var entry))
            return false;

        // Cleanup if expired
        if ((DateTime.UtcNow - entry.CreatedUtc).TotalMinutes > 10)
        {
            _store.TryRemove(key, out _);
            return false;
        }

        return true;
    }

    private class CodeEntry
    {
        public required string Code { get; init; }
        public required string Data { get; init; }
        public required DateTime CreatedUtc { get; init; }
        public int Attempts { get; set; }
    }
}
