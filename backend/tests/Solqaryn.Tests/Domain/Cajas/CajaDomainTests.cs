using Solqaryn.Domain.Entities.Cajas;
using Solqaryn.Domain.Enums.Cajas;
using Xunit;

namespace Solqaryn.Tests.Domain.Cajas;

public class CajaDomainTests
{
    [Fact]
    public void Caja_NoPermiteDosSesionesActivas()
    {
        var caja = new Caja("Principal");
        caja.Activar();
        caja.RegistrarSesionActiva(10);

        Assert.Throws<InvalidOperationException>(() => caja.RegistrarSesionActiva(11));
        Assert.Throws<InvalidOperationException>(() => caja.Desactivar());
    }

    [Fact]
    public void CajaSesion_RespetaAperturaOperacionesArqueoCierre()
    {
        var sesion = new CajaSesion(1, 7, 100m) { Id = 25 };
        Assert.Equal(EstadoCajaSesion.Apertura, sesion.Estado);

        sesion.IniciarOperaciones();
        sesion.RegistrarMovimiento(TipoMovimientoCaja.Ingreso, 50m, "Ingreso manual");
        sesion.RegistrarMovimiento(TipoMovimientoCaja.Retiro, 20m, "Retiro autorizado");
        sesion.RegistrarMovimiento(TipoMovimientoCaja.DepositoBanco, 10m, "Depósito banco");
        sesion.IniciarArqueo();

        Assert.Equal(120m, sesion.SaldoEsperado);
        var diferencia = sesion.Cerrar(115m, "Faltante verificado");

        Assert.Equal(EstadoCajaSesion.Cerrada, sesion.Estado);
        Assert.Equal(-5m, sesion.Diferencia);
        Assert.NotNull(diferencia);
        Assert.Equal(TipoMovimientoCaja.DiferenciaFaltante, diferencia!.Tipo);
        Assert.Equal(-5m, diferencia.ImpactoSaldo);
    }

    [Fact]
    public void CajaSesion_NoRegistraMovimientosAntesDePersistirse()
    {
        var sesion = new CajaSesion(1, 7, 0m);
        sesion.IniciarOperaciones();

        Assert.Throws<InvalidOperationException>(() =>
            sesion.RegistrarMovimiento(TipoMovimientoCaja.Ingreso, 10m, "Ingreso"));
    }

    [Fact]
    public void CajaSesion_RechazaDiferenciasSinMutarLaSesion()
    {
        var sesion = new CajaSesion(1, 7, 100m) { Id = 25 };
        sesion.IniciarOperaciones();

        var movimientosAntes = sesion.Movimientos.Count;
        var ingresosAntes = sesion.TotalIngresos;
        var retirosAntes = sesion.TotalRetiros;
        var depositosAntes = sesion.TotalDepositos;

        Assert.Throws<InvalidOperationException>(() =>
            sesion.RegistrarMovimiento(TipoMovimientoCaja.DiferenciaSobrante, 5m, "No permitido"));
        Assert.Throws<InvalidOperationException>(() =>
            sesion.RegistrarMovimiento(TipoMovimientoCaja.DiferenciaFaltante, 5m, "No permitido"));

        Assert.Equal(movimientosAntes, sesion.Movimientos.Count);
        Assert.Equal(ingresosAntes, sesion.TotalIngresos);
        Assert.Equal(retirosAntes, sesion.TotalRetiros);
        Assert.Equal(depositosAntes, sesion.TotalDepositos);
        Assert.Equal(EstadoCajaSesion.Operaciones, sesion.Estado);
    }

    [Fact]
    public void CajaMovimiento_ExponeSemanticaDeSignoSinMontosNegativos()
    {
        var ingreso = new CajaMovimiento(5, 7, TipoMovimientoCaja.Ingreso, 20m, "Ingreso");
        var retiro = new CajaMovimiento(5, 7, TipoMovimientoCaja.Retiro, 8m, "Retiro");
        var deposito = new CajaMovimiento(5, 7, TipoMovimientoCaja.DepositoBanco, 4m, "Depósito");

        Assert.Equal(20m, ingreso.ImpactoSaldo);
        Assert.Equal(-8m, retiro.ImpactoSaldo);
        Assert.Equal(-4m, deposito.ImpactoSaldo);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CajaMovimiento(5, 7, TipoMovimientoCaja.Ingreso, 0m, "Inválido"));
    }
}
