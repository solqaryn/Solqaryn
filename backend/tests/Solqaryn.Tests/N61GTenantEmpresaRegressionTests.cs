using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Reflection;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N61GTenantEmpresaRegressionTests
{
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();

    public N61GTenantEmpresaRegressionTests()
    {
        _currentUser.SetupGet(x => x.UsuarioId).Returns(99);
        _currentUser.SetupGet(x => x.NombreUsuario).Returns("vaep-qa-regression");

        _auditoria
            .Setup(x => x.RegistrarAsync(
                It.IsAny<ModuloSistema>(),
                It.IsAny<AccionPermiso>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public void Empresa_Es_Unica_Raiz_Canonica()
    {
        var type = typeof(Empresa);
        Assert.True(type.IsClass);
        Assert.Equal("Empresa", type.Name);
        Assert.NotEqual(typeof(EmpresaConfiguracion), type);
    }

    [Fact]
    public void Empresa_Domain_Enforces_Invariants()
    {
        var e = new Empresa("  QA  ");
        Assert.Equal("QA", e.Nombre);
        Assert.True(e.Activa);
        Assert.Throws<ArgumentException>(() => new Empresa(" "));
    }

    [Fact]
    public async Task Persistence_Can_Save_And_Retrieve_Empresa()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("N61G_Regression_Db_" + Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new AppDbContext(options);
        var repo = new EmpresaRepository(dbContext);

        var e = new Empresa("QA Persisted");
        await repo.AddAsync(e, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        Assert.NotEqual(0, e.Id);
        var fetched = await repo.GetByIdAsync(e.Id, CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal("QA Persisted", fetched!.Nombre);
    }

    [Fact]
    public async Task Application_Service_Mutates_And_Audits()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("N61G_Regression_Db_" + Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new AppDbContext(options);
        var repo = new EmpresaRepository(dbContext);
        var svc = new EmpresaService(repo, _currentUser.Object, _auditoria.Object);

        var dto = new CreateEmpresaDto { Nombre = "QA Svc" };
        var created = await svc.CreateAsync(dto);
        Assert.NotEqual(0, created.Id);

        _auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Crear,
            It.Is<string>(v => v.Contains("QA Svc")),
            It.IsAny<int?>(),
            "Empresa",
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Save_Failure_DoesNot_Emit_False_Audit_Success()
    {
        var repoMock = new Mock<IEmpresaRepository>();
        repoMock
            .Setup(x => x.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repoMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var svc = new EmpresaService(repoMock.Object, _currentUser.Object, _auditoria.Object);
        var dto = new CreateEmpresaDto { Nombre = "Failed Tx" };

        await Assert.ThrowsAsync<Solqaryn.Application.Exceptions.BusinessRuleException>(() => svc.CreateAsync(dto));

        _auditoria.Verify(x => x.RegistrarAsync(
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

    [Theory]
    [InlineData(nameof(EmpresasController.List), AccionPermiso.Ver)]
    [InlineData(nameof(EmpresasController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(EmpresasController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(EmpresasController.Update), AccionPermiso.Editar)]
    [InlineData(nameof(EmpresasController.Activar), AccionPermiso.Activar)]
    [InlineData(nameof(EmpresasController.Desactivar), AccionPermiso.Desactivar)]
    public void EmpresasController_Fail_Closed_RBAC_Configuration(string methodName, AccionPermiso expectedAction)
    {
        var controllerType = typeof(EmpresasController);

        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Empty(controllerType.GetCustomAttributes<AllowAnonymousAttribute>());

        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);
        Assert.Empty(method!.GetCustomAttributes<AllowAnonymousAttribute>());

        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);

        Assert.Equal(ModuloSistema.Configuracion, (ModuloSistema)moduloField!.GetValue(permiso)!);
        Assert.Equal(expectedAction, (AccionPermiso)accionField!.GetValue(permiso)!);
    }
}
