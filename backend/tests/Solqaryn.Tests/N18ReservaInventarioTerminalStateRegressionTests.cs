using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N18ReservaInventarioTerminalStateRegressionTests
{
    [Fact]
    public void Reserva_consumida_no_puede_liberarse_expirar_ni_cancelarse()
    {
        var fecha = new DateTime(2026, 8, 17, 6, 1, 0, DateTimeKind.Utc);
        var reserva = CrearReservaActiva(fecha);
        var detalle = Assert.Single(reserva.Detalles);
        detalle.RegistrarConsumo(detalle.CantidadReservada);
        reserva.Consumir(17, fecha.AddMinutes(1));

        Assert.Equal(EstadoReservaInventario.Consumida, reserva.Estado);
        Assert.Throws<InvalidOperationException>(() => reserva.Liberar(17, "no aplica", fecha.AddMinutes(2)));
        Assert.Throws<InvalidOperationException>(() => reserva.Expirar(17, fecha.AddHours(2)));
        Assert.Throws<InvalidOperationException>(() => reserva.Cancelar(17, "no aplica", fecha.AddMinutes(2)));
        Assert.Null(reserva.FechaLiberacion);
        Assert.Null(reserva.FechaExpiracionAplicada);
        Assert.Null(reserva.FechaCancelacion);
    }

    [Theory]
    [InlineData(EstadoReservaInventario.Liberada)]
    [InlineData(EstadoReservaInventario.Expirada)]
    [InlineData(EstadoReservaInventario.Cancelada)]
    public void Estado_terminal_no_puede_consumirse(EstadoReservaInventario terminal)
    {
        var fecha = new DateTime(2026, 8, 17, 6, 1, 0, DateTimeKind.Utc);
        var reserva = CrearReservaActiva(fecha);

        switch (terminal)
        {
            case EstadoReservaInventario.Liberada:
                reserva.Liberar(17, "liberada", fecha.AddMinutes(1));
                break;
            case EstadoReservaInventario.Expirada:
                reserva.Expirar(17, fecha.AddHours(1));
                break;
            case EstadoReservaInventario.Cancelada:
                reserva.Cancelar(17, "cancelada", fecha.AddMinutes(1));
                break;
        }

        var detalle = Assert.Single(reserva.Detalles);
        detalle.RegistrarConsumo(detalle.CantidadReservada);

        Assert.Throws<InvalidOperationException>(() => reserva.Consumir(17, fecha.AddHours(2)));
        Assert.Equal(terminal, reserva.Estado);
        Assert.Null(reserva.FechaConsumo);
        Assert.Null(reserva.ConsumidaPorUsuarioId);
    }

    [Fact]
    public void Cancelar_borrador_no_requiere_activar_y_no_materializa_actor_de_activacion()
    {
        var fecha = new DateTime(2026, 8, 17, 6, 1, 0, DateTimeKind.Utc);
        var reserva = CrearReservaValida();

        reserva.Cancelar(33, "cliente desistió", fecha);

        Assert.Equal(EstadoReservaInventario.Cancelada, reserva.Estado);
        Assert.Equal(33, reserva.CanceladaPorUsuarioId);
        Assert.Equal(fecha, reserva.FechaCancelacion);
        Assert.Equal("cliente desistió", reserva.MotivoCancelacion);
        Assert.Null(reserva.ActivadaPorUsuarioId);
        Assert.Null(reserva.FechaActivacion);
    }

    private static ReservaInventario CrearReservaActiva(DateTime fecha)
    {
        var reserva = CrearReservaValida();
        reserva.FechaExpiracion = fecha.AddMinutes(30);
        reserva.Activar(17, fecha);
        return reserva;
    }

    private static ReservaInventario CrearReservaValida()
    {
        var detalle = new ReservaInventarioDetalle
        {
            ProductoVarianteId = 11,
            AlmacenId = 5
        };
        detalle.EstablecerCantidadReservada(3);

        return new ReservaInventario
        {
            Numero = "RES-000002",
            VentaId = 31,
            Detalles = new List<ReservaInventarioDetalle> { detalle }
        };
    }
}
