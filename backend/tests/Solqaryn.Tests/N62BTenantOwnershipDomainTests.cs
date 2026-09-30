using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class N62BTenantOwnershipDomainTests
{
    [Fact]
    public void Sucursal_ResuelveEmpresaTenant_CuandoEmpresaIdEsPositivo()
    {
        var sucursal = new Sucursal { EmpresaId = 17 };

        Assert.Equal(17, sucursal.ObtenerEmpresaIdTenant());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Sucursal_FallaCerrado_CuandoEmpresaTenantNoEsValida(int? empresaId)
    {
        var sucursal = new Sucursal { EmpresaId = empresaId };

        Assert.Throws<InvalidOperationException>(() => sucursal.ObtenerEmpresaIdTenant());
    }

    [Fact]
    public void Almacen_DerivaEmpresaDesdeSucursal_SinEmpresaIdDuplicado()
    {
        var almacen = new Almacen
        {
            SucursalId = 10,
            Sucursal = new Sucursal { EmpresaId = 23 }
        };

        Assert.Equal(23, almacen.ObtenerEmpresaIdTenant());
        Assert.Null(typeof(Almacen).GetProperty("EmpresaId"));
    }

    [Fact]
    public void Ubicacion_DerivaEmpresaPorAlmacenYSucursal_SinEmpresaIdDuplicado()
    {
        var ubicacion = new UbicacionAlmacen
        {
            AlmacenId = 20,
            Almacen = new Almacen
            {
                SucursalId = 10,
                Sucursal = new Sucursal { EmpresaId = 31 }
            }
        };

        Assert.Equal(31, ubicacion.ObtenerEmpresaIdTenant());
        Assert.Null(typeof(UbicacionAlmacen).GetProperty("EmpresaId"));
    }

    [Fact]
    public void JerarquiaTenant_FallaCerrado_SiFaltaNavegacionCanonica()
    {
        Assert.Throws<InvalidOperationException>(() => new Almacen().ObtenerEmpresaIdTenant());
        Assert.Throws<InvalidOperationException>(() => new UbicacionAlmacen().ObtenerEmpresaIdTenant());
    }
}