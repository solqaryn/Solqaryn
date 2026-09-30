using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class N63BSucursalesEmpresaDomainContractsTests
{
    [Fact]
    public void AsignarEmpresa_establece_owner_inicial_y_permite_idempotencia()
    {
        var sucursal = new Sucursal();

        sucursal.AsignarEmpresa(11);
        sucursal.AsignarEmpresa(11);

        Assert.Equal(11, sucursal.ObtenerEmpresaIdTenant());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AsignarEmpresa_rechaza_owner_no_positivo(int empresaId)
    {
        var sucursal = new Sucursal();

        Assert.Throws<ArgumentOutOfRangeException>(() => sucursal.AsignarEmpresa(empresaId));
    }

    [Fact]
    public void AsignarEmpresa_no_reasigna_owner_implicitamente()
    {
        var sucursal = new Sucursal { EmpresaId = 3 };

        Assert.Throws<InvalidOperationException>(() => sucursal.AsignarEmpresa(4));
        Assert.Equal(3, sucursal.EmpresaId);
    }

    [Fact]
    public void ReasignarEmpresa_exige_owner_actual_y_cambio_explicito()
    {
        var sinOwner = new Sucursal();
        Assert.Throws<InvalidOperationException>(() => sinOwner.ReasignarEmpresa(7));

        var sucursal = new Sucursal { EmpresaId = 3 };
        sucursal.ReasignarEmpresa(7);

        Assert.Equal(7, sucursal.ObtenerEmpresaIdTenant());
    }

    [Fact]
    public void Codigo_es_unico_logicamente_por_empresa_y_normalizado()
    {
        var sucursalA = new Sucursal { EmpresaId = 1, Codigo = "  cen-01 " };
        var sucursalB = new Sucursal { EmpresaId = 1, Codigo = "CEN-01" };
        var sucursalOtraEmpresa = new Sucursal { EmpresaId = 2, Codigo = "cen-01" };

        Assert.Equal(sucursalA.ObtenerClaveCodigoEmpresa(), sucursalB.ObtenerClaveCodigoEmpresa());
        Assert.NotEqual(sucursalA.ObtenerClaveCodigoEmpresa(), sucursalOtraEmpresa.ObtenerClaveCodigoEmpresa());
    }

    [Fact]
    public void ClaveCodigoEmpresa_falla_cerrado_sin_owner_o_codigo()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new Sucursal { Codigo = "CEN-01" }.ObtenerClaveCodigoEmpresa());

        Assert.Throws<InvalidOperationException>(() =>
            new Sucursal { EmpresaId = 1, Codigo = "   " }.ObtenerClaveCodigoEmpresa());
    }
}