using InventoryApp.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace InventoryApp.Infrastructure.Services;

public sealed class PublicStoreTenantKeyProvider : IPublicStoreTenantKeyProvider
{
    private const string ResolverCacheKey = "solqaryn:storefront:tenant-resolution";
    private static readonly TimeSpan ResolverTtl = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _memoryCache;
    private readonly IEmpresaConfiguracionRepository _empresaConfiguracionRepository;
    private readonly IEmpresaRepository _empresaRepository;

    public PublicStoreTenantKeyProvider(
        IMemoryCache memoryCache,
        IEmpresaConfiguracionRepository empresaConfiguracionRepository,
        IEmpresaRepository empresaRepository)
    {
        _memoryCache = memoryCache;
        _empresaConfiguracionRepository = empresaConfiguracionRepository;
        _empresaRepository = empresaRepository;
    }

    public async Task<string> GetTenantKeyAsync(CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue<string>(ResolverCacheKey, out var cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        var configuracion = await _empresaConfiguracionRepository.GetActivaAsync();
        var empresas = await _empresaRepository.ListAsync(activa: true, cancellationToken);

        var tenantKey = ResolveTenantKey(configuracion?.Id, configuracion?.NombreComercial, configuracion?.NombreVisibleSistema, empresas);
        _memoryCache.Set(ResolverCacheKey, tenantKey, ResolverTtl);
        return tenantKey;
    }

    internal static string ResolveTenantKey(
        int? configuracionId,
        string? nombreComercial,
        string? nombreVisibleSistema,
        IReadOnlyCollection<InventoryApp.Domain.Entities.Empresa> empresas)
    {
        var nombres = new[] { nombreComercial, nombreVisibleSistema }
            .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
            .Select(nombre => nombre!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var coincidencias = empresas
            .Where(empresa => nombres.Any(nombre =>
                string.Equals(nombre, empresa.Nombre?.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Select(empresa => empresa.Id)
            .Distinct()
            .ToArray();

        if (coincidencias.Length == 1)
            return $"empresa:{coincidencias[0]}";

        if (empresas.Count == 1)
            return $"empresa:{empresas.Single().Id}";

        if (configuracionId is > 0)
            return $"public-config:{configuracionId.Value}";

        return "public-config:unconfigured";
    }
}
