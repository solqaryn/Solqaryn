using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Domain.Entities.Contabilidad;
using Xunit;

namespace Solqaryn.Tests;

public class CentroCostoDomainTests
{
    [Fact]
    public void TipoCentroCosto_MantieneClasificacionesFuncionalesEstables()
    {
        Assert.Equal(1, (int)TipoCentroCosto.Sucursal);
        Assert.Equal(2, (int)TipoCentroCosto.Departamento);
        Assert.Equal(3, (int)TipoCentroCosto.Proyecto);
        Assert.Equal(4, (int)TipoCentroCosto.UnidadNegocio);
        Assert.Equal(4, Enum.GetValues<TipoCentroCosto>().Length);
    }

    [Fact]
    public void CentroCosto_Sucursal_RequiereSucursalReal()
    {
        var centro = new CentroCosto { Tipo = TipoCentroCosto.Sucursal };
        Assert.False(centro.TieneAsociacionValida());

        centro.SucursalId = 7;
        Assert.True(centro.TieneAsociacionValida());
    }

    [Theory]
    [InlineData(TipoCentroCosto.Departamento)]
    [InlineData(TipoCentroCosto.Proyecto)]
    [InlineData(TipoCentroCosto.UnidadNegocio)]
    public void CentroCosto_TipoSinAgregado_NoInventaSucursal(TipoCentroCosto tipo)
    {
        var centro = new CentroCosto { Tipo = tipo };
        Assert.True(centro.TieneAsociacionValida());

        centro.SucursalId = 9;
        Assert.False(centro.TieneAsociacionValida());
    }

    [Fact]
    public void Contratos_NoExponenFksFicticias()
    {
        Assert.Null(typeof(CentroCosto).GetProperty("DepartamentoId"));
        Assert.Null(typeof(CentroCosto).GetProperty("ProyectoId"));
        Assert.Null(typeof(CentroCosto).GetProperty("UnidadNegocioId"));
        Assert.Null(typeof(CreateCentroCostoDto).GetProperty("DepartamentoId"));
        Assert.Null(typeof(CreateCentroCostoDto).GetProperty("ProyectoId"));
        Assert.Null(typeof(CreateCentroCostoDto).GetProperty("UnidadNegocioId"));
    }

    [Fact]
    public void CentroCosto_EliminacionEsAuditadaYFailClosed()
    {
        var centro = new CentroCosto { Activo = true };

        centro.MarcarEliminado(42);

        Assert.False(centro.Activo);
        Assert.True(centro.Eliminado);
        Assert.NotNull(centro.FechaEliminacion);
        Assert.Equal(42, centro.EliminadoPorUsuarioId);
    }
}
