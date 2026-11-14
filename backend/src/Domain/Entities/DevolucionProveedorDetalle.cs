using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class DevolucionProveedorDetalle : AuditableEntity
{
    public int DevolucionProveedorId { get; set; }
    public int RecepcionCompraDetalleId { get; set; }
    public int OrdenCompraDetalleId { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitarioSnapshot { get; set; }
    public decimal ImpuestoUnitarioSnapshot { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
    public string ProductoNombreSnapshot { get; set; } = string.Empty;
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }

    public decimal SubtotalCredito => decimal.Round(Cantidad * CostoUnitarioSnapshot, 4, MidpointRounding.AwayFromZero);
    public decimal ImpuestoCredito => decimal.Round(Cantidad * ImpuestoUnitarioSnapshot, 4, MidpointRounding.AwayFromZero);
    public decimal TotalCredito => SubtotalCredito + ImpuestoCredito;

    public void Validar()
    {
        if (RecepcionCompraDetalleId <= 0)
            throw new InvalidOperationException("El detalle de recepción de origen es obligatorio.");
        if (OrdenCompraDetalleId <= 0)
            throw new InvalidOperationException("La línea de orden de compra es obligatoria.");
        if (ProductoId <= 0)
            throw new InvalidOperationException("El producto es obligatorio.");
        if (AlmacenId <= 0)
            throw new InvalidOperationException("El almacén de origen es obligatorio.");
        if (Cantidad <= 0m)
            throw new InvalidOperationException("La cantidad devuelta debe ser mayor que cero.");
        if (CostoUnitarioSnapshot < 0m)
            throw new InvalidOperationException("El costo unitario no puede ser negativo.");
        if (ImpuestoUnitarioSnapshot < 0m)
            throw new InvalidOperationException("El impuesto unitario no puede ser negativo.");
        if (string.IsNullOrWhiteSpace(ProductoNombreSnapshot))
            throw new InvalidOperationException("El snapshot del nombre del producto es obligatorio.");
    }
}
