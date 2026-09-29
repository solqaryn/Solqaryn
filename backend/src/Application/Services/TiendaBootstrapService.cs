using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;

namespace InventoryApp.Application.Services;

public sealed class TiendaBootstrapService : ITiendaBootstrapService
{
    private readonly IEmpresaConfiguracionService _empresaConfiguracion;
    private readonly ITemaVisualService _temaVisual;
    private readonly ICategoriaService _categorias;
    private readonly ICatalogoPublicoService _catalogo;
    private readonly IWhatsAppPublicoService _whatsApp;

    public TiendaBootstrapService(
        IEmpresaConfiguracionService empresaConfiguracion,
        ITemaVisualService temaVisual,
        ICategoriaService categorias,
        ICatalogoPublicoService catalogo,
        IWhatsAppPublicoService whatsApp)
    {
        _empresaConfiguracion = empresaConfiguracion;
        _temaVisual = temaVisual;
        _categorias = categorias;
        _catalogo = catalogo;
        _whatsApp = whatsApp;
    }

    public async Task<TiendaBootstrapDto> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Los servicios comparten el scope HTTP/tenant. Se ejecutan secuencialmente porque
        // los repositorios EF del request comparten DbContext y no son thread-safe.
        var configuracion = await _empresaConfiguracion.GetActivaAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var whatsApp = string.IsNullOrWhiteSpace(configuracion.WhatsApp)
            ? await _whatsApp.ObtenerAsync(cancellationToken)
            : new WhatsAppPublicoDto(configuracion.WhatsApp.Trim(), true);

        var tema = await _temaVisual.GetAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var categorias = (await _categorias.GetActivasAsync())
            .Where(categoria => categoria.Activa)
            .OrderBy(categoria => categoria.Nombre)
            .Select(categoria => new CategoriaCatalogoPublicoDto
            {
                Id = categoria.Id,
                Slug = PublicSlug.Create(categoria.Nombre, categoria.Id),
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion,
                TotalProductos = null
            })
            .ToList();

        var destacados = await _catalogo.ObtenerDestacadosAsync(4, cancellationToken);

        return new TiendaBootstrapDto
        {
            Identidad = new TiendaIdentidadPublicaDto
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
            },
            Tema = tema,
            Categorias = categorias,
            Destacados = destacados
        };
    }
}
