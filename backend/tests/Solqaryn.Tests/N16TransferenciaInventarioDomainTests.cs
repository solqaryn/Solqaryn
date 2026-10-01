using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N16TransferenciaInventarioDomainTests
{
    [Fact]
    public void Solicitar_ConOrigenYDestinoIguales_FallaCerrado()
    {
        var transferencia = CrearBorrador();
        transferencia.AlmacenDestinoId = transferencia.AlmacenOrigenId;

        var error = Assert.Throws<InvalidOperationException>(() =>
            transferencia.Solicitar(7, DateTime.UtcNow));

        Assert.Contains("distintos", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EstadoTransferenciaInventario.Borrador, transferencia.Estado);
    }

    [Fact]
    public void Lifecycle_Completo_PreservaTransicionesEsperadas()
    {
        var transferencia = CrearBorrador();
        var detalle = Assert.Single(transferencia.Detalles);

        transferencia.Solicitar(7, DateTime.UtcNow);
        Assert.Equal(EstadoTransferenciaInventario.Solicitada, transferencia.Estado);

        detalle.AprobarCantidad(4);
        transferencia.Aprobar(8, DateTime.UtcNow);
        Assert.Equal(EstadoTransferenciaInventario.Aprobada, transferencia.Estado);

        detalle.RegistrarDespacho(4);
        transferencia.MarcarEnTransito(9, DateTime.UtcNow);
        Assert.Equal(EstadoTransferenciaInventario.EnTransito, transferencia.Estado);

        detalle.RegistrarRecepcion(recibida: 3, faltante: 1, danada: 0, sobrante: 0);
        transferencia.Recibir(10, DateTime.UtcNow);

        Assert.Equal(EstadoTransferenciaInventario.Recibida, transferencia.Estado);
        Assert.True(detalle.RecepcionCerrada);
    }

    [Fact]
    public void Aprobar_SinCantidadAprobada_FallaCerradoYSinMutarAuditoria()
    {
        var transferencia = CrearBorrador();
        transferencia.Solicitar(1, DateTime.UtcNow);

        var fechaAntes = transferencia.FechaAprobacion;
        var usuarioAntes = transferencia.AprobadaPorUsuarioId;

        Assert.Throws<InvalidOperationException>(() =>
            transferencia.Aprobar(2, DateTime.UtcNow));

        Assert.Equal(EstadoTransferenciaInventario.Solicitada, transferencia.Estado);
        Assert.Equal(fechaAntes, transferencia.FechaAprobacion);
        Assert.Equal(usuarioAntes, transferencia.AprobadaPorUsuarioId);
    }

    [Fact]
    public void Despachar_SinCantidadDespachada_FallaCerradoYSinMutarAuditoria()
    {
        var transferencia = CrearBorrador();
        var detalle = Assert.Single(transferencia.Detalles);
        transferencia.Solicitar(1, DateTime.UtcNow);
        detalle.AprobarCantidad(4);
        transferencia.Aprobar(2, DateTime.UtcNow);

        var fechaAntes = transferencia.FechaDespacho;
        var usuarioAntes = transferencia.DespachadaPorUsuarioId;

        Assert.Throws<InvalidOperationException>(() =>
            transferencia.MarcarEnTransito(3, DateTime.UtcNow));

        Assert.Equal(EstadoTransferenciaInventario.Aprobada, transferencia.Estado);
        Assert.Equal(fechaAntes, transferencia.FechaDespacho);
        Assert.Equal(usuarioAntes, transferencia.DespachadaPorUsuarioId);
    }

    [Fact]
    public void RegistrarRecepcion_CuandoBalanceSuperaDespacho_NoMutaEstado()
    {
        var detalle = new TransferenciaInventarioDetalle();
        detalle.EstablecerCantidadSolicitada(5);
        detalle.AprobarCantidad(5);
        detalle.RegistrarDespacho(5);

        Assert.Throws<InvalidOperationException>(() =>
            detalle.RegistrarRecepcion(recibida: 4, faltante: 1, danada: 1, sobrante: 0));

        Assert.Equal(0, detalle.CantidadRecibida);
        Assert.Equal(0, detalle.CantidadFaltante);
        Assert.Equal(0, detalle.CantidadDanada);
        Assert.Equal(0, detalle.CantidadSobrante);
    }

    [Fact]
    public void Recibir_ConDetallePendiente_FallaCerradoYSinMutarAuditoria()
    {
        var transferencia = CrearBorrador();
        var detalle = Assert.Single(transferencia.Detalles);
        transferencia.Solicitar(1, DateTime.UtcNow);
        detalle.AprobarCantidad(4);
        transferencia.Aprobar(2, DateTime.UtcNow);
        detalle.RegistrarDespacho(4);
        transferencia.MarcarEnTransito(3, DateTime.UtcNow);

        var fechaAntes = transferencia.FechaRecepcion;
        var usuarioAntes = transferencia.RecibidaPorUsuarioId;

        Assert.Throws<InvalidOperationException>(() =>
            transferencia.Recibir(4, DateTime.UtcNow));

        Assert.Equal(EstadoTransferenciaInventario.EnTransito, transferencia.Estado);
        Assert.Equal(fechaAntes, transferencia.FechaRecepcion);
        Assert.Equal(usuarioAntes, transferencia.RecibidaPorUsuarioId);
        Assert.False(detalle.RecepcionCerrada);
    }

    [Fact]
    public void Cancelar_TransferenciaRecibida_FallaCerradoYSinMutarAuditoria()
    {
        var transferencia = CrearBorrador();
        var detalle = Assert.Single(transferencia.Detalles);
        transferencia.Solicitar(1, DateTime.UtcNow);
        detalle.AprobarCantidad(4);
        transferencia.Aprobar(2, DateTime.UtcNow);
        detalle.RegistrarDespacho(4);
        transferencia.MarcarEnTransito(3, DateTime.UtcNow);
        detalle.RegistrarRecepcion(4, 0, 0, 0);
        transferencia.Recibir(4, DateTime.UtcNow);

        var fechaAntes = transferencia.FechaCancelacion;
        var usuarioAntes = transferencia.CanceladaPorUsuarioId;
        var motivoAntes = transferencia.MotivoCancelacion;

        Assert.Throws<InvalidOperationException>(() =>
            transferencia.Cancelar(5, "no aplica", DateTime.UtcNow));

        Assert.Equal(EstadoTransferenciaInventario.Recibida, transferencia.Estado);
        Assert.Equal(fechaAntes, transferencia.FechaCancelacion);
        Assert.Equal(usuarioAntes, transferencia.CanceladaPorUsuarioId);
        Assert.Equal(motivoAntes, transferencia.MotivoCancelacion);
    }

    private static TransferenciaInventario CrearBorrador()
    {
        var detalle = new TransferenciaInventarioDetalle
        {
            ProductoVarianteId = 50
        };
        detalle.EstablecerCantidadSolicitada(4);

        return new TransferenciaInventario
        {
            Numero = "TRF-0001",
            AlmacenOrigenId = 1,
            AlmacenDestinoId = 2,
            Detalles = new List<TransferenciaInventarioDetalle> { detalle }
        };
    }
}
