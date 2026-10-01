using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N67FTenantMutationFailClosedTests
{
    [Fact]
    public async Task UpdateTenantLogoAsync_SaveFailure_DoesNotDeletePreviousLogoOrEmitFalseAudit()
    {
        var repository = new Mock<IEmpresaConfiguracionRepository>();
        var imageStorage = new Mock<IImageStorageService>();
        var auditoria = new Mock<IAuditoriaService>();
        var empresaRepository = new Mock<IEmpresaRepository>();
        var scope = new Mock<IUsuarioScopeService>();

        var empresa = new Empresa("Acme Honduras") { Id = 23 };
        empresa.ActualizarIdentidadLegal("08011999123456", "Tegucigalpa", "https://cdn.example/old.png", "old-public-id");

        scope.Setup(service => service.ObtenerActualAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(9, 23, 4, "Administrador", true));
        empresaRepository.Setup(repository => repository.GetByIdAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa);
        empresaRepository.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var logo = new Mock<IFormFile>();
        logo.SetupGet(file => file.Length).Returns(128);
        logo.SetupGet(file => file.FileName).Returns("logo.png");
        logo.SetupGet(file => file.ContentType).Returns("image/png");
        imageStorage.Setup(storage => storage.UploadAsync(logo.Object))
            .ReturnsAsync(("https://cdn.example/new.png", "new-public-id"));

        var service = new EmpresaConfiguracionService(
            repository.Object,
            imageStorage.Object,
            auditoria.Object,
            empresaRepository.Object,
            scope.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateTenantLogoAsync(23, logo.Object));

        imageStorage.Verify(storage => storage.DeleteAsync("old-public-id"), Times.Never);
        VerifyNoAudit(auditoria);
    }

    [Fact]
    public async Task RestaurarTenantLogoAsync_SaveFailure_DoesNotDeletePreviousLogoOrEmitFalseAudit()
    {
        var repository = new Mock<IEmpresaConfiguracionRepository>();
        var imageStorage = new Mock<IImageStorageService>();
        var auditoria = new Mock<IAuditoriaService>();
        var empresaRepository = new Mock<IEmpresaRepository>();
        var scope = new Mock<IUsuarioScopeService>();

        var empresa = new Empresa("Acme Honduras") { Id = 23 };
        empresa.ActualizarIdentidadLegal("08011999123456", "Tegucigalpa", "https://cdn.example/old.png", "old-public-id");

        scope.Setup(service => service.ObtenerActualAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(9, 23, 4, "Administrador", true));
        empresaRepository.Setup(repository => repository.GetByIdAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa);
        empresaRepository.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new EmpresaConfiguracionService(
            repository.Object,
            imageStorage.Object,
            auditoria.Object,
            empresaRepository.Object,
            scope.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.RestaurarTenantLogoAsync(23));

        imageStorage.Verify(storage => storage.DeleteAsync("old-public-id"), Times.Never);
        VerifyNoAudit(auditoria);
    }

    private static void VerifyNoAudit(Mock<IAuditoriaService> auditoria)
    {
        auditoria.Verify(x => x.RegistrarAsync(
            It.IsAny<ModuloSistema>(),
            It.IsAny<AccionPermiso>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Never);
    }
}
