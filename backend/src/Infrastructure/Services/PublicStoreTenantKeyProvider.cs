using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Microsoft.AspNetCore.Http;

namespace Solqaryn.Infrastructure.Services;

public sealed class PublicStoreTenantKeyProvider : IPublicStoreTenantKeyProvider
{
    private static readonly TimeSpan ResolverTtl = TimeSpan.FromMinutes(5);

    private readonly IPublicStoreCache _cache;
    private readonly IEmpresaConfiguracionRepository _empresaConfiguracionRepository;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public PublicStoreTenantKeyProvider(
        IPublicStoreCache cache,
        IEmpresaConfiguracionRepository empresaConfiguracionRepository,
        IEmpresaRepository empresaRepository,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _cache = cache;
        _empresaConfiguracionRepository = empresaConfiguracionRepository;
        _empresaRepository = empresaRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string> GetTenantKeyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var http = _httpContextAccessor?.HttpContext;

        if (http?.Request.Path.StartsWithSegments(
                "/tienda", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Cada request público debe poseer una vinculación tenant
            // server-side ya validada por middleware. Sin marca: denegar,
            // nunca reutilizar una caché global de la tienda anterior.
            var empresaId = VerifiedPublicStoreTenant.Resolve(http);
            if (empresaId is not > 0)
                throw new ForbiddenAccessException(
                    "La tienda pública requiere una empresa vinculada y verificada.");

            return Task.FromResult($"empresa:{empresaId.Value}");
        }

        // Compatibilidad de rutinas legacy sin solicitud pública: solamente
        // se resuelve si el repositorio certifica UNA empresa activa.
        return _cache.GetOrCreateAsync(
            "tenant-resolution",
            PublicStoreCacheSegments.Identity,
            "active-public-tenant:v1",
            ResolverTtl,
            ResolveAsync,
            cancellationToken);
    }

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
