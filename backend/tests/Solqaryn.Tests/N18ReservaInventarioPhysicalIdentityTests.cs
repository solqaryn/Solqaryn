using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N18ReservaInventarioPhysicalIdentityTests
{
    [Fact]
    public void Misma_variante_en_ubicaciones_distintas_es_valida()
    {
        var reserva = new ReservaInventario
        {
            Numero = "RES-000003",
            Detalles = new List<ReservaInventarioDetalle>
            {
                CrearDetalle(11, 5, 7, 2),
                CrearDetalle(11, 5, 8, 3)
            }
        };

        reserva.Activar(17, new DateTime(2026, 8, 17, 6, 3, 0, DateTimeKind.Utc));

        Assert.Equal(EstadoReservaInventario.Activa, reserva.Estado);
        Assert.Equal(2, reserva.Detalles.Count);
    }

    [Fact]
    public void Misma_variante_en_almacenes_distintos_es_valida()
    {
        var reserva = new ReservaInventario
        {
            Numero = "RES-000004",
            Detalles = new List<ReservaInventarioDetalle>
            {
                CrearDetalle(11, 5, null, 2),
                CrearDetalle(11, 6, null, 3)
            }
        };

        reserva.Activar(17, new DateTime(2026, 8, 17, 6, 3, 0, DateTimeKind.Utc));

        Assert.Equal(EstadoReservaInventario.Activa, reserva.Estado);
    }

    [Fact]
    public void Raiz_y_ubicacion_especifica_no_colisionan_como_misma_existencia()
    {
        var reserva = new ReservaInventario
        {
            Numero = "RES-000005",
            Detalles = new List<ReservaInventarioDetalle>
            {
                CrearDetalle(11, 5, null, 1),
                CrearDetalle(11, 5, 7, 1)
            }
        };

        reserva.Activar(17, new DateTime(2026, 8, 17, 6, 3, 0, DateTimeKind.Utc));

        Assert.Equal(EstadoReservaInventario.Activa, reserva.Estado);
    }

    private static ReservaInventarioDetalle CrearDetalle(int varianteId, int almacenId, int? ubicacionId, int cantidad)
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
