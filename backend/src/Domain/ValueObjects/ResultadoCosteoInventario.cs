using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.ValueObjects;

/// <summary>
/// Resultado inmutable de valorar una cantidad de inventario. El costo histórico
/// de una salida debe persistir este resultado y no recalcularse desde el costo
/// corriente al consultar.
/// </summary>
public sealed class ResultadoCosteoInventario
{
    public MetodoCosteoInventario Metodo { get; }
    public int Cantidad { get; }
    public decimal CostoTotal { get; }
    public decimal CostoUnitarioPromedio => Cantidad == 0 ? 0m : CostoTotal / Cantidad;
    public IReadOnlyList<AsignacionCostoInventario> Asignaciones { get; }

    private ResultadoCosteoInventario(
        MetodoCosteoInventario metodo,
        int cantidad,
        decimal costoTotal,
        IReadOnlyList<AsignacionCostoInventario> asignaciones)
    {
        Metodo = metodo;
        Cantidad = cantidad;
        CostoTotal = costoTotal;
        Asignaciones = asignaciones;
    }

    public static ResultadoCosteoInventario Crear(
        MetodoCosteoInventario metodo,
        int cantidad,
        IEnumerable<AsignacionCostoInventario> asignaciones)
    {
        if (!Enum.IsDefined(metodo))
            throw new ArgumentOutOfRangeException(nameof(metodo), "El método de costeo no es válido.");
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad valorada debe ser mayor a cero.");

        var materializadas = asignaciones?.ToArray()
            ?? throw new ArgumentNullException(nameof(asignaciones));
        if (materializadas.Length == 0)
            throw new ArgumentException("La valoración debe contener al menos una asignación de costo.", nameof(asignaciones));
        if (materializadas.Sum(x => x.Cantidad) != cantidad)
            throw new ArgumentException("La suma de cantidades asignadas debe coincidir con la cantidad valorada.", nameof(asignaciones));

        var costoTotal = materializadas.Sum(x => x.CostoTotal);
        if (costoTotal < 0m)
            throw new InvalidOperationException("El costo total no puede ser negativo.");

        if (metodo == MetodoCosteoInventario.FIFO && materializadas.Any(x => !x.CapaCostoInventarioId.HasValue))
            throw new ArgumentException("FIFO requiere identificar la capa de costo de cada asignación.", nameof(asignaciones));

        return new ResultadoCosteoInventario(metodo, cantidad, costoTotal, materializadas);
    }
}
