using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Services;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class PublicStoreTenantKeyProviderTests
{
    [Fact]
    public async Task GetTenantKeyAsync_UsaEmpresaRealCuandoLaIdentidadPublicaCoincide()
    {
        var configuracion = new Mock<IEmpresaConfiguracionRepository>(MockBehavior.Strict);
        configuracion.Setup(x => x.GetActivaAsync()).ReturnsAsync(new EmpresaConfiguracion
        {
            Id = 9,
            NombreComercial = "Storefront",
            NombreVisibleSistema = "Storefront",
            Activa = true
        });

        var empresas = new Mock<IEmpresaRepository>(MockBehavior.Strict);
        empresas.Setup(x => x.ListAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Empresa>
            {
                new("Storefront") { Id = 42 }
            });

        using var memory = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var cache = new PublicStoreMemoryCache(memory);
        var sut = new PublicStoreTenantKeyProvider(cache, configuracion.Object, empresas.Object);

        var tenantKey = await sut.GetTenantKeyAsync();
        var cachedTenantKey = await sut.GetTenantKeyAsync();

        Assert.Equal("empresa:42", tenantKey);
        Assert.Equal(tenantKey, cachedTenantKey);
        configuracion.Verify(x => x.GetActivaAsync(), Times.Once);
        empresas.Verify(x => x.ListAsync(true, It.IsAny<CancellationToken>()), Times.Once);

        cache.InvalidateAll(PublicStoreCacheSegments.Identity);
        var refreshedTenantKey = await sut.GetTenantKeyAsync();

        Assert.Equal("empresa:42", refreshedTenantKey);
        configuracion.Verify(x => x.GetActivaAsync(), Times.Exactly(2));
        empresas.Verify(x => x.ListAsync(true, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetTenantKeyAsync_FallaSeguroAHuellaDeConfiguracionCuandoNoPuedeDesambiguarEmpresa()
    {
        var configuracion = new Mock<IEmpresaConfiguracionRepository>(MockBehavior.Strict);
        configuracion.Setup(x => x.GetActivaAsync()).ReturnsAsync(new EmpresaConfiguracion
        {
            Id = 9,
            NombreComercial = "Tienda pública",
            NombreVisibleSistema = "Tienda pública",
            Activa = true
        });

        var empresas = new Mock<IEmpresaRepository>(MockBehavior.Strict);
        empresas.Setup(x => x.ListAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Empresa>
            {
                new("Empresa A") { Id = 1 },
                new("Empresa B") { Id = 2 }
            });

        using var memory = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var cache = new PublicStoreMemoryCache(memory);
        var sut = new PublicStoreTenantKeyProvider(cache, configuracion.Object, empresas.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetTenantKeyAsync());
        configuracion.VerifyAll();
        empresas.VerifyAll();
    }
    [Fact]
    public void SinVinculacionHost_NoSeleccionaEmpresaPorCoincidenciaDeNombre()
    {
        var empresas = new List<Empresa>
        {
            new("Empresa A") { Id = 1 },
            new("Storefront") { Id = 42 }
        };
        Assert.Throws<InvalidOperationException>(() =>
            PublicStoreTenantKeyProvider.ResolveTenantKey(
                9, "Storefront", "Storefront", empresas));
    }

    [Fact]
    public void SinEmpresas_NoEmiteClavePublicaAmbigua()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PublicStoreTenantKeyProvider.ResolveTenantKey(
                9, "Storefront", "Storefront", Array.Empty<Empresa>()));
    }

}
