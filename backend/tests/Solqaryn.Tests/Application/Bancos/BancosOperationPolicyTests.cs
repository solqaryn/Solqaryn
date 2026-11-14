using Solqaryn.Application.Bancos;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums.Bancos;
using Xunit;

namespace Solqaryn.Tests.Application.Bancos;

public class BancosOperationPolicyTests
{
    private static CuentaBancaria CrearCuenta(int id, string moneda = "HNL")
    {
        var cuenta = new CuentaBancaria(1, "Banco Test", $"CTA-{id}", moneda, 1000m)
        {
            Id = id
        };
        return cuenta;
    }

    [Fact]
    public void CuentaOrigenInactiva_LanzaExcepcion()
    {
        var origen = CrearCuenta(1);
        origen.Desactivar();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            BancosOperationPolicy.ValidarOperacionBancaria(origen, null, TipoOperacionBancaria.Retiro, 100m));

        Assert.Contains("activa", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MontoNegativo_LanzaExcepcion()
    {
        var origen = CrearCuenta(1);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            BancosOperationPolicy.ValidarOperacionBancaria(origen, null, TipoOperacionBancaria.Deposito, -50m));

        Assert.Contains("mayor que cero", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TransferenciaMismaCuenta_LanzaExcepcion()
    {
        var origen = CrearCuenta(1);
        var destino = CrearCuenta(1);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            BancosOperationPolicy.ValidarOperacionBancaria(origen, destino, TipoOperacionBancaria.Transferencia, 100m));

        Assert.Contains("distintas", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TransferenciaDestinoInactiva_LanzaExcepcion()
    {
        var origen = CrearCuenta(1);
        var destino = CrearCuenta(2);
        destino.Desactivar();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            BancosOperationPolicy.ValidarOperacionBancaria(origen, destino, TipoOperacionBancaria.Transferencia, 100m));

        Assert.Contains("activa", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TransferenciaMonedaDistinta_LanzaExcepcion()
    {
        var origen = CrearCuenta(1, "HNL");
        var destino = CrearCuenta(2, "USD");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            BancosOperationPolicy.ValidarOperacionBancaria(origen, destino, TipoOperacionBancaria.Transferencia, 100m));

        Assert.Contains("moneda", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TransferenciaValida_NoLanzaExcepcion()
    {
        var origen = CrearCuenta(1);
        var destino = CrearCuenta(2);

        var exception = Record.Exception(() =>
            BancosOperationPolicy.ValidarOperacionBancaria(origen, destino, TipoOperacionBancaria.Transferencia, 100m));

        Assert.Null(exception);
    }
}
