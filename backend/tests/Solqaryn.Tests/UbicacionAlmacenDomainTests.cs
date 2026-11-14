using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class UbicacionAlmacenDomainTests
{
    [Fact]
    public void TipoUbicacionAlmacen_MantieneClasificacionesEstables()
    {
        Assert.Equal(1, (int)TipoUbicacionAlmacen.Pasillo);
        Assert.Equal(2, (int)TipoUbicacionAlmacen.Estante);
        Assert.Equal(3, (int)TipoUbicacionAlmacen.Rack);
        Assert.Equal(4, (int)TipoUbicacionAlmacen.Seccion);
        Assert.Equal(5, (int)TipoUbicacionAlmacen.Bin);
        Assert.Equal(6, (int)TipoUbicacionAlmacen.Otra);
        Assert.Equal(6, Enum.GetValues<TipoUbicacionAlmacen>().Length);
    }

    [Fact]
    public void UbicacionAlmacen_Nueva_IniciaActivaYSinEliminacion()
    {
        var ubicacion = new UbicacionAlmacen();

        Assert.True(ubicacion.Activa);
        Assert.False(ubicacion.Eliminado);
        Assert.Null(ubicacion.FechaEliminacion);
        Assert.Null(ubicacion.EliminadoPorUsuarioId);
        Assert.Empty(ubicacion.Hijas);
    }

    [Fact]
    public void ContratosUbicacion_DerivanContextoYNoAdelantanStock()
    {
        foreach (var tipo in new[]
        {
            typeof(UbicacionAlmacen),
            typeof(UbicacionAlmacenDto),
            typeof(CreateUbicacionAlmacenDto),
            typeof(UpdateUbicacionAlmacenDto)
        })
        {
            Assert.Null(tipo.GetProperty("SucursalId"));
            Assert.Null(tipo.GetProperty("EmpresaId"));
            Assert.Null(tipo.GetProperty("Cantidad"));
            Assert.Null(tipo.GetProperty("Stock"));
            Assert.Null(tipo.GetProperty("StockFisico"));
            Assert.Null(tipo.GetProperty("StockReservado"));
            Assert.Null(tipo.GetProperty("StockDisponible"));
        }
    }

    [Fact]
    public void ContratosUbicacion_ConservanAlmacenYPadreOpcional()
    {
        var create = new CreateUbicacionAlmacenDto
        {
            AlmacenId = 10,
            UbicacionPadreId = 20
        };
        var update = new UpdateUbicacionAlmacenDto
        {
            AlmacenId = 11,
            UbicacionPadreId = null
        };
        var response = new UbicacionAlmacenDto
        {
            AlmacenId = 12,
            UbicacionPadreId = 21
        };

        Assert.Equal(10, create.AlmacenId);
        Assert.Equal(20, create.UbicacionPadreId);
        Assert.Equal(11, update.AlmacenId);
        Assert.Null(update.UbicacionPadreId);
        Assert.Equal(12, response.AlmacenId);
        Assert.Equal(21, response.UbicacionPadreId);
    }
}
