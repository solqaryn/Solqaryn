using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N14AjusteInventarioContextoFisicoContractTests
{
    [Fact]
    public void DetalleInput_ExigeAlmacenYPreservaUbicacionOpcional()
    {
        var dto = new AjusteInventarioDetalleInputDto
        {
            ProductoId = 10,
            ProductoVarianteId = 20,
            AlmacenId = 30,
            UbicacionAlmacenId = null,
            CantidadObjetivo = 7
        };

        Assert.Equal(30, dto.AlmacenId);
        Assert.Null(dto.UbicacionAlmacenId);
    }

    [Fact]
    public void DetalleSalida_ExponeContextoFisicoPersistido()
    {
        var dto = new AjusteInventarioDetalleDto
        {
            Id = 1,
            ProductoId = 10,
            ProductoVarianteId = 20,
            AlmacenId = 30,
            UbicacionAlmacenId = 40,
            CantidadObjetivo = 7
        };

        Assert.Equal(30, dto.AlmacenId);
        Assert.Equal(40, dto.UbicacionAlmacenId);
    }

    [Fact]
    public void Entidad_ConservaContextoFisicoNullableParaHistoricosPreCutover()
    {
        var historico = new AjusteInventarioDetalle
        {
            ProductoId = 10,
            ProductoVarianteId = 20,
            AlmacenId = null,
            UbicacionAlmacenId = null,
            CantidadObjetivo = 7
        };

        Assert.Null(historico.AlmacenId);
        Assert.Null(historico.UbicacionAlmacenId);
    }
}
