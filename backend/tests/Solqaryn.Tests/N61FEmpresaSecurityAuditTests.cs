using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.API.Middleware;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N61FEmpresaSecurityAuditTests
{
    private readonly Mock<IEmpresaRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();

    public N61FEmpresaSecurityAuditTests()
    {
        _currentUser.SetupGet(x => x.UsuarioId).Returns(41);
        _currentUser.SetupGet(x => x.NombreUsuario).Returns("vaep-n61f");
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
    public void EmpresasController_UsesCanonicalServiceInjection()
    {
        var constructor = Assert.Single(typeof(EmpresasController).GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(IEmpresaService), parameter.ParameterType);
    }

    [Fact]
    public void EmpresasController_IsFailClosed()
    {
        var controllerType = typeof(EmpresasController);

        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Empty(controllerType.GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(nameof(EmpresasController.List), AccionPermiso.Ver)]
    [InlineData(nameof(EmpresasController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(EmpresasController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(EmpresasController.Update), AccionPermiso.Editar)]
    [InlineData(nameof(EmpresasController.Activar), AccionPermiso.Activar)]
    [InlineData(nameof(EmpresasController.Desactivar), AccionPermiso.Desactivar)]
    public void EmpresaActions_RequireExactConfiguracionPermission(string methodName, AccionPermiso expectedAction)
    {
        var method = typeof(EmpresasController).GetMethod(methodName);
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

    [Fact]
    public async Task CreateAsync_AuditsSuccessfulEmpresaMutation()
    {
        _repository
            .Setup(x => x.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmpresaService(_repository.Object, _currentUser.Object, _auditoria.Object);
        await service.CreateAsync(new CreateEmpresaDto { Nombre = "Empresa segura" });

        _auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Crear,
            It.Is<string>(value => value.Contains("Empresa creada")),
            It.IsAny<int?>(),
            "Empresa",
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Theory]
    [InlineData(true, AccionPermiso.Activar)]
    [InlineData(false, AccionPermiso.Desactivar)]
    public async Task CambiarEstadoAsync_AuditsTheExactStatePermission(bool targetState, AccionPermiso expectedAction)
    {
        var empresa = new Empresa("Empresa auditada");
        if (!targetState)
            empresa.Activar();
        else
            empresa.Desactivar();

        _repository
            .Setup(x => x.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa);
        _repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new EmpresaService(_repository.Object, _currentUser.Object, _auditoria.Object);
        await service.CambiarEstadoAsync(7, targetState);

        _auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.Configuracion,
            expectedAction,
            It.IsAny<string>(),
            It.IsAny<int?>(),
            "Empresa",
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task SaveFailure_DoesNotEmitFalseAuditSuccess()
    {
        _repository
            .Setup(x => x.AddAsync(It.IsAny<Empresa>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new EmpresaService(_repository.Object, _currentUser.Object, _auditoria.Object);

        await Assert.ThrowsAsync<Solqaryn.Application.Exceptions.BusinessRuleException>(() =>
            service.CreateAsync(new CreateEmpresaDto { Nombre = "No persistida" }));

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

    [Fact]
    public async Task ExistingCorrelationMiddleware_PropagatesEmpresaRequestCorrelation()
    {
        const string correlationId = "n61f-empresa-security-001";
        string? observed = null;
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;

        var middleware = new CorrelationIdMiddleware(
            ctx =>
            {
                observed = ctx.TraceIdentifier;
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(correlationId, observed);
        Assert.Equal(correlationId, context.Items[CorrelationIdMiddleware.ItemKey]);
        Assert.Equal(correlationId, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }
}
