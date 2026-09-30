using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Domain.Enums.Bancos;
using Xunit;

namespace Solqaryn.Tests.Application.Bancos;

public class CuentaBancariaDtoContractTests
{
    [Fact]
    public void CuentaBancariaDto_Debe_Preservar_Propiedades_Correctamente()
    {
        var dto = new CuentaBancariaDto
        {
            Id = 1,
            BancoId = 100,
            Nombre = "Cuenta Principal",
            NumeroCuenta = "123456789",
            Moneda = "HNL",
            SaldoInicial = 5000.50m,
            Estado = EstadoCuentaBancaria.Activa
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal(100, dto.BancoId);
        Assert.Equal("Cuenta Principal", dto.Nombre);
        Assert.Equal("123456789", dto.NumeroCuenta);
        Assert.Equal("HNL", dto.Moneda);
        Assert.Equal(5000.50m, dto.SaldoInicial);
        Assert.Equal(EstadoCuentaBancaria.Activa, dto.Estado);
    }

    [Theory]
    [InlineData(TipoOperacionBancaria.Deposito)]
    [InlineData(TipoOperacionBancaria.Retiro)]
    [InlineData(TipoOperacionBancaria.Transferencia)]
    [InlineData(TipoOperacionBancaria.Comision)]
    [InlineData(TipoOperacionBancaria.Interes)]
    public void OperacionBancariaDto_Debe_Aceptar_Tipos_De_Operacion(TipoOperacionBancaria tipo)
    {
        var dto = new OperacionBancariaDto
        {
            TipoOperacion = tipo,
            Monto = 1000.75m,
            CuentaDestinoId = tipo == TipoOperacionBancaria.Transferencia ? 2 : null,
            Referencia = "REF-123"
        };

        Assert.Equal(tipo, dto.TipoOperacion);
        Assert.Equal(1000.75m, dto.Monto);
        Assert.Equal(tipo == TipoOperacionBancaria.Transferencia ? 2 : null, dto.CuentaDestinoId);
        Assert.Equal("REF-123", dto.Referencia);
    }
}
