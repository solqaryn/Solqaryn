using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class InventarioPublicoServiceTests
{
    [Fact]
    public async Task ObtenerPorVariantesAsync_AgregaDisponibleYMinimoDeExistenciasAutoritativas()
    {
        var repo = new Mock<IExistenciaVarianteRepository>();
        var a = new ExistenciaVariante { ProductoVarianteId = 10 };
        a.EstablecerStocks(10, 2, 0, 3, null);
        var b = new ExistenciaVariante { ProductoVarianteId = 10 };
        b.EstablecerStocks(5, 1, 0, 2, null);
        repo.Setup(x => x.GetOperativasPublicasPorVariantesAsync(
                It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 10 }))))
            .ReturnsAsync(new List<ExistenciaVariante> { a, b });

        var resultado = await new InventarioPublicoService(repo.Object)
            .ObtenerPorVariantesAsync(new[] { 10 });

        var stock = Assert.Single(resultado).Value;
        Assert.Equal(12, stock.CantidadDisponible);
        Assert.Equal(5, stock.StockMinimo);
        Assert.False(stock.TieneStockBajo);
        Assert.False(stock.EstaAgotada);
        Assert.True(stock.TieneFuenteAutoritativa);
    }

    [Fact]
    public async Task ObtenerPorVariantesAsync_SinExistenciaAutoritativa_FallaCerradoEnCero()
    {
        var repo = new Mock<IExistenciaVarianteRepository>();
        repo.Setup(x => x.GetOperativasPublicasPorVariantesAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<ExistenciaVariante>());

        var resultado = await new InventarioPublicoService(repo.Object)
            .ObtenerPorVariantesAsync(new[] { 11 });

        var stock = Assert.Single(resultado).Value;
        Assert.Equal(0, stock.CantidadDisponible);
        Assert.True(stock.EstaAgotada);
        Assert.False(stock.TieneFuenteAutoritativa);
    }
}
