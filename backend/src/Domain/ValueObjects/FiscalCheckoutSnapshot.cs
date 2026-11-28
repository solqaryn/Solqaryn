using System.Text.Json;

namespace Solqaryn.Domain.ValueObjects;

/// <summary>Desglose monetario inmutable de un borrador de pedido, no una factura.</summary>
public sealed record FiscalCheckoutLinea(int ProductoId, int? ProductoVarianteId, int Unidades,
    decimal PrecioBase, decimal PrecioUnitario)
{
    public decimal Bruto => FiscalCheckoutSnapshot.Moneda(PrecioBase * Unidades);
    public decimal Neto => FiscalCheckoutSnapshot.Moneda(PrecioUnitario * Unidades);
}
public sealed record FiscalCheckoutImpuesto(int ImpuestoId, string Nombre, string Codigo,
    decimal Tasa, decimal BaseImponible, decimal Monto, bool IncluidoEnPrecio);

public sealed record FiscalCheckoutSnapshot(int Version, IReadOnlyList<FiscalCheckoutLinea> Lineas,
    IReadOnlyList<FiscalCheckoutImpuesto> Impuestos)
{
    public decimal ImporteProductos => Lineas.Sum(x => x.Bruto);
    public decimal Subtotal => Lineas.Sum(x => x.Neto);
    public decimal TotalDescuento => ImporteProductos - Subtotal;
    public decimal ImpuestoIncluido => Impuestos.Where(x => x.IncluidoEnPrecio).Sum(x => x.Monto);
    public decimal ImpuestoAdicional => Impuestos.Where(x => !x.IncluidoEnPrecio).Sum(x => x.Monto);
    public static decimal Moneda(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public void Validar()
    {
        if (Version != 1 || Lineas is null || Lineas.Count is < 1 or > 50 ||
            Impuestos is null || Impuestos.Count > 128 ||
            Lineas.Any(x => x is null || x.ProductoId <= 0 || x.ProductoVarianteId is <= 0 ||
                x.Unidades is < 1 or > 999 || x.PrecioUnitario <= 0 ||
                x.PrecioBase < x.PrecioUnitario || x.PrecioBase >= 100000000000000m ||
                decimal.Round(x.PrecioBase, 4) != x.PrecioBase ||
                decimal.Round(x.PrecioUnitario, 4) != x.PrecioUnitario) ||
            Lineas.Select(x => (x.ProductoId, x.ProductoVarianteId)).Distinct().Count() != Lineas.Count ||
            Impuestos.Any(x => x is null || x.ImpuestoId <= 0 || string.IsNullOrWhiteSpace(x.Nombre) ||
                string.IsNullOrWhiteSpace(x.Codigo) || x.Tasa < 0 || x.BaseImponible < 0 || x.Monto <= 0 ||
                Moneda(x.BaseImponible) != x.BaseImponible || Moneda(x.Monto) != x.Monto) ||
            Impuestos.Select(x => x.ImpuestoId).Distinct().Count() != Impuestos.Count ||
            ImpuestoIncluido > ImporteProductos || ImporteProductos + ImpuestoAdicional >= 10000000000000000m)
            throw new InvalidOperationException("El snapshot fiscal del checkout no es válido.");
    }
    public string Serializar()
    {
        Validar();
        var json = JsonSerializer.Serialize(this);
        if (json.Length > 32000) throw new InvalidOperationException("Desglose fiscal demasiado extenso.");
        return json;
    }
    public static FiscalCheckoutSnapshot? Leer(string? entrega)
    {
        if (entrega is null) return null;
        using var json = JsonDocument.Parse(entrega);
        if (json.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("El snapshot de checkout no es un objeto.");
        if (!json.RootElement.TryGetProperty("Fiscal", out var value)) return null;
        var fiscal = value.Deserialize<FiscalCheckoutSnapshot>()
            ?? throw new InvalidOperationException("Snapshot fiscal persistido inválido.");
        fiscal.Validar();
        return fiscal;
    }
}
