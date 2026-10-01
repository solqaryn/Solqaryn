using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class SolicitudCompraDetalle : AuditableEntity
{
    public int SolicitudCompraId { get; set; }
    public SolicitudCompra SolicitudCompra { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }

    public decimal CantidadSolicitada { get; private set; }
    public decimal? CostoEstimadoUnitario { get; private set; }
    public string? Observacion { get; set; }

    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoNombreSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }

    public void EstablecerCantidad(decimal cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad solicitada debe ser mayor que cero.");

        CantidadSolicitada = cantidad;
    }

    public void EstablecerCostoEstimado(decimal? costoEstimadoUnitario)
    {
        if (costoEstimadoUnitario < 0)
            throw new ArgumentOutOfRangeException(nameof(costoEstimadoUnitario), "El costo estimado no puede ser negativo.");

        CostoEstimadoUnitario = costoEstimadoUnitario;
    }

    public void Validar()
    {
        if (ProductoId <= 0)
            throw new InvalidOperationException("El producto es obligatorio.");
        if (ProductoVarianteId is <= 0)
            throw new InvalidOperationException("La variante debe ser válida cuando se especifica.");
        if (CantidadSolicitada <= 0)
            throw new InvalidOperationException("La cantidad solicitada debe ser mayor que cero.");
        if (CostoEstimadoUnitario < 0)
            throw new InvalidOperationException("El costo estimado no puede ser negativo.");
    }
}
