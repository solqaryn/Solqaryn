using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N14ExistenciaVarianteInvariantTests
{
    [Fact]
    public void EstablecerStocks_CalculaDisponibleYFlagsDesdeFuenteFisica()
    {
        var existencia = new ExistenciaVariante();

        existencia.EstablecerStocks(
            stockFisico: 10,
            stockReservado: 3,
            stockTransito: 2,
            stockMinimo: 8,
            stockMaximo: 20);

        Assert.Equal(10, existencia.StockFisico);
        Assert.Equal(3, existencia.StockReservado);
        Assert.Equal(7, existencia.StockDisponible);
        Assert.Equal(2, existencia.StockTransito);
        Assert.Equal(8, existencia.StockMinimo);
        Assert.Equal(20, existencia.StockMaximo);
        Assert.True(existencia.TieneStockBajo);
        Assert.False(existencia.EstaAgotada);
    }

    [Fact]
    public void EstablecerStocks_CeroFisico_ProduceAgotadaYStockBajoSinDisponibleNegativo()
    {
        var existencia = new ExistenciaVariante();

        existencia.EstablecerStocks(
            stockFisico: 0,
            stockReservado: 0,
            stockTransito: 4,
            stockMinimo: 5,
            stockMaximo: null);

        Assert.Equal(0, existencia.StockDisponible);
        Assert.True(existencia.EstaAgotada);
        Assert.True(existencia.TieneStockBajo);
    }
}
