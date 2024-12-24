using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CacheRedis.Api.CacheService;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _defaultCacheOptions;

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
        _defaultCacheOptions = new DistributedCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(60))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(180));
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getData, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null)
    {
        var cachedData = await _cache.GetStringAsync(key);

        if (!string.IsNullOrEmpty(cachedData))
        {
            Console.WriteLine($"Cache hit for key: {key}");
            return JsonSerializer.Deserialize<T>(cachedData);
        }

        var result = await getData();
        if (result != null)
        {
            var options = GetCacheOptions(slidingExpiration, absoluteExpiration);
            await SetAsync(key, result, options.SlidingExpiration, options.AbsoluteExpirationRelativeToNow);
        }
        return result;

    }

    public async Task<T> GetAsync<T>(string key)
    {
        var cachedData = await _cache.GetStringAsync(key); // keyi verileni çek
        return string.IsNullOrEmpty(cachedData) ? default : JsonSerializer.Deserialize<T>(cachedData);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null)
    {
        var options = GetCacheOptions(slidingExpiration, absoluteExpiration); // cache ayarları
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options); // cache'e kaydet
    }

    public async Task RemoveAsync(string key) => await _cache.RemoveAsync(key);

    private DistributedCacheEntryOptions GetCacheOptions(TimeSpan? slidingExpiration, TimeSpan? absoluteExpiration)
    {
        if (slidingExpiration == null && absoluteExpiration == null) return _defaultCacheOptions;

        var options = new DistributedCacheEntryOptions();
        if (slidingExpiration.HasValue) options.SetSlidingExpiration(slidingExpiration.Value);
        if (absoluteExpiration.HasValue) options.SetAbsoluteExpiration(absoluteExpiration.Value);
        return options;
    }

    public string GenerateCacheKey(params string[] parts)
    {
        return string.Join(":", parts);
    }
}
