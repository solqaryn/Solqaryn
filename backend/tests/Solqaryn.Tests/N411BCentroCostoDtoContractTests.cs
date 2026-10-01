using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Domain.Entities.Contabilidad;
using Xunit;

namespace Solqaryn.Tests;

public class N411BCentroCostoDtoContractTests
{
    [Fact]
    public void TipoCentroCosto_ConservaCodigosEstables()
    {
        Assert.Equal(1, (int)TipoCentroCosto.Sucursal);
        Assert.Equal(2, (int)TipoCentroCosto.Departamento);
        Assert.Equal(3, (int)TipoCentroCosto.Proyecto);
        Assert.Equal(4, (int)TipoCentroCosto.UnidadNegocio);
    }

    [Fact]
    public void CreateDto_NoExponeRelacionesFicticias()
    {
        Assert.NotNull(typeof(CreateCentroCostoDto).GetProperty(nameof(CreateCentroCostoDto.SucursalId)));
        Assert.Null(typeof(CreateCentroCostoDto).GetProperty("DepartamentoId"));
        Assert.Null(typeof(CreateCentroCostoDto).GetProperty("ProyectoId"));
        Assert.Null(typeof(CreateCentroCostoDto).GetProperty("UnidadNegocioId"));
    }

    [Fact]
    public void UpdateDto_ExponeEstadoYAsociacionTipada()
    {
        var dto = new UpdateCentroCostoDto
        {
            Codigo = "CC-TGU",
            Nombre = "Centro de costo Tegucigalpa",
            Tipo = TipoCentroCosto.Sucursal,
            SucursalId = 1,
            Activo = false
        };

        Assert.Equal(TipoCentroCosto.Sucursal, dto.Tipo);
        Assert.Equal(1, dto.SucursalId);
        Assert.False(dto.Activo);
    }

    [Fact]
    public void ResponseDto_ConservaAuditoriaYSoftDelete()
    {
        Assert.NotNull(typeof(CentroCostoDto).GetProperty(nameof(CentroCostoDto.FechaCreacion)));
        Assert.NotNull(typeof(CentroCostoDto).GetProperty(nameof(CentroCostoDto.FechaActualizacion)));
        Assert.NotNull(typeof(CentroCostoDto).GetProperty(nameof(CentroCostoDto.Eliminado)));
        Assert.NotNull(typeof(CentroCostoDto).GetProperty(nameof(CentroCostoDto.FechaEliminacion)));
        Assert.NotNull(typeof(CentroCostoDto).GetProperty(nameof(CentroCostoDto.EliminadoPorUsuarioId)));
    }
}
