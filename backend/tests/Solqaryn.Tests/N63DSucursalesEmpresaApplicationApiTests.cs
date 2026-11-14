using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N63DSucursalesEmpresaApplicationApiTests
{
    private static SucursalService CreateService(Mock<ISucursalRepository> repository, Mock<IEmpresaRepository>? empresas = null)
    {
        if (empresas is null)
        {
            empresas = new Mock<IEmpresaRepository>();
            empresas.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => new Empresa($"Empresa {id}") { Id = id });
        }
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UsuarioId).Returns(11);
        currentUser.Setup(x => x.NombreUsuario).Returns("n63d-controller");
        return new SucursalService(repository.Object, empresas.Object, currentUser.Object, new Mock<IAuditoriaService>().Object);
    }

    [Fact]
    public async Task CreateAsync_ValidaCodigoDentroDeLaEmpresa_NoGlobalmente()
    {
        var repository = new Mock<ISucursalRepository>();
        repository.Setup(x => x.ExisteCodigoAsync("CENTRO", 42, null)).ReturnsAsync(false);
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        var service = CreateService(repository);
        await service.CreateAsync(new CreateSucursalDto { EmpresaId = 42, Codigo = "centro", Nombre = "Sucursal Centro", ZonaHoraria = "America/Tegucigalpa" }, 42);
        repository.Verify(x => x.ExisteCodigoAsync("CENTRO", 42, null), Times.Once);
        repository.Verify(x => x.ExisteCodigoAsync("CENTRO", It.IsAny<int?>()), Times.Never);
        repository.Verify(x => x.AddAsync(It.Is<Sucursal>(s => s.EmpresaId == 42 && s.Codigo == "CENTRO")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SinEmpresaId_UsaTenantAutorizado()
    {
        var repository = new Mock<ISucursalRepository>();
        repository.Setup(x => x.ExisteCodigoAsync("CENTRO", 42, null)).ReturnsAsync(false);
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            new CreateSucursalDto { Codigo = "CENTRO", Nombre = "Sucursal Centro", ZonaHoraria = "America/Tegucigalpa" },
            42);

        Assert.Equal(42, result.EmpresaId);
        repository.Verify(x => x.AddAsync(It.Is<Sucursal>(s => s.EmpresaId == 42)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CodigoDuplicadoEnMismaEmpresa_FallaAntesDePersistir()
    {
        var repository = new Mock<ISucursalRepository>();
        repository.Setup(x => x.ExisteCodigoAsync("CENTRO", 42, null)).ReturnsAsync(true);
        var service = CreateService(repository);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new CreateSucursalDto { EmpresaId = 42, Codigo = "CENTRO", Nombre = "Sucursal Centro", ZonaHoraria = "America/Tegucigalpa" }, 42));
        repository.Verify(x => x.AddAsync(It.IsAny<Sucursal>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmpresaInexistente_FallaCerrado()
    {
        var repository = new Mock<ISucursalRepository>();
        var empresas = new Mock<IEmpresaRepository>();
        empresas.Setup(x => x.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Empresa?)null);
        var service = CreateService(repository, empresas);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new CreateSucursalDto { EmpresaId = 42, Codigo = "CENTRO", Nombre = "Centro", ZonaHoraria = "America/Tegucigalpa" }, 42));
        repository.Verify(x => x.AddAsync(It.IsAny<Sucursal>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmpresaInactiva_FallaCerrado()
    {
        var repository = new Mock<ISucursalRepository>();
        var empresas = new Mock<IEmpresaRepository>();
        var empresa = new Empresa("Empresa 42") { Id = 42 };
        empresa.Desactivar();
        empresas.Setup(x => x.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);
        var service = CreateService(repository, empresas);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new CreateSucursalDto { EmpresaId = 42, Codigo = "CENTRO", Nombre = "Centro", ZonaHoraria = "America/Tegucigalpa" }, 42));

        repository.Verify(x => x.AddAsync(It.IsAny<Sucursal>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ValidaEmpresaDestinoActiva_YCodigoEnEmpresaDestino()
    {
        var repository = new Mock<ISucursalRepository>();
        repository.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(new Sucursal { Id = 9, EmpresaId = 42, Codigo = "NORTE", Nombre = "Norte", ZonaHoraria = "America/Tegucigalpa", Activa = true });
        repository.Setup(x => x.ExisteCodigoAsync("CENTRO", 42, 9)).ReturnsAsync(false);
        repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        var service = CreateService(repository);
        var result = await service.UpdateAsync(9, new UpdateSucursalDto { EmpresaId = 42, Codigo = "centro", Nombre = "Centro", ZonaHoraria = "America/Tegucigalpa" }, 42);
        Assert.NotNull(result);
        Assert.Equal(42, result!.EmpresaId);
        repository.Verify(x => x.ExisteCodigoAsync("CENTRO", 42, 9), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NoPermiteReasignarSucursalEntreTenants()
    {
        var repository = new Mock<ISucursalRepository>();
        repository.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(new Sucursal { Id = 9, EmpresaId = 42, Codigo = "NORTE", Nombre = "Norte", ZonaHoraria = "America/Tegucigalpa", Activa = true });
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.UpdateAsync(9, new UpdateSucursalDto { EmpresaId = 99, Codigo = "CENTRO", Nombre = "Centro", ZonaHoraria = "America/Tegucigalpa" }, 42));

        repository.Verify(x => x.Update(It.IsAny<Sucursal>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}