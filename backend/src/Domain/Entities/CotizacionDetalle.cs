using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class CotizacionDetalle : AuditableEntity
{
    public int CotizacionId { get; set; }
    public Cotizacion Cotizacion { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }

    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoNombreSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }

    public decimal Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal Total => Cantidad * PrecioUnitario;

    public void EstablecerValores(decimal cantidad, decimal precioUnitario)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");
        if (precioUnitario < 0)
            throw new ArgumentOutOfRangeException(nameof(precioUnitario), "El precio unitario no puede ser negativo.");

        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
    }

    public void Validar()
    {
        if (ProductoId <= 0)
            throw new InvalidOperationException("El producto es obligatorio.");
        if (ProductoVarianteId is <= 0)
            throw new InvalidOperationException("La variante debe ser válida cuando se especifica.");
        if (Cantidad <= 0)
            throw new InvalidOperationException("La cantidad debe ser mayor que cero.");
        if (PrecioUnitario < 0)
            throw new InvalidOperationException("El precio unitario no puede ser negativo.");
    }
}
