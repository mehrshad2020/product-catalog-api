using Microsoft.Extensions.Caching.Memory;

namespace Api.Services.Cache;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T? Get<T>(string key)
    {
        return _cache.Get<T>(key);
    }

    public void Set<T>(
        string key,
        T value,
        TimeSpan expiration)
    {
        _cache.Set(key, value, expiration);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    public void IncrementVersion(string group)
    {
        var key = $"cache-version:{group}";

        var currentVersion = _cache.Get<int?>(key) ?? 1;

        _cache.Set(key, currentVersion + 1);
    }

    public int GetVersion(string group)
    {
        var key = $"cache-version:{group}";

        return _cache.Get<int?>(key) ?? 1;
    }
}