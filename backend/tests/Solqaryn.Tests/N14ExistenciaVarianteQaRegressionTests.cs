using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class N14ExistenciaVarianteQaRegressionTests
{
    [Fact]
    public void EstablecerStocks_FalloDeValidacion_NoDejaEstadoParcialmenteMutado()
    {
        var existencia = new ExistenciaVariante();
        existencia.EstablecerStocks(
            stockFisico: 20,
            stockReservado: 4,
            stockTransito: 3,
            stockMinimo: 5,
            stockMaximo: 40);

        var error = Assert.Throws<ArgumentException>(() =>
            existencia.EstablecerStocks(
                stockFisico: 10,
                stockReservado: 11,
                stockTransito: 99,
                stockMinimo: 1,
                stockMaximo: 2));

        Assert.Equal("stockReservado", error.ParamName);
        Assert.Equal(20, existencia.StockFisico);
        Assert.Equal(4, existencia.StockReservado);
        Assert.Equal(16, existencia.StockDisponible);
        Assert.Equal(3, existencia.StockTransito);
        Assert.Equal(5, existencia.StockMinimo);
        Assert.Equal(40, existencia.StockMaximo);
    }

    [Fact]
    public void EstablecerStocks_FalloDeValidacionTardia_NoDejaEstadoParcialmenteMutado()
    {
        var existencia = new ExistenciaVariante();
        existencia.EstablecerStocks(
            stockFisico: 30,
            stockReservado: 5,
            stockTransito: 2,
            stockMinimo: 6,
            stockMaximo: 60);

        var error = Assert.Throws<ArgumentException>(() =>
            existencia.EstablecerStocks(
                stockFisico: 50,
                stockReservado: 10,
                stockTransito: 8,
                stockMinimo: 20,
                stockMaximo: 19));

        Assert.Equal("stockMaximo", error.ParamName);
        Assert.Equal(30, existencia.StockFisico);
        Assert.Equal(5, existencia.StockReservado);
        Assert.Equal(25, existencia.StockDisponible);
        Assert.Equal(2, existencia.StockTransito);
        Assert.Equal(6, existencia.StockMinimo);
        Assert.Equal(60, existencia.StockMaximo);
    }

    [Fact]
    public void EstablecerStocks_ReconfiguracionValida_RecalculaDisponibleSinArrastrarSnapshotAnterior()
    {
        var existencia = new ExistenciaVariante();
        existencia.EstablecerStocks(25, 10, 6, 4, 50);

        existencia.EstablecerStocks(8, 3, 0, 2, null);

        Assert.Equal(8, existencia.StockFisico);
        Assert.Equal(3, existencia.StockReservado);
        Assert.Equal(5, existencia.StockDisponible);
        Assert.Equal(0, existencia.StockTransito);
        Assert.Equal(2, existencia.StockMinimo);
        Assert.Null(existencia.StockMaximo);
        Assert.False(existencia.EstaAgotada);
        Assert.False(existencia.TieneStockBajo);
    }

    [Fact]
    public void IndicadoresStock_RespetanUmbralesDeDisponible()
    {
        var existencia = new ExistenciaVariante();

        existencia.EstablecerStocks(
            stockFisico: 10,
            stockReservado: 5,
            stockTransito: 0,
            stockMinimo: 5,
            stockMaximo: 20);

        Assert.Equal(5, existencia.StockDisponible);
        Assert.True(existencia.TieneStockBajo);
        Assert.False(existencia.EstaAgotada);

        existencia.EstablecerStocks(
            stockFisico: 10,
            stockReservado: 10,
            stockTransito: 0,
            stockMinimo: 5,
            stockMaximo: 20);

        Assert.Equal(0, existencia.StockDisponible);
        Assert.True(existencia.TieneStockBajo);
        Assert.True(existencia.EstaAgotada);
    }
}
