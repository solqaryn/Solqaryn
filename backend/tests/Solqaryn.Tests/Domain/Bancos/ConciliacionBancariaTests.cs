using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums.Bancos;
using Xunit;

namespace Solqaryn.Tests.Domain.Bancos;

public class ConciliacionBancariaTests
{
    [Fact]
    public void Constructor_SetsInitialState()
    {
        var conciliacion = new ConciliacionBancaria(1, DateTime.Today, DateTime.Today.AddDays(1), 100, 200, "Ref");

        Assert.Equal(1, conciliacion.CuentaBancariaId);
        Assert.Equal(100, conciliacion.SaldoInicialBanco);
        Assert.Equal(200, conciliacion.SaldoFinalBanco);
        Assert.Equal(EstadoConciliacionBancaria.Borrador, conciliacion.Estado);
    }

    [Fact]
    public void AgregarMovimiento_MovimientoValido_AddsToCollection()
    {
        var conciliacion = new ConciliacionBancaria(1, DateTime.Today, DateTime.Today.AddDays(1), 100, 200, "Ref");
        var movimiento = new MovimientoEstadoCuenta("EXT-1", DateTime.Today, "Desc", "Ref", TipoMovimientoEstadoCuenta.Credito, 50);

        conciliacion.AgregarMovimiento(movimiento);

        Assert.Single(conciliacion.Movimientos);
        Assert.Equal(movimiento, conciliacion.Movimientos.First());
    }

    [Fact]
    public void AgregarMovimiento_ConciliacionCompletada_ThrowsException()
    {
        var conciliacion = new ConciliacionBancaria(1, DateTime.Today, DateTime.Today.AddDays(1), 100, 100, "Ref");
        conciliacion.MarcarComoEnProceso();
        conciliacion.Completar();
        var movimiento = new MovimientoEstadoCuenta("EXT-1", DateTime.Today, "Desc", "Ref", TipoMovimientoEstadoCuenta.Credito, 50);

        var ex = Assert.Throws<InvalidOperationException>(() => conciliacion.AgregarMovimiento(movimiento));
        Assert.Contains("Solo se pueden agregar movimientos", ex.Message);
    }
}
