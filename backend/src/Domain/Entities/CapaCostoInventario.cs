using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Capa contable FIFO. Es deliberadamente independiente de LoteInventario:
/// la identidad logística y la identidad de valoración resuelven problemas distintos.
/// Una capa puede nacer de un movimiento real o del cutover explícito de apertura,
/// pero nunca se inventa un movimiento histórico para representar stock preexistente.
/// </summary>
public sealed class CapaCostoInventario : AuditableEntity
{
    public int ProductoVarianteId { get; private set; }
    public int AlmacenId { get; private set; }
    public int? UbicacionAlmacenId { get; private set; }
    public int? MovimientoInventarioOrigenId { get; private set; }
    public int? CapaCostoOrigenId { get; private set; }
    public bool EsApertura { get; private set; }
    public string? MotivoApertura { get; private set; }
    public int CantidadOriginal { get; private set; }
    public int CantidadRestante { get; private set; }
    public decimal CostoUnitario { get; private set; }
    public DateTime FechaOrigenUtc { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;

    public bool Agotada => CantidadRestante == 0;
    public decimal ValorRestante => CantidadRestante * CostoUnitario;

    private CapaCostoInventario()
    {
    }

    public static CapaCostoInventario CrearDesdeMovimiento(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId,
        int movimientoInventarioOrigenId,
        int cantidad,
        decimal costoUnitario,
        DateTime fechaOrigenUtc,
        string correlationId,
        int? capaCostoOrigenId = null)
    {
        ValidarBase(productoVarianteId, almacenId, ubicacionAlmacenId, cantidad, costoUnitario, fechaOrigenUtc, correlationId);
        if (movimientoInventarioOrigenId <= 0)
            throw new ArgumentOutOfRangeException(nameof(movimientoInventarioOrigenId));
        if (capaCostoOrigenId.HasValue && capaCostoOrigenId.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(capaCostoOrigenId));

        return new CapaCostoInventario
        {
            ProductoVarianteId = productoVarianteId,
            AlmacenId = almacenId,
            UbicacionAlmacenId = ubicacionAlmacenId,
            MovimientoInventarioOrigenId = movimientoInventarioOrigenId,
            CapaCostoOrigenId = capaCostoOrigenId,
            EsApertura = false,
            CantidadOriginal = cantidad,
            CantidadRestante = cantidad,
            CostoUnitario = costoUnitario,
            FechaOrigenUtc = fechaOrigenUtc,
            CorrelationId = correlationId.Trim()
        };
    }

    public static CapaCostoInventario CrearApertura(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId,
        int cantidad,
        decimal costoUnitario,
        DateTime fechaOrigenUtc,
        string correlationId,
        string motivoApertura)
    {
        ValidarBase(productoVarianteId, almacenId, ubicacionAlmacenId, cantidad, costoUnitario, fechaOrigenUtc, correlationId);
        if (string.IsNullOrWhiteSpace(motivoApertura))
            throw new ArgumentException("El motivo de apertura es obligatorio.", nameof(motivoApertura));

        return new CapaCostoInventario
        {
            ProductoVarianteId = productoVarianteId,
            AlmacenId = almacenId,
            UbicacionAlmacenId = ubicacionAlmacenId,
            MovimientoInventarioOrigenId = null,
            CapaCostoOrigenId = null,
            EsApertura = true,
            MotivoApertura = motivoApertura.Trim(),
            CantidadOriginal = cantidad,
            CantidadRestante = cantidad,
            CostoUnitario = costoUnitario,
            FechaOrigenUtc = fechaOrigenUtc,
            CorrelationId = correlationId.Trim()
        };
    }

    public void Consumir(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad a consumir debe ser mayor a cero.");
        if (cantidad > CantidadRestante)
            throw new InvalidOperationException("La capa FIFO no tiene saldo suficiente para el consumo solicitado.");

        CantidadRestante -= cantidad;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Restaurar(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad a restaurar debe ser mayor a cero.");
        if (CantidadRestante + cantidad > CantidadOriginal)
            throw new InvalidOperationException("La restauración no puede superar la cantidad original de la capa.");

        CantidadRestante += cantidad;
        FechaActualizacion = DateTime.UtcNow;
    }

    private static void ValidarBase(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId,
        int cantidad,
        decimal costoUnitario,
        DateTime fechaOrigenUtc,
        string correlationId)
    {
        if (productoVarianteId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productoVarianteId));
        if (almacenId <= 0)
            throw new ArgumentOutOfRangeException(nameof(almacenId));
        if (ubicacionAlmacenId.HasValue && ubicacionAlmacenId.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(ubicacionAlmacenId));
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad de apertura debe ser mayor a cero.");
        if (costoUnitario < 0m)
            throw new ArgumentOutOfRangeException(nameof(costoUnitario), "El costo unitario no puede ser negativo.");
        if (fechaOrigenUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha de origen debe expresarse en UTC.", nameof(fechaOrigenUtc));
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("CorrelationId es obligatorio para una capa de costo.", nameof(correlationId));
    }
}
