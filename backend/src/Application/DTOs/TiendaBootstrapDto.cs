namespace InventoryApp.Application.DTOs;

public sealed class TiendaBootstrapDto
{
    public TiendaIdentidadPublicaDto Identidad { get; init; } = new();
    public TemaVisualDto Tema { get; init; } = new();
    public List<CategoriaCatalogoPublicoDto> Categorias { get; init; } = new();
    public List<TiendaProductoResumenDto> Destacados { get; init; } = new();
}

public sealed class TiendaIdentidadPublicaDto
{
    public string NombreComercial { get; init; } = string.Empty;
    public string Eslogan { get; init; } = string.Empty;
    public string? Telefono { get; init; }
    public string? Correo { get; init; }
    public string? WhatsApp { get; init; }
    public string? LogoUrl { get; init; }
    public string Moneda { get; init; } = "HNL";
    public bool EncabezadoActivo { get; init; }
    public string? EncabezadoTexto { get; init; }
    public bool PiePaginaActivo { get; init; }
    public string? PiePaginaTexto { get; init; }
    public string Copyright { get; init; } = string.Empty;
    public bool MostrarCopyright { get; init; }
    public bool UsarAnioAutomaticoCopyright { get; init; }
}

public sealed record WhatsAppPublicoDto(string? NumeroTelefonoE164, bool Disponible);
