using Xunit;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums.Bancos;

namespace Solqaryn.Tests.Domain.Bancos;

public class ConciliacionBancariaDomainTests
{
    [Fact]
    public void ConciliacionBancaria_ValidParameters_CreatesSuccessfully()
    {
        var conciliacion = new ConciliacionBancaria(1, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, 1000m, 2000m, "Test");
        Assert.Equal(1, conciliacion.CuentaBancariaId);
        Assert.Equal(1000m, conciliacion.SaldoInicialBanco);
        Assert.Equal(2000m, conciliacion.SaldoFinalBanco);
        Assert.Equal(EstadoConciliacionBancaria.Borrador, conciliacion.Estado);
    }

    [Fact]
    public void ConciliacionBancaria_InvalidFechas_ThrowsException() => Assert.Throws<ArgumentException>(() => new ConciliacionBancaria(1, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 1000m, 2000m));

    [Fact]
    public void AgregarMovimiento_IdempotencyKeyUnica_AgregaCorrectamente()
    {
        var conciliacion = new ConciliacionBancaria(1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 100, 200);
        conciliacion.AgregarMovimiento(new MovimientoEstadoCuenta("IDEMP1", DateTime.UtcNow, "Depósito", "REF1", TipoMovimientoEstadoCuenta.Credito, 100));
        Assert.Single(conciliacion.Movimientos);
    }

    [Fact]
    public void AgregarMovimiento_IdempotencyKeyDuplicada_ThrowsException()
    {
        var conciliacion = new ConciliacionBancaria(1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 100, 200);
        conciliacion.AgregarMovimiento(new MovimientoEstadoCuenta("IDEMP1", DateTime.UtcNow, "Depósito 1", "REF1", TipoMovimientoEstadoCuenta.Credito, 100));
        Assert.Throws<InvalidOperationException>(() => conciliacion.AgregarMovimiento(new MovimientoEstadoCuenta("IDEMP1", DateTime.UtcNow, "Depósito 2", "REF2", TipoMovimientoEstadoCuenta.Credito, 100)));
    }

    [Fact]
    public void MovimientoEstadoCuenta_AgregarMatch_ActualizaEstadoYMontos()
    {
        var mov = new MovimientoEstadoCuenta("ID1", DateTime.UtcNow, "Test", "", TipoMovimientoEstadoCuenta.Debito, 100);
        mov.AgregarMatch(1, 40, TipoMatchConciliacion.Manual);
        Assert.Equal(EstadoMovimientoEstadoCuenta.Parcial, mov.Estado);
        Assert.Equal(40, mov.MontoConciliado);
        Assert.Equal(60, mov.MontoPendiente);
        mov.AgregarMatch(2, 60, TipoMatchConciliacion.Manual);
        Assert.Equal(EstadoMovimientoEstadoCuenta.Conciliado, mov.Estado);
        Assert.Equal(100, mov.MontoConciliado);
        Assert.Equal(0, mov.MontoPendiente);
    }

    [Fact]
    public void Completar_ConciliacionValida_CambiaEstadoACompletada()
    {
        var conciliacion = new ConciliacionBancaria(1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1000, 1100);
        conciliacion.MarcarComoEnProceso();
        var mov = new MovimientoEstadoCuenta("ID1", DateTime.UtcNow, "Credito", "", TipoMovimientoEstadoCuenta.Credito, 100);
        mov.AgregarMatch(1, 100, TipoMatchConciliacion.Manual);
        conciliacion.AgregarMovimiento(mov);
        conciliacion.Completar();
        Assert.Equal(EstadoConciliacionBancaria.Completada, conciliacion.Estado);
        Assert.Equal(0, conciliacion.Diferencia);
    }
}
