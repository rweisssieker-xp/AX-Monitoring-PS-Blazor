using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace AXMonitoringBU.Api.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    void InvalidateCache();
    CacheStats GetCacheStats();
}

public class CacheStats
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public double HitRate => (HitCount + MissCount) > 0 ? (double)HitCount / (HitCount + MissCount) * 100 : 0;
    public int CurrentItemCount { get; set; }
    public long TotalRequests => HitCount + MissCount;
}

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheService> _logger;
    private readonly IConfiguration _configuration;
    private long _hitCount = 0;
    private long _missCount = 0;
    private readonly object _statsLock = new object();

    public CacheService(
        IMemoryCache memoryCache,
        ILogger<CacheService> logger,
        IConfiguration configuration)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _configuration = configuration;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out var cachedValue))
            {
                Interlocked.Increment(ref _hitCount);
                
                if (cachedValue is T typedValue)
                {
                    return Task.FromResult<T?>(typedValue);
                }
                
                // Try to deserialize if it's a string
                if (cachedValue is string jsonString)
                {
                    try
                    {
                        var deserialized = JsonSerializer.Deserialize<T>(jsonString);
                        return Task.FromResult(deserialized);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deserializing cached value for key {Key}", key);
                    }
                }
            }

            Interlocked.Increment(ref _missCount);
            return Task.FromResult<T?>(default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key {Key}", key);
            Interlocked.Increment(ref _missCount);
            return Task.FromResult<T?>(default);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var defaultTTL = TimeSpan.FromMinutes(int.Parse(_configuration["Cache:DefaultTTLMinutes"] ?? "5"));
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? defaultTTL,
                SlidingExpiration = expiration ?? defaultTTL,
                Priority = CacheItemPriority.Normal
            };

            _memoryCache.Set(key, value, cacheOptions);
            
            _logger.LogDebug("Cached value for key {Key} with TTL {TTL}", key, expiration ?? defaultTTL);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key {Key}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveAsync(string key)
    {
        try
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("Removed cache entry for key {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entry for key {Key}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        // Note: IMemoryCache doesn't support pattern-based removal natively
        // This would require a custom implementation or using a distributed cache like Redis
        // For now, we'll log a warning
        _logger.LogWarning("Pattern-based cache removal not fully supported with IMemoryCache. Pattern: {Pattern}", pattern);
        return Task.CompletedTask;
    }

    public void InvalidateCache()
    {
        // Note: IMemoryCache doesn't support clearing all entries natively
        // This would require tracking all keys or using a distributed cache
        _logger.LogWarning("Full cache invalidation not fully supported with IMemoryCache. Consider using distributed cache.");
        
        // Reset stats
        lock (_statsLock)
        {
            _hitCount = 0;
            _missCount = 0;
        }
    }

    public void InvalidateByPattern(string pattern)
    {
        // For IMemoryCache, we can't easily iterate all keys
        // This would require maintaining a key registry
        _logger.LogInformation("Cache invalidation requested for pattern: {Pattern}", pattern);
    }

    public CacheStats GetCacheStats()
    {
        lock (_statsLock)
        {
            // Get approximate item count (IMemoryCache doesn't expose this directly)
            // This is an approximation
            var itemCount = 0; // Would need custom tracking for accurate count
            
            return new CacheStats
            {
                HitCount = _hitCount,
                MissCount = _missCount,
                CurrentItemCount = itemCount
            };
        }
    }
}

