using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class ExistenciaVarianteDomainTests
{
    [Fact]
    public void EstablecerStocks_DerivaDisponible_YExponeEstados()
    {
        var existencia = new ExistenciaVariante();

        existencia.EstablecerStocks(
            stockFisico: 12,
            stockReservado: 3,
            stockTransito: 4,
            stockMinimo: 10,
            stockMaximo: 30);

        Assert.Equal(12, existencia.StockFisico);
        Assert.Equal(3, existencia.StockReservado);
        Assert.Equal(9, existencia.StockDisponible);
        Assert.Equal(4, existencia.StockTransito);
        Assert.Equal(10, existencia.StockMinimo);
        Assert.Equal(30, existencia.StockMaximo);
        Assert.True(existencia.TieneStockBajo);
        Assert.False(existencia.EstaAgotada);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0, null)]
    [InlineData(0, -1, 0, 0, null)]
    [InlineData(0, 0, -1, 0, null)]
    [InlineData(0, 0, 0, -1, null)]
    public void EstablecerStocks_RechazaValoresNegativos(
        int fisico,
        int reservado,
        int transito,
        int minimo,
        int? maximo)
    {
        var existencia = new ExistenciaVariante();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            existencia.EstablecerStocks(fisico, reservado, transito, minimo, maximo));
    }

    [Fact]
    public void EstablecerStocks_RechazaReservadoMayorQueFisico()
    {
        var existencia = new ExistenciaVariante();

        Assert.Throws<ArgumentException>(() =>
            existencia.EstablecerStocks(5, 6, 0, 0, null));
    }

    [Fact]
    public void EstablecerStocks_RechazaMaximoMenorQueMinimo()
    {
        var existencia = new ExistenciaVariante();

        Assert.Throws<ArgumentException>(() =>
            existencia.EstablecerStocks(5, 0, 0, 10, 9));
    }

    [Fact]
    public void EstablecerStocks_PermiteMaximoNulo_YAgotadaCuandoDisponibleCero()
    {
        var existencia = new ExistenciaVariante();

        existencia.EstablecerStocks(5, 5, 2, 1, null);

        Assert.Equal(0, existencia.StockDisponible);
        Assert.Null(existencia.StockMaximo);
        Assert.True(existencia.EstaAgotada);
        Assert.True(existencia.TieneStockBajo);
    }

    [Fact]
    public void Entidad_NoDuplicaContextoSucursalNiEmpresa()
    {
        var propiedades = typeof(ExistenciaVariante).GetProperties()
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("SucursalId", propiedades);
        Assert.DoesNotContain("EmpresaId", propiedades);
        Assert.Contains(nameof(ExistenciaVariante.ProductoVarianteId), propiedades);
        Assert.Contains(nameof(ExistenciaVariante.AlmacenId), propiedades);
        Assert.Contains(nameof(ExistenciaVariante.UbicacionAlmacenId), propiedades);
    }

    [Fact]
    public void Contratos_NoAceptanStockDisponibleComoInput()
    {
        Assert.Null(typeof(CreateExistenciaVarianteDto).GetProperty("StockDisponible"));
        Assert.Null(typeof(UpdateExistenciaVarianteConfiguracionDto).GetProperty("StockDisponible"));
        Assert.Null(typeof(UpdateExistenciaVarianteConfiguracionDto).GetProperty("StockFisico"));
        Assert.Null(typeof(UpdateExistenciaVarianteConfiguracionDto).GetProperty("StockReservado"));
        Assert.Null(typeof(UpdateExistenciaVarianteConfiguracionDto).GetProperty("StockTransito"));
    }

    [Fact]
    public void Ubicacion_EsOpcionalEnEntidadYContratos()
    {
        var existencia = new ExistenciaVariante
        {
            ProductoVarianteId = 10,
            AlmacenId = 20,
            UbicacionAlmacenId = null
        };
        var dto = new CreateExistenciaVarianteDto
        {
            ProductoVarianteId = 10,
            AlmacenId = 20,
            UbicacionAlmacenId = null
        };

        Assert.Null(existencia.UbicacionAlmacenId);
        Assert.Null(dto.UbicacionAlmacenId);
    }
}
