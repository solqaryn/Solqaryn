using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.API.Middleware;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
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

public sealed class N63FSucursalesEmpresaSecurityAuditTests
{
    private readonly Mock<ISucursalRepository> _repository = new();
    private readonly Mock<IEmpresaRepository> _empresas = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();

    public N63FSucursalesEmpresaSecurityAuditTests()
    {
        _empresas.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new Empresa($"Empresa {id}") { Id = id });
        _currentUser.SetupGet(x => x.UsuarioId).Returns(63);
        _currentUser.SetupGet(x => x.NombreUsuario).Returns("vaep-n63f");
        _auditoria.Setup(x => x.RegistrarAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    private SucursalService CreateService() =>
        new(_repository.Object, _empresas.Object, _currentUser.Object, _auditoria.Object);

    [Fact]
    public void Controller_IsFailClosed_AndUsesCanonicalServiceInjection()
    {
        Assert.NotNull(typeof(SucursalesController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.Empty(typeof(SucursalesController).GetCustomAttributes<AllowAnonymousAttribute>());
        var parameter = Assert.Single(Assert.Single(typeof(SucursalesController).GetConstructors()).GetParameters());
        Assert.Equal(typeof(ISucursalService), parameter.ParameterType);
    }

    [Theory]
    [InlineData(nameof(SucursalesController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(SucursalesController.GetActivas), AccionPermiso.Ver)]
    [InlineData(nameof(SucursalesController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(SucursalesController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(SucursalesController.Update), AccionPermiso.Editar)]
    [InlineData(nameof(SucursalesController.Activar), AccionPermiso.Activar)]
    [InlineData(nameof(SucursalesController.Desactivar), AccionPermiso.Desactivar)]
    [InlineData(nameof(SucursalesController.Delete), AccionPermiso.EliminarLogico)]
    public void Actions_RequireExactSucursalPermission(string methodName, AccionPermiso expectedAction)
    {
        var method = typeof(SucursalesController).GetMethod(methodName);
        Assert.NotNull(method);
        Assert.Empty(method!.GetCustomAttributes<AllowAnonymousAttribute>());
        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);
        var modulo = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accion = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Equal(ModuloSistema.Sucursales, (ModuloSistema)modulo!.GetValue(permiso)!);
        Assert.Equal(expectedAction, (AccionPermiso)accion!.GetValue(permiso)!);
    }

    [Fact]
    public async Task UpdateAsync_AuditsOwnerReassignmentWithPreviousAndNewTenant()
    {
        var sucursal = new Sucursal { Id = 9, EmpresaId = 7, Codigo = "NORTE", Nombre = "Norte", ZonaHoraria = "America/Tegucigalpa", Activa = true };
        _repository.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(sucursal);
        _repository.Setup(x => x.ExisteCodigoAsync("CENTRO", 42, 9)).ReturnsAsync(false);
        _repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        await CreateService().UpdateAsync(9, new UpdateSucursalDto
        {
            EmpresaId = 42, Codigo = "centro", Nombre = "Centro", ZonaHoraria = "America/Tegucigalpa"
        });

        _auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.Sucursales, AccionPermiso.Editar,
            It.Is<string>(value => value.Contains("reasignación EmpresaId 7->42")),
            9, "Sucursal",
            It.Is<object>(value => value.ToString()!.Contains("EmpresaId = 7")),
            It.Is<object>(value => value.ToString()!.Contains("EmpresaId = 42")),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreateSaveFailure_DoesNotEmitFalseAuditSuccess()
    {
        _repository.Setup(x => x.ExisteCodigoAsync("CENTRO", 42, null)).ReturnsAsync(false);
        _repository.Setup(x => x.AddAsync(It.IsAny<Sucursal>())).Returns(Task.CompletedTask);
        _repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(false);

        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateAsync(new CreateSucursalDto
        {
            EmpresaId = 42, Codigo = "centro", Nombre = "Centro", ZonaHoraria = "America/Tegucigalpa"
        }));

        _auditoria.Verify(x => x.RegistrarAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task ExistingCorrelationMiddleware_PropagatesSucursalRequestCorrelation()
    {
        const string correlationId = "n63f-sucursal-security-001";
        string? observed = null;
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;
        var middleware = new CorrelationIdMiddleware(ctx => { observed = ctx.TraceIdentifier; return Task.CompletedTask; },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(correlationId, observed);
        Assert.Equal(correlationId, context.Items[CorrelationIdMiddleware.ItemKey]);
        Assert.Equal(correlationId, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }
}
