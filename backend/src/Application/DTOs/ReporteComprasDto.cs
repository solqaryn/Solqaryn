using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class ReporteComprasFiltroDto
{
    public DateTime? DesdeUtc { get; set; }
    public DateTime? HastaUtc { get; set; }
    public int? ProveedorId { get; set; }
    public int? ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public EstadoOrdenCompra? EstadoOrden { get; set; }
    public EstadoFacturaProveedor? EstadoFactura { get; set; }
    public EstadoRecepcionCompra? EstadoRecepcion { get; set; }
    public EstadoDevolucionProveedor? EstadoDevolucion { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed class ReporteComprasDetalleDto
{
    public int OrdenCompraId { get; set; }
    public int OrdenCompraDetalleId { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; set; }
    public DateTime? FechaEsperadaUtc { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public string Moneda { get; set; } = string.Empty;
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public string? ProductoSku { get; set; }
    public string? ProductoNombre { get; set; }
    public string? ProductoMarca { get; set; }
    public string? ProductoModelo { get; set; }
    public string? ProductoColor { get; set; }
    public string? ProductoTalla { get; set; }
    public decimal CantidadOrdenada { get; set; }
    public decimal PrecioUnitarioOrdenado { get; set; }
    public decimal? PrecioUnitarioFacturado { get; set; }
    public string? MonedaFactura { get; set; }
    public decimal? VariacionPrecioAbsoluta { get; set; }
    public decimal CantidadRecibida { get; set; }
    public decimal CantidadAceptada { get; set; }
    public decimal CantidadDanada { get; set; }
    public decimal CantidadFaltante { get; set; }
    public decimal CantidadSobrante { get; set; }
    public decimal CantidadDevueltaEfectiva { get; set; }
    public DateTime? FechaEsperadaEvaluadaUtc { get; set; }
    public DateTime? FechaRecepcionEvaluadaUtc { get; set; }
    public int? DesviacionEntregaDias { get; set; }
    public decimal? CantidadEvaluadaOrdenada { get; set; }
    public decimal? CantidadEvaluadaAceptada { get; set; }
    public decimal? CantidadEvaluadaDanada { get; set; }
    public decimal? CantidadEvaluadaSobrante { get; set; }
}
