using Microsoft.Extensions.Caching.Memory;

namespace OnlineStore.Api.Services;

/// <summary>
/// A generic cache interface that abstracts the underlying storage (IMemoryCache, Redis, etc.).
/// This allows the business logic to remain "clean" while making it easy to swap cache providers.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);
    Task RemoveAsync(string key);
}

/// <summary>
/// Local implementation using IMemoryCache. Suitable for development and single-node production.
/// </summary>
public class LocalCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<LocalCacheService> _logger;

    public LocalCacheService(IMemoryCache memoryCache, ILogger<LocalCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            // Debug logging only - helpful for verifying cache behavior in dev
            _logger.LogDebug("Cache HIT for key: {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogDebug("Cache MISS for key: {Key}", key);
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        
        if (absoluteExpiration.HasValue)
            options.SetAbsoluteExpiration(absoluteExpiration.Value);
            
        if (slidingExpiration.HasValue)
            options.SetSlidingExpiration(slidingExpiration.Value);

        _memoryCache.Set(key, value, options);
        _logger.LogDebug("Cache SET for key: {Key}", key);
        
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        _logger.LogDebug("Cache REMOVE for key: {Key}", key);
        
        return Task.CompletedTask;
    }
}
