using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N16TransferenciaInventarioContractTests
{
    [Fact]
    public void RecepcionParcial_NoCierraTransferenciaHastaBalancearDespacho()
    {
        var transferencia = CrearEnTransito(cantidad: 6);
        var detalle = Assert.Single(transferencia.Detalles);

        detalle.RegistrarRecepcion(recibida: 3, faltante: 0, danada: 0, sobrante: 0);

        Assert.False(detalle.RecepcionCerrada);
        Assert.Throws<InvalidOperationException>(() =>
            transferencia.Recibir(9, DateTime.UtcNow));
        Assert.Equal(EstadoTransferenciaInventario.EnTransito, transferencia.Estado);

        detalle.RegistrarRecepcion(recibida: 5, faltante: 1, danada: 0, sobrante: 0);
        transferencia.Recibir(9, DateTime.UtcNow);

        Assert.True(detalle.RecepcionCerrada);
        Assert.Equal(EstadoTransferenciaInventario.Recibida, transferencia.Estado);
    }

    [Fact]
    public void RecepcionConSobrante_ConservaDiscrepanciaSinAlterarBalanceDespachado()
    {
        var transferencia = CrearEnTransito(cantidad: 4);
        var detalle = Assert.Single(transferencia.Detalles);

        detalle.RegistrarRecepcion(recibida: 4, faltante: 0, danada: 0, sobrante: 2);
        transferencia.Recibir(10, DateTime.UtcNow);

        Assert.True(detalle.RecepcionCerrada);
        Assert.Equal(4, detalle.CantidadRecibida);
        Assert.Equal(2, detalle.CantidadSobrante);
        Assert.Equal(EstadoTransferenciaInventario.Recibida, transferencia.Estado);
    }

    [Fact]
    public void SolicitarSinDetalles_FallaCerradoSinMutarAuditoria()
    {
        var transferencia = new TransferenciaInventario
        {
            Numero = "TRF-EMPTY",
            AlmacenOrigenId = 1,
            AlmacenDestinoId = 2
        };

        Assert.Throws<InvalidOperationException>(() =>
            transferencia.Solicitar(5, DateTime.UtcNow));

        Assert.Equal(EstadoTransferenciaInventario.Borrador, transferencia.Estado);
        Assert.Null(transferencia.SolicitadaPorUsuarioId);
        Assert.Null(transferencia.FechaSolicitud);
    }

    [Fact]
    public void ContratosDeEntrada_ExponenDimensionesFisicasYDiscrepancias()
    {
        var create = new CreateTransferenciaInventarioDto
        {
            AlmacenOrigenId = 11,
            AlmacenDestinoId = 22,
            Detalles =
            {
                new TransferenciaInventarioDetalleInputDto
                {
                    ProductoVarianteId = 31,
                    UbicacionOrigenId = 101,
                    UbicacionDestinoId = 202,
                    CantidadSolicitada = 7
                }
            }
        };
        var recepcion = new RecibirTransferenciaInventarioDetalleDto
        {
            DetalleId = 1,
            CantidadRecibida = 5,
            CantidadFaltante = 1,
            CantidadDanada = 1,
            CantidadSobrante = 2
        };

        Assert.Equal(11, create.AlmacenOrigenId);
        Assert.Equal(22, create.AlmacenDestinoId);
        var detalle = Assert.Single(create.Detalles);
        Assert.Equal(101, detalle.UbicacionOrigenId);
        Assert.Equal(202, detalle.UbicacionDestinoId);
        Assert.Equal(2, recepcion.CantidadSobrante);
        Assert.Equal(7, recepcion.CantidadRecibida + recepcion.CantidadFaltante + recepcion.CantidadDanada);
    }

    private static TransferenciaInventario CrearEnTransito(int cantidad)
    {
        var detalle = new TransferenciaInventarioDetalle
        {
            ProductoVarianteId = 77
        };
        detalle.EstablecerCantidadSolicitada(cantidad);

        var transferencia = new TransferenciaInventario
        {
            Numero = "TRF-PARCIAL",
            AlmacenOrigenId = 1,
            AlmacenDestinoId = 2,
            Detalles = new List<TransferenciaInventarioDetalle> { detalle }
        };

        transferencia.Solicitar(1, DateTime.UtcNow);
        detalle.AprobarCantidad(cantidad);
        transferencia.Aprobar(2, DateTime.UtcNow);
        detalle.RegistrarDespacho(cantidad);
        transferencia.MarcarEnTransito(3, DateTime.UtcNow);
        return transferencia;
    }
}
