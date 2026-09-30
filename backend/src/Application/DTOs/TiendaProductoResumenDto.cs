namespace InventoryApp.Application.DTOs;

/// <summary>
/// Proyección pública mínima para listados del storefront.
/// No expone galería completa, descripción completa ni grafos administrativos.
/// </summary>
public sealed class TiendaProductoResumenDto
{
    public int Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string? DescripcionResumen { get; init; }
    public int? CategoriaId { get; init; }
    public string? CategoriaNombre { get; init; }
    public string? MarcaNombre { get; init; }
    public string? ModeloNombre { get; init; }
    public decimal Precio { get; init; }
    public decimal? PrecioOferta { get; init; }
    public bool OfertaActiva { get; init; }
    public string? OfertaNombre { get; init; }
    public decimal Ahorro { get; init; }
    public decimal PorcentajeAhorro { get; init; }
    public int CantidadDisponible { get; init; }
    public bool EstaAgotado { get; init; }
    public string EstadoDisponibilidad { get; init; } = "Agotado";
    public bool EsDestacado { get; init; }
    public DateTime FechaCreacion { get; init; }
    public string? ImagenPrincipalUrl { get; init; }
    public List<TiendaProductoVarianteResumenDto> Modelos { get; init; } = new();
}

public sealed class TiendaProductoVarianteResumenDto
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
    public decimal Ahorro { get; init; }
    public decimal PorcentajeAhorro { get; init; }
    public int CantidadDisponible { get; init; }
    public bool EstaAgotado { get; init; }
    public string EstadoDisponibilidad { get; init; } = "Agotado";
}
