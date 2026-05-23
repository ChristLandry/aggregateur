using AggregatorPlatform.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace AggregatorPlatform.Infrastructure.Services;

/// <summary>
/// In-memory cache implementation backed by <see cref="IMemoryCache"/>.
/// Replaces the previous Redis-based implementation.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly object _incrementLock = new();

    public MemoryCacheService(IMemoryCache cache) => _cache = cache;

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out object? raw) && raw is T typed)
            return Task.FromResult<T?>(typed);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();
        if (ttl.HasValue) options.AbsoluteExpirationRelativeToNow = ttl.Value;
        _cache.Set(key, value!, options);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(_cache.TryGetValue(key, out _));

    public Task<long> IncrementAsync(string key, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        lock (_incrementLock)
        {
            var current = _cache.TryGetValue<long>(key, out var v) ? v : 0L;
            var next = current + 1;
            var options = new MemoryCacheEntryOptions();
            if (ttl.HasValue) options.AbsoluteExpirationRelativeToNow = ttl.Value;
            _cache.Set(key, next, options);
            return Task.FromResult(next);
        }
    }
}
