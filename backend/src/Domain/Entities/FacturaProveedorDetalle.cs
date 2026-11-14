using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class FacturaProveedorDetalle : AuditableEntity
{
    public int FacturaProveedorId { get; set; }
    public FacturaProveedor FacturaProveedor { get; set; } = null!;

    public int OrdenCompraDetalleId { get; set; }
    public OrdenCompraDetalle OrdenCompraDetalle { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }

    public decimal CantidadFacturada { get; private set; }
    public decimal PrecioUnitarioSnapshot { get; private set; }
    public decimal DescuentoSnapshot { get; private set; }
    public decimal ImpuestoSnapshot { get; private set; }

    public string? ProductoSkuSnapshot { get; set; }
    public string ProductoNombreSnapshot { get; set; } = string.Empty;
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
    public string? Observacion { get; set; }

    public decimal SubtotalSnapshot => CantidadFacturada * PrecioUnitarioSnapshot;
    public decimal TotalSnapshot => SubtotalSnapshot - DescuentoSnapshot + ImpuestoSnapshot;

    public void EstablecerValores(
        decimal cantidadFacturada,
        decimal precioUnitarioSnapshot,
        decimal descuentoSnapshot = 0m,
        decimal impuestoSnapshot = 0m)
    {
        if (cantidadFacturada <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidadFacturada), "La cantidad facturada debe ser mayor que cero.");
        if (precioUnitarioSnapshot < 0)
            throw new ArgumentOutOfRangeException(nameof(precioUnitarioSnapshot), "El precio unitario facturado no puede ser negativo.");
        if (descuentoSnapshot < 0)
            throw new ArgumentOutOfRangeException(nameof(descuentoSnapshot), "El descuento facturado no puede ser negativo.");
        if (impuestoSnapshot < 0)
            throw new ArgumentOutOfRangeException(nameof(impuestoSnapshot), "El impuesto facturado no puede ser negativo.");

        var subtotal = cantidadFacturada * precioUnitarioSnapshot;
        if (descuentoSnapshot > subtotal)
            throw new ArgumentOutOfRangeException(nameof(descuentoSnapshot), "El descuento no puede superar el subtotal facturado del detalle.");

        CantidadFacturada = cantidadFacturada;
        PrecioUnitarioSnapshot = precioUnitarioSnapshot;
        DescuentoSnapshot = descuentoSnapshot;
        ImpuestoSnapshot = impuestoSnapshot;
    }

    public void Validar()
    {
        if (OrdenCompraDetalleId <= 0)
            throw new InvalidOperationException("La línea de orden de compra es obligatoria.");
        if (ProductoId <= 0)
            throw new InvalidOperationException("El producto es obligatorio.");
        if (ProductoVarianteId is <= 0)
            throw new InvalidOperationException("La variante debe ser válida cuando se especifica.");
        if (CantidadFacturada <= 0)
            throw new InvalidOperationException("La cantidad facturada debe ser mayor que cero.");
        if (PrecioUnitarioSnapshot < 0 || DescuentoSnapshot < 0 || ImpuestoSnapshot < 0)
            throw new InvalidOperationException("Los importes facturados no pueden ser negativos.");
        if (DescuentoSnapshot > SubtotalSnapshot)
            throw new InvalidOperationException("El descuento no puede superar el subtotal facturado del detalle.");
        if (string.IsNullOrWhiteSpace(ProductoNombreSnapshot))
            throw new InvalidOperationException("El snapshot del nombre del producto es obligatorio.");
    }
}
