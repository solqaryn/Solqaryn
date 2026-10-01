namespace Solqaryn.Domain.ValueObjects;

/// <summary>
/// Porción auditable del costo asignado a una salida. CapaCostoInventarioId
/// sólo se informa cuando el método requiere una capa durable (FIFO).
/// </summary>
public sealed record AsignacionCostoInventario
{
    public int Cantidad { get; }
    public decimal CostoUnitario { get; }
    public int? CapaCostoInventarioId { get; }
    public decimal CostoTotal => Cantidad * CostoUnitario;

    private AsignacionCostoInventario(int cantidad, decimal costoUnitario, int? capaCostoInventarioId)
    {
        Cantidad = cantidad;
        CostoUnitario = costoUnitario;
        CapaCostoInventarioId = capaCostoInventarioId;
    }

    public static AsignacionCostoInventario Crear(
        int cantidad,
        decimal costoUnitario,
        int? capaCostoInventarioId = null)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad asignada debe ser mayor a cero.");
        if (costoUnitario < 0m)
            throw new ArgumentOutOfRangeException(nameof(costoUnitario), "El costo unitario no puede ser negativo.");
        if (capaCostoInventarioId.HasValue && capaCostoInventarioId.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(capaCostoInventarioId), "La capa de costo debe ser válida.");

        return new AsignacionCostoInventario(cantidad, costoUnitario, capaCostoInventarioId);
    }
}
