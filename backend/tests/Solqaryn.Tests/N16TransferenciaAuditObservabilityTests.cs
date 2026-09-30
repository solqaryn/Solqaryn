using Solqaryn.Application.Common;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaAuditObservabilityTests
{
    [Fact]
    public void Lifecycle_PreservaActorYTimestampPorTransicion()
    {
        var transferencia = CrearBorradorValido();
        var solicitud = new DateTime(2026, 8, 16, 10, 0, 0, DateTimeKind.Utc);
        var aprobacion = solicitud.AddMinutes(1);
        var despacho = aprobacion.AddMinutes(1);

        transferencia.Solicitar(101, solicitud);
        transferencia.Detalles.Single().AprobarCantidad(3);
        transferencia.Aprobar(202, aprobacion);
        transferencia.Detalles.Single().RegistrarDespacho(3);
        transferencia.MarcarEnTransito(303, despacho);

        Assert.Equal(101, transferencia.SolicitadaPorUsuarioId);
        Assert.Equal(solicitud, transferencia.FechaSolicitud);
        Assert.Equal(202, transferencia.AprobadaPorUsuarioId);
        Assert.Equal(aprobacion, transferencia.FechaAprobacion);
        Assert.Equal(303, transferencia.DespachadaPorUsuarioId);
        Assert.Equal(despacho, transferencia.FechaDespacho);
    }

    [Fact]
    public void Recepcion_PreservaActorYTimestampSinSobrescribirDespacho()
    {
        var transferencia = CrearBorradorValido();
        var solicitud = new DateTime(2026, 8, 16, 10, 0, 0, DateTimeKind.Utc);
        var aprobacion = solicitud.AddMinutes(1);
        var despacho = aprobacion.AddMinutes(1);
        var recepcion = despacho.AddMinutes(5);

        transferencia.Solicitar(101, solicitud);
        transferencia.Detalles.Single().AprobarCantidad(3);
        transferencia.Aprobar(202, aprobacion);
        transferencia.Detalles.Single().RegistrarDespacho(3);
        transferencia.MarcarEnTransito(303, despacho);
        transferencia.Detalles.Single().RegistrarRecepcion(3, 0, 0, 0);
        transferencia.Recibir(404, recepcion);

        Assert.Equal(303, transferencia.DespachadaPorUsuarioId);
        Assert.Equal(despacho, transferencia.FechaDespacho);
        Assert.Equal(404, transferencia.RecibidaPorUsuarioId);
        Assert.Equal(recepcion, transferencia.FechaRecepcion);
        Assert.Equal(EstadoTransferenciaInventario.Recibida, transferencia.Estado);
    }

    [Fact]
    public void Cancelacion_PreservaActorTimestampYMotivoNormalizado()
    {
        var transferencia = CrearBorradorValido();
        var fecha = new DateTime(2026, 8, 16, 10, 5, 0, DateTimeKind.Utc);

        transferencia.Cancelar(404, "  daño detectado en origen  ", fecha);

        Assert.Equal(EstadoTransferenciaInventario.Cancelada, transferencia.Estado);
        Assert.Equal(404, transferencia.CanceladaPorUsuarioId);
        Assert.Equal(fecha, transferencia.FechaCancelacion);
        Assert.Equal("daño detectado en origen", transferencia.MotivoCancelacion);
    }

    [Fact]
    public void CorrelationIds_SonDeterministasYSeparanCadaOperacionFisica()
    {
        const int transferenciaId = 77;

        var despacho1 = KardexCorrelationId.TransferenciaDespachar(transferenciaId);
        var despacho2 = KardexCorrelationId.TransferenciaDespachar(transferenciaId);
        var recepcion = KardexCorrelationId.TransferenciaRecibir(transferenciaId);
        var cancelacion = KardexCorrelationId.TransferenciaCancelar(transferenciaId);

        Assert.Equal(despacho1, despacho2);
        Assert.NotEqual(despacho1, recepcion);
        Assert.NotEqual(despacho1, cancelacion);
        Assert.NotEqual(recepcion, cancelacion);
        Assert.Contains(transferenciaId.ToString(), despacho1, StringComparison.Ordinal);
        Assert.Contains(transferenciaId.ToString(), recepcion, StringComparison.Ordinal);
        Assert.Contains(transferenciaId.ToString(), cancelacion, StringComparison.Ordinal);
    }

    private static TransferenciaInventario CrearBorradorValido()
    {
        var detalle = new TransferenciaInventarioDetalle
        {
            Id = 1,
            ProductoVarianteId = 9
        };
        detalle.EstablecerCantidadSolicitada(3);

        return new TransferenciaInventario
        {
            Id = 77,
            Numero = "TRF-TEST-77",
            AlmacenOrigenId = 1,
            AlmacenDestinoId = 2,
            Detalles = new List<TransferenciaInventarioDetalle> { detalle }
        };
    }
}
