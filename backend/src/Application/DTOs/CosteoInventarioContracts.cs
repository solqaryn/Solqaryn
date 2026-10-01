namespace Solqaryn.Application.DTOs;

/// <summary>
/// Entrada autoritativa a la valoración. El costo real de adquisición siempre
/// se conserva aunque la política activa sea Costo Estándar.
/// </summary>
public sealed class CosteoInventarioEntradaRequest
{
    public int ProductoVarianteId { get; init; }
    public int AlmacenId { get; init; }
    public int? UbicacionAlmacenId { get; init; }
    public int Cantidad { get; init; }
    public decimal CostoUnitarioReal { get; init; }
    public int MovimientoInventarioId { get; init; }
    public DateTime FechaUtc { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}

/// <summary>
/// Salida a valorar bajo la política vigente. El resultado debe congelarse en
/// el movimiento/documento confirmado, no durante la edición de un borrador.
/// </summary>
public sealed class CosteoInventarioSalidaRequest
{
    public int ProductoVarianteId { get; init; }
    public int AlmacenId { get; init; }
    public int? UbicacionAlmacenId { get; init; }
    public int Cantidad { get; init; }
    public int MovimientoInventarioId { get; init; }
    public DateTime FechaUtc { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}

/// <summary>
/// Reversión de una valoración previamente confirmada. Debe referenciar el
/// movimiento original para no reinterpretar el costo con la política actual.
/// </summary>
public sealed class CosteoInventarioReversionRequest
{
    public int MovimientoInventarioOriginalId { get; init; }
    public int MovimientoInventarioReversionId { get; init; }
    public DateTime FechaUtc { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}
