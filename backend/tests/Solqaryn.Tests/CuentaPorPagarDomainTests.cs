using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class CuentaPorPagarDomainTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Validar_RechazaCondicionPagoFueraDelEnum(int valor)
    {
        var fecha = new DateTime(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc);
        var cuenta = new CuentaPorPagar
        {
            FacturaProveedorId = 10,
            ProveedorId = 20,
            Moneda = "HNL",
            CondicionPago = (CondicionPagoProveedor)valor,
            FechaEmisionUtc = fecha,
            FechaVencimientoUtc = fecha.AddDays(30),
            MontoOriginal = 100m
        };

        Assert.Throws<InvalidOperationException>(() => cuenta.Validar());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Aplicar_RechazaTipoFueraDelEnum(int valor)
    {
        var fecha = new DateTime(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc);
        var cuenta = new CuentaPorPagar
        {
            FacturaProveedorId = 10,
            ProveedorId = 20,
            Moneda = "HNL",
            CondicionPago = CondicionPagoProveedor.Credito,
            FechaEmisionUtc = fecha,
            FechaVencimientoUtc = fecha.AddDays(30),
            MontoOriginal = 100m
        };
        cuenta.Validar();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cuenta.Aplicar((TipoAplicacionCuentaPorPagar)valor, 10m, "n28b-enum-guard", fecha));
    }
}
