using Solqaryn.Application.Interfaces;
using Solqaryn.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Solqaryn.Tests;

public sealed class PublicStoreMemoryCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_DeduplicaDiezConsumidoresConcurrentes()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var sut = new PublicStoreMemoryCache(memory);
        var calls = 0;

        async Task<string> Factory(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(50, cancellationToken);
            return "ok";
        }

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => sut.GetOrCreateAsync(
                "empresa:7",
                PublicStoreCacheSegments.Categories,
                "page=all",
                TimeSpan.FromMinutes(1),
                Factory))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, value => Assert.Equal("ok", value));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Cache_SeparaTenantYParametros_EInvalidaPorGeneracion()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var sut = new PublicStoreMemoryCache(memory);
        var calls = 0;

        Task<int> Factory(CancellationToken _)
            => Task.FromResult(Interlocked.Increment(ref calls));

        var t1p1 = await sut.GetOrCreateAsync("empresa:1", PublicStoreCacheSegments.Products, "page=1", TimeSpan.FromMinutes(1), Factory);
        var t1p1Repeat = await sut.GetOrCreateAsync("empresa:1", PublicStoreCacheSegments.Products, "page=1", TimeSpan.FromMinutes(1), Factory);
        var t1p2 = await sut.GetOrCreateAsync("empresa:1", PublicStoreCacheSegments.Products, "page=2", TimeSpan.FromMinutes(1), Factory);
        var t2p1 = await sut.GetOrCreateAsync("empresa:2", PublicStoreCacheSegments.Products, "page=1", TimeSpan.FromMinutes(1), Factory);

        Assert.Equal(t1p1, t1p1Repeat);
        Assert.NotEqual(t1p1, t1p2);
        Assert.NotEqual(t1p1, t2p1);
        Assert.Equal(3, calls);

        sut.InvalidateTenant("empresa:1", PublicStoreCacheSegments.Products);
        var t1AfterTenantInvalidation = await sut.GetOrCreateAsync("empresa:1", PublicStoreCacheSegments.Products, "page=1", TimeSpan.FromMinutes(1), Factory);
        var t2StillCached = await sut.GetOrCreateAsync("empresa:2", PublicStoreCacheSegments.Products, "page=1", TimeSpan.FromMinutes(1), Factory);

        Assert.NotEqual(t1p1, t1AfterTenantInvalidation);
        Assert.Equal(t2p1, t2StillCached);
        Assert.Equal(4, calls);

        sut.InvalidateAll(PublicStoreCacheSegments.Products);
        var t2AfterGlobalInvalidation = await sut.GetOrCreateAsync("empresa:2", PublicStoreCacheSegments.Products, "page=1", TimeSpan.FromMinutes(1), Factory);

        Assert.NotEqual(t2p1, t2AfterGlobalInvalidation);
        Assert.Equal(5, calls);
    }
}
