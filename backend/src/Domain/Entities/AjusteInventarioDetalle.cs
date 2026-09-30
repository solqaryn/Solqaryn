using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Línea del documento de ajuste. Durante Borrador sólo conserva la cantidad objetivo;
/// los snapshots económicos se materializan bajo lock al confirmar.
/// </summary>
public class AjusteInventarioDetalle : BaseEntity
{
    public int AjusteInventarioId { get; set; }
    public AjusteInventario AjusteInventario { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }

    // Contexto físico N1.4. Nullable para históricos anteriores al cutover.
    public int? AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public UbicacionAlmacen? UbicacionAlmacen { get; set; }

    public int CantidadObjetivo { get; set; }
    public int? CantidadAnteriorSnapshot { get; private set; }
    public int? CantidadNuevaSnapshot { get; private set; }
    public int? DiferenciaSnapshot =>
        CantidadAnteriorSnapshot.HasValue && CantidadNuevaSnapshot.HasValue
            ? CantidadNuevaSnapshot.Value - CantidadAnteriorSnapshot.Value
            : null;
    public decimal? CostoUnitarioSnapshot { get; private set; }
    public decimal? ImpactoCostoSnapshot =>
        DiferenciaSnapshot.HasValue && CostoUnitarioSnapshot.HasValue
            ? DiferenciaSnapshot.Value * CostoUnitarioSnapshot.Value
            : null;

    public bool TieneSnapshotConfirmacion =>
        CantidadAnteriorSnapshot.HasValue &&
        CantidadNuevaSnapshot.HasValue &&
        CostoUnitarioSnapshot.HasValue;

    public string? NombreSnapshot { get; set; }
    public string? SkuSnapshot { get; set; }
    public string? MarcaSnapshot { get; set; }
    public string? ModeloSnapshot { get; set; }
    public string? ColorSnapshot { get; set; }
    public string? TallaSnapshot { get; set; }

    public void MaterializarConfirmacion(int cantidadAnterior, decimal costoUnitario)
    {
        if (ProductoId <= 0)
            throw new InvalidOperationException("El detalle debe referenciar un producto válido.");
        if (ProductoVarianteId.HasValue && ProductoVarianteId.Value <= 0)
            throw new InvalidOperationException("La variante del detalle debe ser válida cuando se informa.");
        if (CantidadObjetivo < 0)
            throw new InvalidOperationException("La cantidad objetivo no puede ser negativa.");
        if (cantidadAnterior < 0)
            throw new ArgumentOutOfRangeException(nameof(cantidadAnterior), "La cantidad anterior no puede ser negativa.");
        if (costoUnitario < 0)
            throw new ArgumentOutOfRangeException(nameof(costoUnitario), "El costo unitario no puede ser negativo.");
        if (CantidadObjetivo == cantidadAnterior)
            throw new InvalidOperationException("Un detalle de ajuste debe producir una diferencia real de inventario.");

        CantidadAnteriorSnapshot = cantidadAnterior;
        CantidadNuevaSnapshot = CantidadObjetivo;
        CostoUnitarioSnapshot = costoUnitario;
    }
}
