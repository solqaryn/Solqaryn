using Solqaryn.Domain.Entities.Contabilidad;
using Xunit;

namespace Solqaryn.Tests;

public class N411BCentroCostoEntityTests
{
    [Fact]
    public void Sucursal_RequiereAsociacionEstructural()
    {
        var centroCosto = new CentroCosto
        {
            Tipo = TipoCentroCosto.Sucursal,
            SucursalId = 7
        };

        Assert.True(centroCosto.TieneAsociacionValida());

        centroCosto.SucursalId = null;

        Assert.False(centroCosto.TieneAsociacionValida());
    }

    [Theory]
    [InlineData(TipoCentroCosto.Departamento)]
    [InlineData(TipoCentroCosto.Proyecto)]
    [InlineData(TipoCentroCosto.UnidadNegocio)]
    public void TipoSinAgregadoPersistente_NoAdmiteSucursalId(TipoCentroCosto tipo)
    {
        var centroCosto = new CentroCosto
        {
            Tipo = tipo,
            SucursalId = null
        };

        Assert.True(centroCosto.TieneAsociacionValida());

        centroCosto.SucursalId = 7;

        Assert.False(centroCosto.TieneAsociacionValida());
    }

    [Fact]
    public void MarcarEliminado_AplicaSoftDeleteYRevocaActividad()
    {
        var centroCosto = new CentroCosto { Activo = true };

        centroCosto.MarcarEliminado(42);

        Assert.False(centroCosto.Activo);
        Assert.True(centroCosto.Eliminado);
        Assert.NotNull(centroCosto.FechaEliminacion);
        Assert.Equal(42, centroCosto.EliminadoPorUsuarioId);
        Assert.NotEqual(default, centroCosto.FechaActualizacion);
    }

    [Fact]
    public void Activar_LimpiaEstadoDeSoftDelete()
    {
        var centroCosto = new CentroCosto
        {
            Activo = false,
            Eliminado = true,
            FechaEliminacion = DateTime.UtcNow.AddMinutes(-1),
            EliminadoPorUsuarioId = 42
        };

        centroCosto.Activar();

        Assert.True(centroCosto.Activo);
        Assert.False(centroCosto.Eliminado);
        Assert.Null(centroCosto.FechaEliminacion);
        Assert.Null(centroCosto.EliminadoPorUsuarioId);
        Assert.NotEqual(default, centroCosto.FechaActualizacion);
    }

    [Fact]
    public void Desactivar_NoMarcaEliminado()
    {
        var centroCosto = new CentroCosto { Activo = true };

        centroCosto.Desactivar();

        Assert.False(centroCosto.Activo);
        Assert.False(centroCosto.Eliminado);
        Assert.Null(centroCosto.FechaEliminacion);
        Assert.Null(centroCosto.EliminadoPorUsuarioId);
    }
}
