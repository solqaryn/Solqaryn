using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N21SolicitudCompraDomainTests
{
    [Fact]
    public void Solicitar_SinDetalles_FallaCerradoYSinMutarEstado()
    {
        var solicitud = new SolicitudCompra { NumeroSolicitud = "SC-0001" };

        Assert.Throws<InvalidOperationException>(() => solicitud.Solicitar(7, "Javier", DateTime.UtcNow));

        Assert.Equal(EstadoSolicitudCompra.Borrador, solicitud.Estado);
        Assert.Null(solicitud.FechaSolicitudUtc);
        Assert.Null(solicitud.SolicitadaPorUsuarioId);
    }

    [Fact]
    public void Lifecycle_Aprobacion_RespetaBorradorSolicitadaAprobada()
    {
        var solicitud = CrearBorrador();
        var fechaSolicitud = DateTime.UtcNow;
        var fechaDecision = fechaSolicitud.AddMinutes(1);

        solicitud.Solicitar(7, " Javier ", fechaSolicitud);
        Assert.Equal(EstadoSolicitudCompra.Solicitada, solicitud.Estado);
        Assert.Equal("Javier", solicitud.SolicitadaPorNombreSnapshot);

        solicitud.Aprobar(8, "Ana", fechaDecision);

        Assert.Equal(EstadoSolicitudCompra.Aprobada, solicitud.Estado);
        Assert.Equal(fechaDecision, solicitud.FechaDecisionUtc);
        Assert.Equal(8, solicitud.DecididaPorUsuarioId);
        Assert.Null(solicitud.MotivoRechazo);
        Assert.True(solicitud.EsTerminal);
    }

    [Fact]
    public void Rechazar_SinMotivo_FallaCerradoYSinMutarDecision()
    {
        var solicitud = CrearBorrador();
        solicitud.Solicitar(7, "Javier", DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => solicitud.Rechazar(8, "Ana", "   ", DateTime.UtcNow));

        Assert.Equal(EstadoSolicitudCompra.Solicitada, solicitud.Estado);
        Assert.Null(solicitud.FechaDecisionUtc);
        Assert.Null(solicitud.DecididaPorUsuarioId);
        Assert.Null(solicitud.MotivoRechazo);
    }

    [Fact]
    public void Rechazar_ConMotivo_MaterializaDecisionTerminal()
    {
        var solicitud = CrearBorrador();
        solicitud.Solicitar(7, "Javier", DateTime.UtcNow);

        solicitud.Rechazar(8, " Ana ", " Presupuesto insuficiente ", DateTime.UtcNow);

        Assert.Equal(EstadoSolicitudCompra.Rechazada, solicitud.Estado);
        Assert.Equal("Ana", solicitud.DecididaPorNombreSnapshot);
        Assert.Equal("Presupuesto insuficiente", solicitud.MotivoRechazo);
        Assert.True(solicitud.EsTerminal);
    }

    [Fact]
    public void Aprobar_DesdeBorrador_FallaCerrado()
    {
        var solicitud = CrearBorrador();

        Assert.Throws<InvalidOperationException>(() => solicitud.Aprobar(8, "Ana", DateTime.UtcNow));

        Assert.Equal(EstadoSolicitudCompra.Borrador, solicitud.Estado);
        Assert.Null(solicitud.FechaDecisionUtc);
    }

    [Fact]
    public void Detalle_CantidadOCostoInvalidos_NoMutanValorValido()
    {
        var detalle = new SolicitudCompraDetalle { ProductoId = 10 };
        detalle.EstablecerCantidad(5);
        detalle.EstablecerCostoEstimado(125.50m);

        Assert.Throws<ArgumentOutOfRangeException>(() => detalle.EstablecerCantidad(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => detalle.EstablecerCostoEstimado(-1));

        Assert.Equal(5, detalle.CantidadSolicitada);
        Assert.Equal(125.50m, detalle.CostoEstimadoUnitario);
    }

    private static SolicitudCompra CrearBorrador()
    {
        var detalle = new SolicitudCompraDetalle
        {
            ProductoId = 10,
            ProductoVarianteId = 20
        };
        detalle.EstablecerCantidad(3);
        detalle.EstablecerCostoEstimado(250m);

        return new SolicitudCompra
        {
            NumeroSolicitud = "SC-0001",
            ProveedorId = 4,
            Detalles = new List<SolicitudCompraDetalle> { detalle }
        };
    }
}
