namespace Solqaryn.Application.DTOs;

/// <summary>
/// Aggregated monetary result for the sales-report application contract.
/// Values map directly to persisted Venta monetary fields; no derived percentage
/// or accounting meaning is invented here.
/// </summary>
public sealed class ReporteVentasResumenDto
{
    public decimal ImporteBruto { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public decimal CostoTotal { get; set; }
    public decimal UtilidadBruta { get; set; }
}

/// <summary>
/// Bounded row contract for sales-report detail. Historical display dimensions
/// use the durable Venta/VentaDetalle snapshots. Identity fields that require a
/// current master join remain explicit and nullable instead of being presented
/// as historical snapshots.
/// </summary>
public sealed class ReporteVentasDetalleDto
{
    public int VentaId { get; set; }
    public string NumeroVenta { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }

    public int? ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public int? VendedorId { get; set; }
    public string? VendedorNombre { get; set; }

    // Sucursal is derived at detail level through VentaDetalle.Almacen -> Sucursal.
    public int? SucursalId { get; set; }

    // CategoriaId is current-master identity; VentaDetalle has no category snapshot.
    public int? CategoriaId { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;
    public string ProductoMarca { get; set; } = string.Empty;
    public string ProductoModelo { get; set; } = string.Empty;
    public string? ProductoColor { get; set; }
    public string? ProductoTalla { get; set; }
    public string? ProductoSku { get; set; }

    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public decimal UtilidadBruta { get; set; }
}
