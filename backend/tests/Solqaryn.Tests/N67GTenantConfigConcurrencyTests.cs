using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N67GTenantConfigConcurrencyTests
{
    [Fact]
    public async Task GetTenantAsync_ConcurrentCreateLoser_ReloadsWinnerWithout409()
    {
        const int empresaId = 31;
        var repository = new Mock<IEmpresaConfiguracionRepository>();
        var imageStorage = new Mock<IImageStorageService>();
        var auditoria = new Mock<IAuditoriaService>();
        var empresaRepository = new Mock<IEmpresaRepository>();
        var scope = new Mock<IUsuarioScopeService>();

        var empresa = new Empresa("Tenant Concurrente") { Id = empresaId };
        var winner = new ConfigEmpresa(empresaId)
        {
            Moneda = "USD",
            Version = 4
        };

        scope.Setup(service => service.ObtenerActualAsync(empresaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(9, empresaId, 4, "Administrador", true));
        empresaRepository.Setup(repo => repo.GetByIdAsync(empresaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa);
        repository.SetupSequence(repo => repo.GetTenantAsync(empresaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigEmpresa?)null)
            .ReturnsAsync(winner);
        repository.Setup(repo => repo.SaveChangesAsync())
            .ThrowsAsync(new InvalidOperationException("duplicate tenant config"));
        repository.Setup(repo => repo.ListPlantillasAsync(empresaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlantillaCorreoEmpresa>());

        var service = new EmpresaConfiguracionService(
            repository.Object,
            imageStorage.Object,
            auditoria.Object,
            empresaRepository.Object,
            scope.Object);

        var result = await service.GetTenantAsync(empresaId);

        Assert.Equal(empresaId, result.EmpresaId);
        Assert.Equal("USD", result.Moneda);
        Assert.Equal(4, result.Version);
        repository.Verify(repo => repo.AddTenantAsync(
            It.Is<ConfigEmpresa>(config => config.EmpresaId == empresaId),
            It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(repo => repo.DetachTenant(
            It.Is<ConfigEmpresa>(config => config.EmpresaId == empresaId)), Times.Once);
        repository.Verify(repo => repo.GetTenantAsync(empresaId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
