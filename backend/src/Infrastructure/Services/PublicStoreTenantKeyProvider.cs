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

    public static string ResolveTenantKey(
        int? configuracionId,
        string? nombreComercial,
        string? nombreVisibleSistema,
        IReadOnlyCollection<Solqaryn.Domain.Entities.Empresa> empresas)
    {
        // Sin vinculación dominio->empresa certificada, elegir por nombre
        // comercial o por configuración compartida es inseguro con 2+ tenants.
        // La única identidad anónima verificable hoy es UNA empresa activa,
        // protegida además por la cuarentena HTTP (incluye inactivas).
        if (empresas.Count != 1 || empresas.Single().Id <= 0)
            throw new InvalidOperationException(
                "La tienda pública requiere una vinculación empresarial única y verificable.");

        return $"empresa:{empresas.Single().Id}";
    }
}
