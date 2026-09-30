using Solqaryn.Application.Interfaces;

namespace Solqaryn.Infrastructure.Services;

public sealed class PublicStoreTenantKeyProvider : IPublicStoreTenantKeyProvider
{
    private static readonly TimeSpan ResolverTtl = TimeSpan.FromMinutes(5);

    private readonly IPublicStoreCache _cache;
    private readonly IEmpresaConfiguracionRepository _empresaConfiguracionRepository;
    private readonly IEmpresaRepository _empresaRepository;

    public PublicStoreTenantKeyProvider(
        IPublicStoreCache cache,
        IEmpresaConfiguracionRepository empresaConfiguracionRepository,
        IEmpresaRepository empresaRepository)
    {
        _cache = cache;
        _empresaConfiguracionRepository = empresaConfiguracionRepository;
        _empresaRepository = empresaRepository;
    }

    public Task<string> GetTenantKeyAsync(CancellationToken cancellationToken = default) =>
        _cache.GetOrCreateAsync(
            "tenant-resolution",
            PublicStoreCacheSegments.Identity,
            "active-public-tenant:v1",
            ResolverTtl,
            ResolveAsync,
            cancellationToken);

    private async Task<string> ResolveAsync(CancellationToken cancellationToken)
    {
        var configuracion = await _empresaConfiguracionRepository.GetActivaAsync();
        var empresas = await _empresaRepository.ListAsync(activa: true, cancellationToken);
        return ResolveTenantKey(
            configuracion?.Id,
            configuracion?.NombreComercial,
            configuracion?.NombreVisibleSistema,
            empresas);
    }

    internal static string ResolveTenantKey(
        int? configuracionId,
        string? nombreComercial,
        string? nombreVisibleSistema,
        IReadOnlyCollection<Solqaryn.Domain.Entities.Empresa> empresas)
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
