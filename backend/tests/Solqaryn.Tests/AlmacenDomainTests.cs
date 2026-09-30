using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class AlmacenDomainTests
{
    [Fact]
    public void TipoAlmacen_MantieneClasificacionesOperacionalesEstables()
    {
        Assert.Equal(1, (int)TipoAlmacen.Tienda);
        Assert.Equal(2, (int)TipoAlmacen.Bodega);
        Assert.Equal(3, (int)TipoAlmacen.Transito);
        Assert.Equal(4, (int)TipoAlmacen.Devolucion);
        Assert.Equal(5, (int)TipoAlmacen.Cuarentena);
        Assert.Equal(5, Enum.GetValues<TipoAlmacen>().Length);
    }

    [Fact]
    public void Almacen_Nuevo_IniciaActivoYSinEliminacion()
    {
        var almacen = new Almacen();

        Assert.True(almacen.Activo);
        Assert.False(almacen.Eliminado);
        Assert.Null(almacen.FechaEliminacion);
        Assert.Null(almacen.EliminadoPorUsuarioId);
    }

    [Fact]
    public void ContratosAlmacen_UsanSucursalComoJerarquiaSinEmpresaDuplicada()
    {
        Assert.Null(typeof(Almacen).GetProperty("EmpresaId"));
        Assert.Null(typeof(AlmacenDto).GetProperty("EmpresaId"));
        Assert.Null(typeof(CreateAlmacenDto).GetProperty("EmpresaId"));
        Assert.Null(typeof(UpdateAlmacenDto).GetProperty("EmpresaId"));

        var create = new CreateAlmacenDto { SucursalId = 11 };
        var update = new UpdateAlmacenDto { SucursalId = 12 };
        var response = new AlmacenDto { SucursalId = 13 };

        Assert.Equal(11, create.SucursalId);
        Assert.Equal(12, update.SucursalId);
        Assert.Equal(13, response.SucursalId);
    }
}
