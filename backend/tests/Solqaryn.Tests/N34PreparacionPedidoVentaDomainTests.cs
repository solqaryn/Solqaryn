using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.ValueObjects;
using Xunit;

namespace Solqaryn.Tests;

public class N34PreparacionPedidoVentaDomainTests
{
    private static readonly DateTime FechaBaseUtc = new(2026, 8, 24, 19, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Crear_ExigePedidoConfirmadoReservaActivaYConsistenciaExacta()
    {
        var (pedido, reserva) = CrearPedidoYReservaActiva(3);

        var preparacion = PreparacionPedidoVenta.Crear(pedido, reserva);

        Assert.Equal(pedido.Id, preparacion.PedidoVentaId);
        Assert.Equal(reserva.Id, preparacion.ReservaInventarioId);
        Assert.Equal(EstadoPreparacionPedidoVenta.PendientePicking, preparacion.Estado);
        Assert.Single(preparacion.Detalles);
        Assert.Equal(3, preparacion.Detalles.Single().CantidadPreparar);
    }

    [Fact]
    public void Crear_RechazaReservaDeOtroPedidoOCoberturaDistinta()
    {
        var (pedido, reserva) = CrearPedidoYReservaActiva(3);
        reserva.PedidoVentaId = pedido.Id + 1;
        Assert.Throws<InvalidOperationException>(() => PreparacionPedidoVenta.Crear(pedido, reserva));

        var (pedido2, reserva2) = CrearPedidoYReservaActiva(2);
        reserva2.Detalles.Single().EstablecerCantidadReservada(1);
        Assert.Throws<InvalidOperationException>(() => PreparacionPedidoVenta.Crear(pedido2, reserva2));
    }

    [Fact]
    public void Lifecycle_EsOrdenadoFailClosed_YCancelacionSoloAntesDeDespacho()
    {
        var (pedido, reserva) = CrearPedidoYReservaActiva(2);
        var preparacion = PreparacionPedidoVenta.Crear(pedido, reserva);

        Assert.Throws<InvalidOperationException>(() => preparacion.CompletarPacking(1, FechaBaseUtc));

        preparacion.CompletarPicking(1, FechaBaseUtc);
        preparacion.CompletarPacking(1, FechaBaseUtc.AddMinutes(1));
        preparacion.MarcarDespachado(1, FechaBaseUtc.AddMinutes(2));

        Assert.Equal(EstadoPreparacionPedidoVenta.Despachado, preparacion.Estado);
        Assert.Throws<InvalidOperationException>(() => preparacion.Cancelar(1, "ya despachado", FechaBaseUtc.AddMinutes(3)));

        preparacion.MarcarEntregado(1, FechaBaseUtc.AddMinutes(4));
        Assert.Equal(EstadoPreparacionPedidoVenta.Entregado, preparacion.Estado);
        Assert.Throws<InvalidOperationException>(() => preparacion.MarcarEntregado(1, FechaBaseUtc.AddMinutes(5)));
    }

    [Fact]
    public void Cancelar_AntesDeDespacho_ExigeMotivoUsuarioYUtc()
    {
        var (pedido, reserva) = CrearPedidoYReservaActiva(1);
        var preparacion = PreparacionPedidoVenta.Crear(pedido, reserva);

        Assert.Throws<ArgumentOutOfRangeException>(() => preparacion.Cancelar(0, "motivo", FechaBaseUtc));
        Assert.Throws<ArgumentException>(() => preparacion.Cancelar(1, " ", FechaBaseUtc));
        Assert.Throws<ArgumentException>(() => preparacion.Cancelar(1, "motivo", DateTime.SpecifyKind(FechaBaseUtc, DateTimeKind.Local)));

        preparacion.Cancelar(1, "Cliente solicita detener preparación", FechaBaseUtc);
        Assert.Equal(EstadoPreparacionPedidoVenta.Cancelado, preparacion.Estado);
        Assert.Equal("Cliente solicita detener preparación", preparacion.MotivoCancelacion);
    }

    private static (PedidoVenta Pedido, ReservaInventario Reserva) CrearPedidoYReservaActiva(int cantidad)
    {
        var cotizacion = new Cotizacion
        {
            Id = 900,
            ClienteId = 7,
            ClienteNombreSnapshot = "Cliente QA"
        };

        var detalleCotizacion = new CotizacionDetalle
        {
            ProductoId = 100,
            ProductoVarianteId = 11,
            ProductoSkuSnapshot = "SKU-11",
            ProductoNombreSnapshot = "Producto"
        };
        detalleCotizacion.EstablecerValores(cantidad, 10m);
        cotizacion.Detalles.Add(detalleCotizacion);
        cotizacion.Enviar(1, FechaBaseUtc.AddMinutes(-4));
        cotizacion.Aceptar(2, FechaBaseUtc.AddMinutes(-3));

        var pedido = PedidoVenta.CrearDesdeCotizacion(cotizacion);
        pedido.Id = 1000;

        var contrato = pedido.PrepararReservaAutomatica(new[]
        {
            AsignacionReservaAutomatica.Crear(11, 200, 300, cantidad)
        });

        var reserva = new ReservaInventario
        {
            Id = 500,
            Numero = "RES-500",
            PedidoVentaId = pedido.Id
        };

        foreach (var asignacion in contrato.Asignaciones)
        {
            var detalleReserva = new ReservaInventarioDetalle
            {
                ProductoVarianteId = asignacion.ProductoVarianteId,
                AlmacenId = asignacion.AlmacenId,
                UbicacionAlmacenId = asignacion.UbicacionAlmacenId
            };
            detalleReserva.EstablecerCantidadReservada(asignacion.Cantidad);
            reserva.Detalles.Add(detalleReserva);
        }

        reserva.Activar(3, FechaBaseUtc.AddMinutes(-2));
        pedido.Confirmar(3, "qa", FechaBaseUtc.AddMinutes(-1));
        return (pedido, reserva);
    }
}
