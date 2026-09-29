using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using InventoryApp.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace InventoryApp.Infrastructure.Services;

public sealed class PublicStoreMemoryCache : IPublicStoreCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, long> _globalGenerations = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _tenantGenerations = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public PublicStoreMemoryCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string tenantKey,
        string segment,
        string parameters,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantKey))
            throw new ArgumentException("La cache pública requiere un tenantKey.", nameof(tenantKey));
        if (string.IsNullOrWhiteSpace(segment))
            throw new ArgumentException("La cache pública requiere un segmento.", nameof(segment));
        if (ttl <= TimeSpan.Zero)
            return await factory(cancellationToken);

        var cacheKey = BuildKey(tenantKey, segment, parameters);
        if (_memoryCache.TryGetValue<T>(cacheKey, out var cached) && cached is not null)
            return cached;

        var gate = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_memoryCache.TryGetValue<T>(cacheKey, out cached) && cached is not null)
                return cached;

            var created = await factory(cancellationToken);
            _memoryCache.Set(cacheKey, created, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });
            return created;
        }
        finally
        {
            gate.Release();
            _locks.TryRemove(new KeyValuePair<string, SemaphoreSlim>(cacheKey, gate));
        }
    }

    public void InvalidateAll(params string[] segments)
    {
        foreach (var segment in NormalizeSegments(segments))
            _globalGenerations.AddOrUpdate(segment, 1, static (_, current) => current + 1);
    }

    public void InvalidateTenant(string tenantKey, params string[] segments)
    {
        if (string.IsNullOrWhiteSpace(tenantKey))
            return;

        var tenant = Normalize(tenantKey);
        foreach (var segment in NormalizeSegments(segments))
        {
            var key = $"{tenant}:{segment}";
            _tenantGenerations.AddOrUpdate(key, 1, static (_, current) => current + 1);
        }
    }

    private string BuildKey(string tenantKey, string segment, string parameters)
    {
        var tenant = Normalize(tenantKey);
        var normalizedSegment = Normalize(segment);
        var globalGeneration = _globalGenerations.GetOrAdd(normalizedSegment, 0);
        var tenantGeneration = _tenantGenerations.GetOrAdd($"{tenant}:{normalizedSegment}", 0);
        var parameterHash = Hash(parameters ?? string.Empty);

        return $"solqaryn:store:{tenant}:{normalizedSegment}:g{globalGeneration}:t{tenantGeneration}:p{parameterHash}";
    }

    private static IEnumerable<string> NormalizeSegments(IEnumerable<string> segments) =>
        segments
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Select(Normalize)
            .Distinct(StringComparer.Ordinal);

    private static string Normalize(string value) =>
        value.Trim().ToLowerInvariant().Replace(' ', '-');

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes.AsSpan(0, 12)).ToLowerInvariant();
    }
}
