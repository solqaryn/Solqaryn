using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N18ReservaInventarioDomainTests
{
    [Fact]
    public void Reserva_nueva_inicia_en_borrador()
    {
        var reserva = new ReservaInventario();

        Assert.Equal(EstadoReservaInventario.Borrador, reserva.Estado);
    }

    [Fact]
    public void Activar_requiere_detalle_fisico_valido_y_materializa_actor()
    {
        var fecha = new DateTime(2026, 8, 17, 6, 0, 0, DateTimeKind.Utc);
        var reserva = CrearReservaValida();

        reserva.Activar(17, fecha);

        Assert.Equal(EstadoReservaInventario.Activa, reserva.Estado);
        Assert.Equal(17, reserva.ActivadaPorUsuarioId);
        Assert.Equal(fecha, reserva.FechaActivacion);
    }

    [Fact]
    public void Activar_falla_cerrado_si_repite_clave_fisica()
    {
        var reserva = CrearReservaValida();
        reserva.Detalles.Add(CrearDetalle(11, 5, null, 1));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            reserva.Activar(17, DateTime.UtcNow));

        Assert.Contains("misma clave física", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EstadoReservaInventario.Borrador, reserva.Estado);
        Assert.Null(reserva.FechaActivacion);
    }

    [Fact]
    public void Activar_rechaza_reserva_ya_expirada_sin_mutacion_parcial()
    {
        var fecha = new DateTime(2026, 8, 17, 6, 0, 0, DateTimeKind.Utc);
        var reserva = CrearReservaValida();
        reserva.FechaExpiracion = fecha.AddSeconds(-1);

        Assert.Throws<InvalidOperationException>(() => reserva.Activar(17, fecha));

        Assert.Equal(EstadoReservaInventario.Borrador, reserva.Estado);
        Assert.Null(reserva.ActivadaPorUsuarioId);
        Assert.Null(reserva.FechaActivacion);
    }

    [Fact]
    public void Consumir_exige_consumo_completo_de_todas_las_lineas()
    {
        var reserva = CrearReservaValida();
        reserva.Activar(17, DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => reserva.Consumir(17, DateTime.UtcNow));
        Assert.Equal(EstadoReservaInventario.Activa, reserva.Estado);

        var detalle = Assert.Single(reserva.Detalles);
        detalle.RegistrarConsumo(detalle.CantidadReservada);
        reserva.Consumir(17, DateTime.UtcNow);

        Assert.Equal(EstadoReservaInventario.Consumida, reserva.Estado);
    }

    [Fact]
    public void Linea_no_permite_doble_consumo_ni_consumo_parcial()
    {
        var detalle = CrearDetalle(11, 5, 7, 3);

        Assert.Throws<InvalidOperationException>(() => detalle.RegistrarConsumo(2));
        Assert.Equal(0, detalle.CantidadConsumida);

        detalle.RegistrarConsumo(3);
        Assert.True(detalle.EstaConsumida);
        Assert.Throws<InvalidOperationException>(() => detalle.RegistrarConsumo(3));
    }

    [Fact]
    public void Liberar_es_terminal_y_exige_motivo()
    {
        var reserva = CrearReservaValida();
        reserva.Activar(17, DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => reserva.Liberar(17, " ", DateTime.UtcNow));
        Assert.Equal(EstadoReservaInventario.Activa, reserva.Estado);

        reserva.Liberar(17, "Pedido cancelado", DateTime.UtcNow);

        Assert.Equal(EstadoReservaInventario.Liberada, reserva.Estado);
        Assert.Equal("Pedido cancelado", reserva.MotivoLiberacion);
        Assert.Throws<InvalidOperationException>(() => reserva.Liberar(17, "otra vez", DateTime.UtcNow));
    }

    [Fact]
    public void Expirar_requiere_fecha_vencida_y_no_puede_aplicarse_dos_veces()
    {
        var fecha = new DateTime(2026, 8, 17, 6, 0, 0, DateTimeKind.Utc);
        var reserva = CrearReservaValida();
        reserva.FechaExpiracion = fecha.AddMinutes(5);
        reserva.Activar(17, fecha);

        Assert.Throws<InvalidOperationException>(() => reserva.Expirar(99, fecha.AddMinutes(4)));
        Assert.Equal(EstadoReservaInventario.Activa, reserva.Estado);

        reserva.Expirar(99, fecha.AddMinutes(5));
        Assert.Equal(EstadoReservaInventario.Expirada, reserva.Estado);
        Assert.Throws<InvalidOperationException>(() => reserva.Expirar(99, fecha.AddMinutes(6)));
    }

    [Theory]
    [InlineData(0, 5, null)]
    [InlineData(11, 0, null)]
    [InlineData(11, 5, 0)]
    public void Clave_fisica_invalida_falla_cerrado(int varianteId, int almacenId, int? ubicacionId)
    {
        var detalle = CrearDetalleSinValidar(varianteId, almacenId, ubicacionId, 1);

        Assert.Throws<InvalidOperationException>(() => detalle.ValidarClaveFisica());
    }

    private static ReservaInventario CrearReservaValida()
    {
        return new ReservaInventario
        {
            Numero = "RES-000001",
            VentaId = 31,
            Detalles = new List<ReservaInventarioDetalle>
            {
                CrearDetalle(11, 5, null, 3)
            }
        };
    }

    private static ReservaInventarioDetalle CrearDetalle(
        int varianteId,
        int almacenId,
        int? ubicacionId,
        int cantidad)
    {
        var detalle = CrearDetalleSinValidar(varianteId, almacenId, ubicacionId, cantidad);
        detalle.ValidarClaveFisica();
        return detalle;
    }

    private static ReservaInventarioDetalle CrearDetalleSinValidar(
        int varianteId,
        int almacenId,
        int? ubicacionId,
        int cantidad)
    {
        var detalle = new ReservaInventarioDetalle
        {
            ProductoVarianteId = varianteId,
            AlmacenId = almacenId,
            UbicacionAlmacenId = ubicacionId
        };
        detalle.EstablecerCantidadReservada(cantidad);
        return detalle;
    }
}
