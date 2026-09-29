namespace InventoryApp.Application.Interfaces;

public static class PublicStoreCacheSegments
{
    public const string Identity = "identity";
    public const string Theme = "theme";
    public const string Categories = "categories";
    public const string Featured = "featured";
    public const string Products = "products";
}

public static class PublicStoreCacheDurations
{
    public static readonly TimeSpan Identity = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Theme = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Categories = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Featured = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan Products = TimeSpan.FromSeconds(15);
}

public interface IPublicStoreCache
{
    Task<T> GetOrCreateAsync<T>(
        string tenantKey,
        string segment,
        string parameters,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default);

    void InvalidateAll(params string[] segments);

    void InvalidateTenant(string tenantKey, params string[] segments);
}
