using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N21SolicitudCompraDomainRegressionTests
{
    [Fact]
    public void Solicitar_sin_detalles_falla_cerrado_y_no_muta_estado()
    {
        var solicitud = new SolicitudCompra
        {
            NumeroSolicitud = "SC-000001"
        };

        var error = Assert.Throws<InvalidOperationException>(
            () => solicitud.Solicitar(7, "Comprador", DateTime.UtcNow));

        Assert.Contains("detalle", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EstadoSolicitudCompra.Borrador, solicitud.Estado);
        Assert.Null(solicitud.FechaSolicitudUtc);
        Assert.Null(solicitud.SolicitadaPorUsuarioId);
    }

    [Fact]
    public void Solicitar_valida_documento_y_normaliza_snapshot_del_usuario()
    {
        var solicitud = CrearBorradorValido();
        var fecha = new DateTime(2026, 8, 18, 18, 45, 0, DateTimeKind.Utc);

        solicitud.Solicitar(41, "  Comprador QA  ", fecha);

        Assert.Equal(EstadoSolicitudCompra.Solicitada, solicitud.Estado);
        Assert.Equal(fecha, solicitud.FechaSolicitudUtc);
        Assert.Equal(41, solicitud.SolicitadaPorUsuarioId);
        Assert.Equal("Comprador QA", solicitud.SolicitadaPorNombreSnapshot);
        Assert.False(solicitud.EsEditable);
        Assert.False(solicitud.EsTerminal);
    }

    [Fact]
    public void Rechazar_sin_motivo_no_muta_una_solicitud_enviada()
    {
        var solicitud = CrearBorradorValido();
        solicitud.Solicitar(41, "Comprador", DateTime.UtcNow);

        Assert.Throws<ArgumentException>(
            () => solicitud.Rechazar(52, "Aprobador", "   ", DateTime.UtcNow));

        Assert.Equal(EstadoSolicitudCompra.Solicitada, solicitud.Estado);
        Assert.Null(solicitud.FechaDecisionUtc);
        Assert.Null(solicitud.DecididaPorUsuarioId);
        Assert.Null(solicitud.MotivoRechazo);
    }

    [Fact]
    public void Aprobar_deja_estado_terminal_y_bloquea_edicion_y_segunda_decision()
    {
        var solicitud = CrearBorradorValido();
        solicitud.Solicitar(41, "Comprador", DateTime.UtcNow);
        var fechaDecision = new DateTime(2026, 8, 18, 18, 50, 0, DateTimeKind.Utc);

        solicitud.Aprobar(52, "  Aprobador QA  ", fechaDecision);

        Assert.Equal(EstadoSolicitudCompra.Aprobada, solicitud.Estado);
        Assert.True(solicitud.EsTerminal);
        Assert.False(solicitud.EsEditable);
        Assert.Equal(fechaDecision, solicitud.FechaDecisionUtc);
        Assert.Equal(52, solicitud.DecididaPorUsuarioId);
        Assert.Equal("Aprobador QA", solicitud.DecididaPorNombreSnapshot);
        Assert.Null(solicitud.MotivoRechazo);
        Assert.Throws<InvalidOperationException>(() => solicitud.AsegurarEditable());
        Assert.Throws<InvalidOperationException>(
            () => solicitud.Rechazar(52, "Aprobador QA", "cambio tardío", DateTime.UtcNow));
    }

    [Fact]
    public void Rechazar_normaliza_motivo_y_deja_estado_terminal()
    {
        var solicitud = CrearBorradorValido();
        solicitud.Solicitar(41, "Comprador", DateTime.UtcNow);

        solicitud.Rechazar(52, "Aprobador", "  Sin presupuesto  ", DateTime.UtcNow);

        Assert.Equal(EstadoSolicitudCompra.Rechazada, solicitud.Estado);
        Assert.True(solicitud.EsTerminal);
        Assert.Equal("Sin presupuesto", solicitud.MotivoRechazo);
        Assert.Throws<InvalidOperationException>(
            () => solicitud.Aprobar(52, "Aprobador", DateTime.UtcNow));
    }

    private static SolicitudCompra CrearBorradorValido()
    {
        var detalle = new SolicitudCompraDetalle
        {
            ProductoId = 3
        };
        detalle.EstablecerCantidad(2);
        detalle.EstablecerCostoEstimado(125.50m);

        return new SolicitudCompra
        {
            NumeroSolicitud = "SC-000001",
            ProveedorId = 9,
            Detalles = new List<SolicitudCompraDetalle> { detalle }
        };
    }
}
