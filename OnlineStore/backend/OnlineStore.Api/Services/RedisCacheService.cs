using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace OnlineStore.Api.Services;

/// <summary>
/// High-performance Redis implementation of ICacheService with automatic fallback to IMemoryCache
/// if Redis is unreachable or throws a runtime connection exception.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IMemoryCache _localCache;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCacheService(
        IConnectionMultiplexer? redis,
        IMemoryCache localCache,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _localCache = localCache;
        _logger = logger;
    }

    private IDatabase? GetDatabase()
    {
        try
        {
            if (_redis != null && _redis.IsConnected)
            {
                return _redis.GetDatabase();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis connection issue. Falling back to local memory cache.");
        }

        return null;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var db = GetDatabase();
        if (db != null)
        {
            try
            {
                var redisValue = await db.StringGetAsync(key);
                if (redisValue.HasValue)
                {
                    _logger.LogDebug("Redis Cache HIT for key: {Key}", key);
                    return JsonSerializer.Deserialize<T>((string)redisValue!, _jsonOptions);
                }

                _logger.LogDebug("Redis Cache MISS for key: {Key}", key);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis GetAsync failed for key: {Key}. Falling back to local cache.", key);
            }
        }

        // Local cache fallback
        if (_localCache.TryGetValue(key, out T? localValue))
        {
            _logger.LogDebug("Local MemoryCache HIT for key: {Key}", key);
            return localValue;
        }

        _logger.LogDebug("Local MemoryCache MISS for key: {Key}", key);
        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
    {
        if (value == null) return;

        var expiry = absoluteExpiration ?? slidingExpiration ?? TimeSpan.FromMinutes(30);
        var db = GetDatabase();

        if (db != null)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(value, _jsonOptions);
                await db.StringSetAsync(key, serialized, expiry);
                _logger.LogDebug("Redis Cache SET for key: {Key} (Expiry: {Expiry})", key, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis SetAsync failed for key: {Key}. Setting local cache.", key);
            }
        }

        // Also update local cache as secondary hot-layer or fallback
        var options = new MemoryCacheEntryOptions();
        if (absoluteExpiration.HasValue) options.SetAbsoluteExpiration(absoluteExpiration.Value);
        if (slidingExpiration.HasValue) options.SetSlidingExpiration(slidingExpiration.Value);
        _localCache.Set(key, value, options);
    }

    public async Task RemoveAsync(string key)
    {
        var db = GetDatabase();
        if (db != null)
        {
            try
            {
                await db.KeyDeleteAsync(key);
                _logger.LogDebug("Redis Cache REMOVE for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis KeyDeleteAsync failed for key: {Key}.", key);
            }
        }

        _localCache.Remove(key);
    }
}
