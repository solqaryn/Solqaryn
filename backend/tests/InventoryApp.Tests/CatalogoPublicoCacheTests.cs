using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class CatalogoPublicoCacheTests
{
    [Fact]
    public async Task BuscarAsync_ReutilizaResultadoParaMismoTenantYParametros()
    {
        var repository = new Mock<IProductoCatalogoPublicoRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetPagedSummaryAsync(
                It.IsAny<ProductoPagedRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ProductoCatalogoResumenReadModel>(), 0));

        var promociones = new Mock<IPromocionPublicaService>(MockBehavior.Strict);
        var inventario = new Mock<IInventarioPublicoService>(MockBehavior.Strict);
        var tenant = new Mock<IPublicStoreTenantKeyProvider>(MockBehavior.Strict);
        tenant.Setup(x => x.GetTenantKeyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("empresa:1");

        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new PublicStoreMemoryCache(memory);
        var sut = new CatalogoPublicoService(
            repository.Object,
            promociones.Object,
            inventario.Object,
            cache,
            tenant.Object);

        var first = await sut.BuscarAsync(new ProductoPagedRequest { Page = 1, PageSize = 24, Search = "camara" });
        var second = await sut.BuscarAsync(new ProductoPagedRequest { Page = 1, PageSize = 24, Search = "camara" });

        Assert.Empty(first.Items);
        Assert.Empty(second.Items);
        repository.Verify(x => x.GetPagedSummaryAsync(
            It.IsAny<ProductoPagedRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
        tenant.Verify(x => x.GetTenantKeyAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        repository.VerifyNoOtherCalls();
        promociones.VerifyNoOtherCalls();
        inventario.VerifyNoOtherCalls();
    }
}
