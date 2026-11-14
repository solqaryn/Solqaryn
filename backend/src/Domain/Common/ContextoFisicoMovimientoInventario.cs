namespace Solqaryn.Domain.Common;

/// <summary>
/// Contexto físico y de correlación obligatorio para todo movimiento de Kardex
/// generado después del cutover ERP-N1.5. Los históricos pueden seguir teniendo
/// columnas físicas/correlación nulas en persistencia, pero los writers nuevos no.
/// </summary>
public sealed record ContextoFisicoMovimientoInventario
{
    public const int MaxCorrelationIdLength = 64;

    public int ProductoVarianteId { get; }
    public int AlmacenId { get; }
    public int? UbicacionAlmacenId { get; }
    public string CorrelationId { get; }

    private ContextoFisicoMovimientoInventario(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId,
        string correlationId)
    {
        if (productoVarianteId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productoVarianteId), "ProductoVarianteId debe ser mayor que cero.");
        if (almacenId <= 0)
            throw new ArgumentOutOfRangeException(nameof(almacenId), "AlmacenId debe ser mayor que cero.");
        if (ubicacionAlmacenId.HasValue && ubicacionAlmacenId.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(ubicacionAlmacenId), "UbicacionAlmacenId debe ser mayor que cero cuando se informa.");

        var correlationIdNormalizado = correlationId?.Trim();
        if (string.IsNullOrWhiteSpace(correlationIdNormalizado))
            throw new ArgumentException("CorrelationId es obligatorio para movimientos nuevos de Kardex.", nameof(correlationId));
        if (correlationIdNormalizado.Length > MaxCorrelationIdLength)
            throw new ArgumentOutOfRangeException(nameof(correlationId), $"CorrelationId no puede exceder {MaxCorrelationIdLength} caracteres.");
        if (!correlationIdNormalizado.All(EsCaracterSeguroCorrelationId))
            throw new ArgumentException("CorrelationId contiene caracteres no permitidos.", nameof(correlationId));

        ProductoVarianteId = productoVarianteId;
        AlmacenId = almacenId;
        UbicacionAlmacenId = ubicacionAlmacenId;
        CorrelationId = correlationIdNormalizado;
    }

    public static ContextoFisicoMovimientoInventario Crear(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId,
        string correlationId) =>
        new(productoVarianteId, almacenId, ubicacionAlmacenId, correlationId);

    private static bool EsCaracterSeguroCorrelationId(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is '-' or '_' or '.' or ':';
}
