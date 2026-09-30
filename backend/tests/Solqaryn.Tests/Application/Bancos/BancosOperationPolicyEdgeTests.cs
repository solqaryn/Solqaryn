using System;
using Solqaryn.Application.Bancos;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums.Bancos;
using Xunit;

namespace Solqaryn.Tests.Application.Bancos;

public class BancosOperationPolicyEdgeTests
{
    [Fact]
    public void ValidarOperacionBancaria_DestinoProporcionadoParaOperacionNoTransferencia_LanzaArgumentException()
    {
        var origen = new CuentaBancaria(1, "Origen", "123", "HNL");
        var propertyInfo = typeof(CuentaBancaria).GetProperty("Id");
        if (propertyInfo != null)
        {
            propertyInfo.SetValue(origen, 1);
        }

        var destino = new CuentaBancaria(1, "Destino", "456", "HNL");
        if (propertyInfo != null)
        {
            propertyInfo.SetValue(destino, 2);
        }

        Action act = () => BancosOperationPolicy.ValidarOperacionBancaria(origen, destino, TipoOperacionBancaria.Deposito, 100m);

        var ex = Assert.Throws<ArgumentException>(act);
        Assert.Equal("Solo las transferencias permiten una cuenta destino. (Parameter 'destino')", ex.Message);
    }

    [Fact]
    public void ValidarOperacionBancaria_MismaInstanciaTransitoriaComoOrigenYDestino_LanzaInvalidOperationException()
    {
        var cuenta = new CuentaBancaria(1, "Transitoria", "000", "HNL");

        Action act = () => BancosOperationPolicy.ValidarOperacionBancaria(cuenta, cuenta, TipoOperacionBancaria.Transferencia, 100m);

        var ex = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal("La cuenta origen y destino de una transferencia deben ser distintas.", ex.Message);
    }
}
