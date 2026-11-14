using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums.Bancos;
using Xunit;

namespace Solqaryn.Tests.Domain.Bancos;

public class CuentaBancariaDomainTests
{
    [Fact]
    public void CuentaBancaria_NormalizaIdentidadYQuedaActiva()
    {
        var cuenta = new CuentaBancaria(7, " Operativa ", " 001-123 ", " hnl ", 100m);

        Assert.Equal(7, cuenta.BancoId);
        Assert.Equal("Operativa", cuenta.Nombre);
        Assert.Equal("001-123", cuenta.NumeroCuenta);
        Assert.Equal("HNL", cuenta.Moneda);
        Assert.Equal(100m, cuenta.SaldoInicial);
        Assert.Equal(EstadoCuentaBancaria.Activa, cuenta.Estado);
    }

    [Theory]
    [InlineData(TipoOperacionBancaria.Deposito)]
    [InlineData(TipoOperacionBancaria.Retiro)]
    [InlineData(TipoOperacionBancaria.Comision)]
    [InlineData(TipoOperacionBancaria.Interes)]
    [InlineData(TipoOperacionBancaria.ConciliacionAjuste)]
    public void CuentaBancaria_ValidaOperacionesSinCuentaDestino(TipoOperacionBancaria tipo)
    {
        var cuenta = new CuentaBancaria(7, "Operativa", "001-123", "HNL");

        cuenta.ValidarOperacion(tipo, 10m);
    }

    [Fact]
    public void CuentaBancaria_TransferenciaExigeDestinoDistinto()
    {
        var cuenta = new CuentaBancaria(7, "Operativa", "001-123", "HNL") { Id = 25 };

        Assert.Throws<ArgumentException>(() =>
            cuenta.ValidarOperacion(TipoOperacionBancaria.Transferencia, 10m));
        Assert.Throws<InvalidOperationException>(() =>
            cuenta.ValidarOperacion(TipoOperacionBancaria.Transferencia, 10m, 25));

        cuenta.ValidarOperacion(TipoOperacionBancaria.Transferencia, 10m, 26);
    }

    [Fact]
    public void CuentaBancaria_RechazaOperacionSiEstaInactivaOMontoNoPositivo()
    {
        var cuenta = new CuentaBancaria(7, "Operativa", "001-123", "HNL");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cuenta.ValidarOperacion(TipoOperacionBancaria.Deposito, 0m));

        cuenta.Desactivar();

        Assert.Throws<InvalidOperationException>(() =>
            cuenta.ValidarOperacion(TipoOperacionBancaria.Deposito, 10m));
    }

    [Fact]
    public void CuentaBancaria_NoAceptaDestinoEnOperacionNoTransferencia()
    {
        var cuenta = new CuentaBancaria(7, "Operativa", "001-123", "HNL");

        Assert.Throws<ArgumentException>(() =>
            cuenta.ValidarOperacion(TipoOperacionBancaria.Deposito, 10m, 26));
    }
}
