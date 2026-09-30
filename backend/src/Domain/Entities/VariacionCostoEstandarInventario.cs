using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Evidencia de diferencia entre costo real y costo estándar aplicada a una
/// entrada. El signo se conserva: positivo = real sobre estándar; negativo = real bajo estándar.
/// </summary>
public sealed class VariacionCostoEstandarInventario : AuditableEntity
{
    public int MovimientoInventarioId { get; private set; }
    public int ProductoVarianteId { get; private set; }
    public int CostoEstandarInventarioId { get; private set; }
    public int Cantidad { get; private set; }
    public decimal CostoRealUnitario { get; private set; }
    public decimal CostoEstandarUnitario { get; private set; }
    public decimal VariacionTotal { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;

    private VariacionCostoEstandarInventario()
    {
    }

    public static VariacionCostoEstandarInventario Crear(
        int movimientoInventarioId,
        int productoVarianteId,
        int costoEstandarInventarioId,
        int cantidad,
        decimal costoRealUnitario,
        decimal costoEstandarUnitario,
        string correlationId)
    {
        if (movimientoInventarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(movimientoInventarioId));
        if (productoVarianteId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productoVarianteId));
        if (costoEstandarInventarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(costoEstandarInventarioId));
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad));
        if (costoRealUnitario < 0m)
            throw new ArgumentOutOfRangeException(nameof(costoRealUnitario));
        if (costoEstandarUnitario < 0m)
            throw new ArgumentOutOfRangeException(nameof(costoEstandarUnitario));
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("CorrelationId es obligatorio.", nameof(correlationId));

        return new VariacionCostoEstandarInventario
        {
            MovimientoInventarioId = movimientoInventarioId,
            ProductoVarianteId = productoVarianteId,
            CostoEstandarInventarioId = costoEstandarInventarioId,
            Cantidad = cantidad,
            CostoRealUnitario = costoRealUnitario,
            CostoEstandarUnitario = costoEstandarUnitario,
            VariacionTotal = (costoRealUnitario - costoEstandarUnitario) * cantidad,
            CorrelationId = correlationId.Trim()
        };
    }
}
