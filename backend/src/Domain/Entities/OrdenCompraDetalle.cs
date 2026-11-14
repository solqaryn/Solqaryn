using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class OrdenCompraDetalle : AuditableEntity
{
    public int OrdenCompraId { get; set; }
    public OrdenCompra OrdenCompra { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }

    public decimal CantidadOrdenada { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal Descuento { get; private set; }
    public decimal Impuesto { get; private set; }
    public string? Observacion { get; set; }

    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoNombreSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }

    public decimal Subtotal => CantidadOrdenada * PrecioUnitario;
    public decimal Total => Subtotal - Descuento + Impuesto;

    public void EstablecerValores(decimal cantidadOrdenada, decimal precioUnitario, decimal descuento = 0m, decimal impuesto = 0m)
    {
        if (cantidadOrdenada <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidadOrdenada), "La cantidad ordenada debe ser mayor que cero.");
        if (precioUnitario < 0)
            throw new ArgumentOutOfRangeException(nameof(precioUnitario), "El precio unitario no puede ser negativo.");
        if (descuento < 0)
            throw new ArgumentOutOfRangeException(nameof(descuento), "El descuento no puede ser negativo.");
        if (impuesto < 0)
            throw new ArgumentOutOfRangeException(nameof(impuesto), "El impuesto no puede ser negativo.");
        if (descuento > cantidadOrdenada * precioUnitario)
            throw new ArgumentOutOfRangeException(nameof(descuento), "El descuento no puede superar el subtotal del detalle.");

        CantidadOrdenada = cantidadOrdenada;
        PrecioUnitario = precioUnitario;
        Descuento = descuento;
        Impuesto = impuesto;
    }

    public void Validar()
    {
        if (ProductoId <= 0)
            throw new InvalidOperationException("El producto es obligatorio.");
        if (ProductoVarianteId is <= 0)
            throw new InvalidOperationException("La variante debe ser válida cuando se especifica.");
        if (CantidadOrdenada <= 0)
            throw new InvalidOperationException("La cantidad ordenada debe ser mayor que cero.");
        if (PrecioUnitario < 0 || Descuento < 0 || Impuesto < 0)
            throw new InvalidOperationException("Los importes del detalle no pueden ser negativos.");
        if (Descuento > Subtotal)
            throw new InvalidOperationException("El descuento no puede superar el subtotal del detalle.");
    }
}
