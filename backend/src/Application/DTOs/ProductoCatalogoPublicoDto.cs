namespace InventoryApp.Application.DTOs;

/// <summary>
/// Proyeccion segura y canonica para el escaparate publico. Excluye costos,
/// auditoria y datos reservados a administracion. Los campos comerciales aun
/// no soportados por la fuente de verdad se exponen como nulos/no activos para
/// mantener un contrato evolutivo sin inventar informacion.
/// </summary>
public sealed class ProductoCatalogoPublicoDto
{
    public int Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public int? CategoriaId { get; init; }
    public string? CategoriaNombre { get; init; }
    public string? MarcaNombre { get; init; }
    public string? ModeloNombre { get; init; }
    public decimal Precio { get; init; }
    public decimal? PrecioOferta { get; init; }
    public bool OfertaActiva { get; init; }
    public string? OfertaNombre { get; init; }
    public DateTime? OfertaInicioUtc { get; init; }
    public DateTime? OfertaFinUtc { get; init; }
    public decimal Ahorro { get; init; }
    public decimal PorcentajeAhorro { get; init; }
    public int CantidadDisponible { get; init; }
    public bool EstaAgotado { get; init; }
    public string EstadoDisponibilidad { get; init; } = "Agotado";
    public string? Sku { get; init; }
    public bool Activo { get; init; }
    public bool EsDestacado { get; init; }
    public DateTime FechaCreacion { get; init; }
    public string? ImagenPrincipalUrl { get; init; }
    public List<ProductoImagenPublicaDto> Imagenes { get; init; } = new();
    public List<ModeloCatalogoPublicoDto> Modelos { get; init; } = new();
}

public sealed class ProductoImagenPublicaDto
{
    public string Url { get; init; } = string.Empty;
    public int Orden { get; init; }
    public bool EsPrincipal { get; init; }
}

public sealed class ModeloCatalogoPublicoDto
{
    public int ProductoVarianteId { get; init; }
    public int? ModeloId { get; init; }
    public string? ModeloNombre { get; init; }
    public string? MarcaNombre { get; init; }
    public string? Sku { get; init; }
    public decimal Precio { get; init; }
    public decimal? PrecioOferta { get; init; }
    public bool OfertaActiva { get; init; }
    public string? OfertaNombre { get; init; }
    public DateTime? OfertaInicioUtc { get; init; }
    public DateTime? OfertaFinUtc { get; init; }
    public decimal Ahorro { get; init; }
    public decimal PorcentajeAhorro { get; init; }
    public int CantidadDisponible { get; init; }
    public bool EstaAgotado { get; init; }
    public string EstadoDisponibilidad { get; init; } = "Agotado";
    public List<ProductoImagenPublicaDto> Imagenes { get; init; } = new();
}

public sealed class ProductosContextoPublicoRequestDto
{
    public List<int> ProductoIds { get; init; } = new();
}
