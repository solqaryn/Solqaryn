using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Application.Validators;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N62DTenantAwareApplicationApiTests
{
    private static SucursalService CreateService(Mock<ISucursalRepository> repository, Mock<IAuditoriaService>? auditoria = null)
    {
        var empresas = new Mock<IEmpresaRepository>();
        empresas.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new Empresa($"Empresa {id}") { Id = id });
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UsuarioId).Returns(7);
        currentUser.Setup(x => x.NombreUsuario).Returns("n62d-controller");
        return new SucursalService(repository.Object, empresas.Object, currentUser.Object, (auditoria ?? new Mock<IAuditoriaService>()).Object);
    }

    [Fact]
    public void CreateValidator_ExigeEmpresaIdParaOwnershipTenant()
    {
        var result = new CreateSucursalValidator().Validate(new CreateSucursalDto { EmpresaId = null, Codigo = "TGU-01", Nombre = "Centro", ZonaHoraria = "America/Tegucigalpa" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateSucursalDto.EmpresaId));
    }

    [Fact]
    public void UpdateValidator_ExigeEmpresaIdParaConservarOwnershipTenant()
    {
        var result = new UpdateSucursalValidator().Validate(new UpdateSucursalDto { EmpresaId = null, Codigo = "TGU-01", Nombre = "Centro", ZonaHoraria = "America/Tegucigalpa" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateSucursalDto.EmpresaId));
    }

    [Fact]
    public async Task CreateAsync_SinEmpresaId_FallaCerradoAntesDePersistir()
    {
        var repository = new Mock<ISucursalRepository>();
        var service = CreateService(repository);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new CreateSucursalDto { EmpresaId = null, Codigo = "TGU-01", Nombre = "Centro", ZonaHoraria = "America/Tegucigalpa" }, 42));
        repository.Verify(x => x.AddAsync(It.IsAny<Sucursal>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ConEmpresaId_PersisteOwnershipDeterminista()
    {
        var repository = new Mock<ISucursalRepository>();
        repository.Setup(x => x.ExisteCodigoAsync("TGU-01", 42, null)).ReturnsAsync(false);
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        Sucursal? persisted = null;
        repository.Setup(x => x.AddAsync(It.IsAny<Sucursal>())).Callback<Sucursal>(entity => persisted = entity).Returns(Task.CompletedTask);
        var result = await CreateService(repository).CreateAsync(new CreateSucursalDto { EmpresaId = 42, Codigo = "tgu-01", Nombre = "Centro", ZonaHoraria = "America/Tegucigalpa" }, 42);
        Assert.NotNull(persisted);
        Assert.Equal(42, persisted!.ObtenerEmpresaIdTenant());
        Assert.Equal(42, result.EmpresaId);
    }

    [Fact]
    public async Task UpdateAsync_SinEmpresaId_NoPuedeDejarOwnershipAmbiguo()
    {
        var repository = new Mock<ISucursalRepository>();
        var existing = new Sucursal { Id = 9, EmpresaId = 42, Codigo = "TGU-01", Nombre = "Centro", ZonaHoraria = "America/Tegucigalpa", Activa = true };
        repository.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(existing);
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService(repository).UpdateAsync(9, new UpdateSucursalDto { EmpresaId = null, Codigo = "TGU-01", Nombre = "Centro editado", ZonaHoraria = "America/Tegucigalpa" }, 42));
        Assert.Equal(42, existing.EmpresaId);
        repository.Verify(x => x.Update(It.IsAny<Sucursal>()), Times.Never);
    }

    [Fact]
    public async Task ReadLegacyNullable_FallaCerradoEnEndpointTenant()
    {
        var repository = new Mock<ISucursalRepository>();
        repository.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new Sucursal { Id = 10, EmpresaId = null, Codigo = "LEGACY", Nombre = "Legacy", ZonaHoraria = "America/Tegucigalpa", Activa = true });
        var result = await CreateService(repository).GetByIdAsync(10, 42);
        Assert.Null(result);
    }

    [Fact]
    public async Task BuscarAsync_SinEmpresaExplicita_UsaTenantAutorizado()
    {
        var repository = new Mock<ISucursalRepository>();
        repository.Setup(x => x.BuscarAsync(null, null, 42, 1, 25)).ReturnsAsync((new List<Sucursal>(), 0));
        await CreateService(repository).BuscarAsync(new SucursalFiltroDto { EmpresaId = null, Pagina = 1, TamanoPagina = 25 }, 42);
        repository.Verify(x => x.BuscarAsync(null, null, 42, 1, 25), Times.Once);
    }

    [Fact]
    public async Task BuscarAsync_EmpresaDistintaAlTenantAutorizado_FallaCerrado()
    {
        var repository = new Mock<ISucursalRepository>();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            CreateService(repository).BuscarAsync(new SucursalFiltroDto { EmpresaId = 99, Pagina = 1, TamanoPagina = 25 }, 42));
        repository.Verify(x => x.BuscarAsync(
            It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }
}
