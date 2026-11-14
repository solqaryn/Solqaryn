using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Services;

public class EmpresaConfiguracionTenantServiceTests
{
    [Fact]
    public async Task GetTenantAsync_SinMembresiaActiva_FallaCerradoSinLeerDatosTenant()
    {
        var repository = new Mock<IEmpresaConfiguracionRepository>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var empresaRepository = new Mock<IEmpresaRepository>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);

        scope.Setup(service => service.ObtenerActualAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);

        var service = new EmpresaConfiguracionService(
            repository.Object,
            imageStorage.Object,
            auditoria.Object,
            empresaRepository.Object,
            scope.Object);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetTenantAsync(23));

        empresaRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(repository => repository.GetTenantAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTenantAsync_ConMembresiaActiva_ExponeConfiguracionDelTenantSinReferenciaPrivada()
    {
        var repository = new Mock<IEmpresaConfiguracionRepository>();
        var imageStorage = new Mock<IImageStorageService>();
        var auditoria = new Mock<IAuditoriaService>();
        var empresaRepository = new Mock<IEmpresaRepository>();
        var scope = new Mock<IUsuarioScopeService>();

        var empresa = new Empresa("Acme Honduras") { Id = 23 };
        empresa.ActualizarIdentidadLegal("0801-1999-00000", "Tegucigalpa", "https://cdn.example.test/logo.png", "logo-ref");
        var config = new ConfigEmpresa(23)
        {
            Moneda = "USD",
            ZonaHoraria = "America/Tegucigalpa",
            ImpuestosJson = "{\"isv\":15}",
            EmisionJson = "{\"factura\":true}",
            CorreoRemitente = "facturas@example.test",
            CorreoNombreRemitente = "Acme",
            CorreoConfigurado = true,
            CorreoSecretoReferencia = "private-reference-23",
            Version = 7
        };
        var plantilla = new PlantillaCorreoEmpresa(23, "factura", "Factura {{numero}}", "Hola {{cliente}}");

        scope.Setup(service => service.ObtenerActualAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(9, 23, 4, "Administrador", true));
        empresaRepository.Setup(repository => repository.GetByIdAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa);
        repository.Setup(repository => repository.GetTenantAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        repository.Setup(repository => repository.ListPlantillasAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlantillaCorreoEmpresa> { plantilla });

        var service = new EmpresaConfiguracionService(
            repository.Object,
            imageStorage.Object,
            auditoria.Object,
            empresaRepository.Object,
            scope.Object);

        var result = await service.GetTenantAsync(23);

        Assert.Equal(23, result.EmpresaId);
        Assert.Equal("Acme Honduras", result.Nombre);
        Assert.Equal("0801199900000", result.Rtn);
        Assert.Equal("USD", result.Moneda);
        Assert.Equal(7, result.Version);
        Assert.True(result.CorreoConfigurado);
        Assert.Single(result.PlantillasCorreo);
        Assert.Equal("factura", result.PlantillasCorreo[0].TipoPlantilla);
        Assert.Null(typeof(ConfigEmpresaTenantDto).GetProperty("CorreoSecretoReferencia"));
    }

    [Fact]
    public async Task UpdateTenantAsync_VersionObsoleta_RechazaSinPersistir()
    {
        var repository = new Mock<IEmpresaConfiguracionRepository>();
        var imageStorage = new Mock<IImageStorageService>();
        var auditoria = new Mock<IAuditoriaService>();
        var empresaRepository = new Mock<IEmpresaRepository>();
        var scope = new Mock<IUsuarioScopeService>();

        var empresa = new Empresa("Acme Honduras") { Id = 23 };
        var config = new ConfigEmpresa(23) { Version = 5 };

        scope.Setup(service => service.ObtenerActualAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(9, 23, 4, "Administrador", true));
        empresaRepository.Setup(repository => repository.GetByIdAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa);
        repository.Setup(repository => repository.GetTenantAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var service = new EmpresaConfiguracionService(
            repository.Object,
            imageStorage.Object,
            auditoria.Object,
            empresaRepository.Object,
            scope.Object);

        var dto = new UpdateConfigEmpresaTenantDto
        {
            Nombre = "Acme Honduras",
            Moneda = "HNL",
            ZonaHoraria = "America/Tegucigalpa",
            ImpuestosJson = "{}",
            EmisionJson = "{}",
            Version = 4
        };

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateTenantAsync(23, dto));

        empresaRepository.Verify(repository => repository.Update(It.IsAny<Empresa>()), Times.Never);
        repository.Verify(repository => repository.UpdateTenant(It.IsAny<ConfigEmpresa>()), Times.Never);
        repository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateTenantLogoAsync_UsaStorageTenantAwareDerivadoDeMembresiaVerificada()
    {
        var repository = new Mock<IEmpresaConfiguracionRepository>();
        var imageStorage = new Mock<IImageStorageService>();
        var auditoria = new Mock<IAuditoriaService>();
        var empresaRepository = new Mock<IEmpresaRepository>();
        var scope = new Mock<IUsuarioScopeService>();

        var empresa = new Empresa("Acme Honduras") { Id = 23 };
        var config = new ConfigEmpresa(23) { Version = 1 };
        var tenantScope = new UsuarioTenantScopeActual(9, 23, 4, "Administrador", true);

        scope.Setup(service => service.ObtenerActualAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantScope);
        empresaRepository.Setup(repository => repository.GetByIdAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa);
        empresaRepository.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(repository => repository.GetTenantAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        repository.Setup(repository => repository.ListPlantillasAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlantillaCorreoEmpresa>());
        imageStorage.Setup(storage => storage.UploadAsync(
                It.Is<StorageTenantContext>(tenant => tenant.EmpresaId == 23 && tenant.UsuarioId == 9),
                It.IsAny<IFormFile>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(("https://res.cloudinary.com/test/image/upload/v1/solqaryn/productos/empresas/23/logo.png",
                "solqaryn/productos/empresas/23/logo"));

        var service = new EmpresaConfiguracionService(
            repository.Object,
            imageStorage.Object,
            auditoria.Object,
            empresaRepository.Object,
            scope.Object);

        await using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        IFormFile logo = new FormFile(stream, 0, stream.Length, "logo", "logo.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var result = await service.UpdateTenantLogoAsync(23, logo);

        Assert.Equal(23, result.EmpresaId);
        Assert.Equal("solqaryn/productos/empresas/23/logo", empresa.LogoPublicId);
        imageStorage.Verify(storage => storage.UploadAsync(
            It.Is<StorageTenantContext>(tenant => tenant.EmpresaId == 23 && tenant.UsuarioId == 9),
            logo,
            It.IsAny<CancellationToken>()), Times.Once);
        imageStorage.Verify(storage => storage.UploadAsync(It.IsAny<IFormFile>()), Times.Never);
    }
}
