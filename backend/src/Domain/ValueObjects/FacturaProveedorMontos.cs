namespace Solqaryn.Domain.ValueObjects;

public sealed record FacturaProveedorMontos
{
    public decimal Subtotal { get; }
    public decimal Descuento { get; }
    public decimal Impuesto { get; }
    public decimal Total { get; }

    private FacturaProveedorMontos(decimal subtotal, decimal descuento, decimal impuesto, decimal total)
    {
        Subtotal = subtotal;
        Descuento = descuento;
        Impuesto = impuesto;
        Total = total;
    }

    public static FacturaProveedorMontos Crear(decimal subtotal, decimal impuesto, decimal total)
        => Crear(subtotal, 0m, impuesto, total);

    public static FacturaProveedorMontos Crear(decimal subtotal, decimal descuento, decimal impuesto, decimal total)
    {
        if (subtotal < 0m)
            throw new ArgumentOutOfRangeException(nameof(subtotal), "El subtotal no puede ser negativo.");
        if (descuento < 0m)
            throw new ArgumentOutOfRangeException(nameof(descuento), "El descuento no puede ser negativo.");
        if (impuesto < 0m)
            throw new ArgumentOutOfRangeException(nameof(impuesto), "El impuesto no puede ser negativo.");
        if (total < 0m)
            throw new ArgumentOutOfRangeException(nameof(total), "El total no puede ser negativo.");

        var subtotalNormalizado = Redondear(subtotal);
        var descuentoNormalizado = Redondear(descuento);
        var impuestoNormalizado = Redondear(impuesto);
        var totalNormalizado = Redondear(total);

        if (descuentoNormalizado > subtotalNormalizado)
            throw new ArgumentOutOfRangeException(nameof(descuento), "El descuento no puede superar el subtotal.");

        var totalCalculado = Redondear(subtotalNormalizado - descuentoNormalizado + impuestoNormalizado);
        if (totalNormalizado != totalCalculado)
        {
            throw new ArgumentException(
                $"El total ({total}) debe ser subtotal ({subtotal}) menos descuento ({descuento}) más impuesto ({impuesto}), redondeado a 2 decimales.",
                nameof(total));
        }

        return new FacturaProveedorMontos(
            subtotalNormalizado,
            descuentoNormalizado,
            impuestoNormalizado,
            totalNormalizado);
    }

    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
