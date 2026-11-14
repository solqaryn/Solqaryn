using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Evidencia inmutable del costo atribuido a un movimiento confirmado. FIFO
/// referencia la capa consumida; Promedio/Estándar no pueden apropiarse de una
/// capa contable FIFO.
/// </summary>
public sealed class AsignacionCostoMovimientoInventario : AuditableEntity
{
    public int MovimientoInventarioId { get; private set; }
    public int ProductoVarianteId { get; private set; }
    public int? CapaCostoInventarioId { get; private set; }
    public MetodoCosteoInventario Metodo { get; private set; }
    public int Cantidad { get; private set; }
    public decimal CostoUnitario { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;

    public decimal CostoTotal => Cantidad * CostoUnitario;

    private AsignacionCostoMovimientoInventario()
    {
    }

    public static AsignacionCostoMovimientoInventario Crear(
        int movimientoInventarioId,
        int productoVarianteId,
        MetodoCosteoInventario metodo,
        int cantidad,
        decimal costoUnitario,
        string correlationId,
        int? capaCostoInventarioId = null)
    {
        if (movimientoInventarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(movimientoInventarioId));
        if (productoVarianteId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productoVarianteId));
        if (!Enum.IsDefined(typeof(MetodoCosteoInventario), metodo))
            throw new ArgumentOutOfRangeException(nameof(metodo));
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad));
        if (costoUnitario < 0m)
            throw new ArgumentOutOfRangeException(nameof(costoUnitario));
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("CorrelationId es obligatorio.", nameof(correlationId));
        if (capaCostoInventarioId.HasValue && capaCostoInventarioId.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(capaCostoInventarioId));
        if (metodo == MetodoCosteoInventario.FIFO && !capaCostoInventarioId.HasValue)
            throw new ArgumentException("Una asignación FIFO debe identificar la capa consumida.", nameof(capaCostoInventarioId));
        if (metodo != MetodoCosteoInventario.FIFO && capaCostoInventarioId.HasValue)
            throw new ArgumentException("Sólo FIFO puede referenciar una capa de costo.", nameof(capaCostoInventarioId));

        return new AsignacionCostoMovimientoInventario
        {
            MovimientoInventarioId = movimientoInventarioId,
            ProductoVarianteId = productoVarianteId,
            CapaCostoInventarioId = capaCostoInventarioId,
            Metodo = metodo,
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            CorrelationId = correlationId.Trim()
        };
    }
}
