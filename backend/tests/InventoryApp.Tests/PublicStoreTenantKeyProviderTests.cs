using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class PublicStoreTenantKeyProviderTests
{
    [Fact]
    public async Task GetTenantKeyAsync_UsaEmpresaRealCuandoLaIdentidadPublicaCoincide()
    {
        var configuracion = new Mock<IEmpresaConfiguracionRepository>(MockBehavior.Strict);
        configuracion.Setup(x => x.GetActivaAsync()).ReturnsAsync(new EmpresaConfiguracion
        {
            Id = 9,
            NombreComercial = "VariStoreHN",
            NombreVisibleSistema = "VariStoreHN",
            Activa = true
        });

        var empresas = new Mock<IEmpresaRepository>(MockBehavior.Strict);
        empresas.Setup(x => x.ListAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Empresa>
            {
                new("Otra empresa") { Id = 11 },
                new("VariStoreHN") { Id = 42 }
            });

        using var memory = new MemoryCache(new MemoryCacheOptions());
        var sut = new PublicStoreTenantKeyProvider(memory, configuracion.Object, empresas.Object);

        var tenantKey = await sut.GetTenantKeyAsync();

        Assert.Equal("empresa:42", tenantKey);
        configuracion.VerifyAll();
        empresas.VerifyAll();
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

        using var memory = new MemoryCache(new MemoryCacheOptions());
        var sut = new PublicStoreTenantKeyProvider(memory, configuracion.Object, empresas.Object);

        var tenantKey = await sut.GetTenantKeyAsync();

        Assert.Equal("public-config:9", tenantKey);
        configuracion.VerifyAll();
        empresas.VerifyAll();
    }
}
