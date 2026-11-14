namespace Solqaryn.Domain.ValueObjects;

/// <summary>
/// Regla pura de Promedio Ponderado Móvil compatible con la valoración histórica
/// de CompraService. N1.10.D reutilizará esta regla desde el motor de costeo.
/// </summary>
public static class CosteoPromedioPonderado
{
    public static decimal CalcularCostoUnitario(
        int stockAnterior,
        decimal costoAnterior,
        int cantidadEntrada,
        decimal valorEntrada)
    {
        if (stockAnterior < 0)
            throw new ArgumentOutOfRangeException(nameof(stockAnterior), "El stock anterior no puede ser negativo.");
        if (costoAnterior < 0m)
            throw new ArgumentOutOfRangeException(nameof(costoAnterior), "El costo anterior no puede ser negativo.");
        if (cantidadEntrada <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidadEntrada), "La cantidad de entrada debe ser mayor a cero.");
        if (valorEntrada < 0m)
            throw new ArgumentOutOfRangeException(nameof(valorEntrada), "El valor de entrada no puede ser negativo.");

        var stockNuevo = checked(stockAnterior + cantidadEntrada);
        var valorAnterior = costoAnterior * stockAnterior;
        return Math.Round(
            (valorAnterior + valorEntrada) / stockNuevo,
            2,
            MidpointRounding.AwayFromZero);
    }
}
