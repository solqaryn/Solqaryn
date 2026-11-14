namespace Solqaryn.Application.DTOs;

/// <summary>
/// Source-backed absolute profitability projection for N5.4.D.
/// No percentage margin or synthetic header-discount allocation is exposed.
/// </summary>
public sealed class ReporteRentabilidadDto
{
    public int? AgrupacionId { get; set; }
    public string Agrupacion { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Venta { get; set; }
    public decimal Costo { get; set; }
    public decimal UtilidadBruta { get; set; }
    public bool IncluyeDescuentoEncabezadoEnUtilidad { get; set; }
    public string Semantica { get; set; } = string.Empty;
}

public enum RentabilidadAgrupacion
{
    Vendedor,
    Cliente,
    Producto,
    Categoria
}
