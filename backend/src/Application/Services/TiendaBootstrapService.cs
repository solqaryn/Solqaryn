using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;

namespace Solqaryn.Application.Services;

public sealed class TiendaBootstrapService : ITiendaBootstrapService
{
    private readonly IEmpresaConfiguracionService _empresaConfiguracion;
    private readonly ITemaVisualService _temaVisual;
    private readonly ICategoriaService _categorias;
    private readonly ICatalogoPublicoService _catalogo;
    private readonly IWhatsAppPublicoService _whatsApp;
    private readonly IPublicStoreCache? _cache;
    private readonly IPublicStoreTenantKeyProvider? _tenantKeyProvider;
    private readonly ICategoriaGaleriaService? _galerias;

    public TiendaBootstrapService(
        IEmpresaConfiguracionService empresaConfiguracion,
        ITemaVisualService temaVisual,
        ICategoriaService categorias,
        ICatalogoPublicoService catalogo,
        IWhatsAppPublicoService whatsApp,
        IPublicStoreCache? cache = null,
        IPublicStoreTenantKeyProvider? tenantKeyProvider = null,
        ICategoriaGaleriaService? galerias = null)
    {
        _empresaConfiguracion = empresaConfiguracion;
        _temaVisual = temaVisual;
        _categorias = categorias;
        _catalogo = catalogo;
        _whatsApp = whatsApp;
        _cache = cache;
        _tenantKeyProvider = tenantKeyProvider;
        _galerias = galerias;
    }

    public async Task<TiendaBootstrapDto> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tenantKey = await ResolverTenantKeyAsync(cancellationToken);

        var identidad = await CacheAsync(
            tenantKey,
            PublicStoreCacheSegments.Identity,
            "bootstrap-identity:v1",
            PublicStoreCacheDurations.Identity,
            ObtenerIdentidadAsync,
            cancellationToken);

        var tema = await CacheAsync(
            tenantKey,
            PublicStoreCacheSegments.Theme,
            "bootstrap-theme:v1",
            PublicStoreCacheDurations.Theme,
            _ => _temaVisual.GetAsync(),
            cancellationToken);

        var categorias = await ObtenerCategoriasCoreAsync(tenantKey, cancellationToken);
        var destacados = await _catalogo.ObtenerDestacadosAsync(4, cancellationToken);

        return new TiendaBootstrapDto
        {
            Identidad = identidad,
            Tema = tema,
            Categorias = categorias.Take(6).ToList(),
            Destacados = destacados
        };
    }

    public async Task<List<CategoriaCatalogoPublicoDto>> ObtenerCategoriasAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tenantKey = await ResolverTenantKeyAsync(cancellationToken);
        return await ObtenerCategoriasCoreAsync(tenantKey, cancellationToken);
    }

    private async Task<TiendaIdentidadPublicaDto> ObtenerIdentidadAsync(CancellationToken cancellationToken)
    {
        var configuracion = await _empresaConfiguracion.GetActivaAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var whatsApp = string.IsNullOrWhiteSpace(configuracion.WhatsApp)
            ? await _whatsApp.ObtenerAsync(cancellationToken)
            : new WhatsAppPublicoDto(configuracion.WhatsApp.Trim(), true);

        return new TiendaIdentidadPublicaDto
        {
            NombreComercial = configuracion.NombreComercial,
            Eslogan = configuracion.Eslogan,
            Telefono = configuracion.Telefono,
            Correo = configuracion.Correo,
            WhatsApp = whatsApp.Disponible ? whatsApp.NumeroTelefonoE164 : null,
            LogoUrl = configuracion.LogoUrl,
            Moneda = configuracion.Moneda,
            EncabezadoActivo = configuracion.EncabezadoActivo,
            EncabezadoTexto = configuracion.EncabezadoTexto,
            PiePaginaActivo = configuracion.PiePaginaActivo,
            PiePaginaTexto = configuracion.PiePaginaTexto,
            Copyright = configuracion.Copyright,
            MostrarCopyright = configuracion.MostrarCopyright,
            UsarAnioAutomaticoCopyright = configuracion.UsarAnioAutomaticoCopyright
        };
    }

    private async Task<List<CategoriaCatalogoPublicoDto>> ObtenerCategoriasCoreAsync(
        string? tenantKey,
        CancellationToken cancellationToken)
    {
        return await CacheAsync(
            tenantKey,
            PublicStoreCacheSegments.Categories,
            "public-categories:v2",
            PublicStoreCacheDurations.Categories,
            async ct =>
            {
                ct.ThrowIfCancellationRequested();
                var categorias = (await _categorias.GetActivasAsync())
                    .Where(categoria => categoria.Activa).OrderBy(categoria => categoria.Nombre).ToList();
                var imagenes = new Dictionary<int, string[]>();
                // La identidad pública ambigua nunca obtiene imágenes de otro tenant.
                if (_galerias is not null && tenantKey is not null
                    && tenantKey.StartsWith("empresa:", StringComparison.Ordinal)
                    && int.TryParse(tenantKey["empresa:".Length..], out var empresaId) && empresaId > 0)
                    imagenes = await _galerias.ObtenerPublicasAsync(empresaId, categorias.Select(c => c.Id).ToArray(), ct);
                return categorias.Select(categoria => new CategoriaCatalogoPublicoDto
                    {
                        Id = categoria.Id,
                        Slug = PublicSlug.Create(categoria.Nombre, categoria.Id),
                        Nombre = categoria.Nombre,
                        Descripcion = categoria.Descripcion,
                        Imagenes = imagenes.GetValueOrDefault(categoria.Id) ?? Array.Empty<string>(),
                        TotalProductos = null
                    })
                    .ToList();
            },
            cancellationToken);
    }

    private async Task<string?> ResolverTenantKeyAsync(CancellationToken cancellationToken)
    {
        if (_tenantKeyProvider is null)
            return null;

        return await _tenantKeyProvider.GetTenantKeyAsync(cancellationToken);
    }

    private async Task<T> CacheAsync<T>(
        string? tenantKey,
        string segment,
        string parameters,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken)
    {
        if (_cache is null || string.IsNullOrWhiteSpace(tenantKey))
            return await factory(cancellationToken);

        return await _cache.GetOrCreateAsync(
            tenantKey,
            segment,
            parameters,
            ttl,
            factory,
            cancellationToken);
    }
}
