using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-001.F: caso medible para la matriz GET de cinco endpoints comerciales.
/// Fixture: header tenant ausente o invalido y selector de contexto unico.
/// Esperado: sin contexto unico no se ejecuta la accion ni se marca tenant;
/// con contexto unico A y permiso Ver, solo A puede quedar autorizado.
/// No equivale a una prueba HTTP/JWT ni a integracion con MySQL.
/// </summary>
public sealed class P2001FMatrizContextoUnicoTests
{
    [Theory]
    [InlineData(typeof(ClientesController), nameof(ClientesController.GetAll), ModuloSistema.Clientes)]
    [InlineData(typeof(ProductosController), nameof(ProductosController.GetPaged), ModuloSistema.Productos)]
    [InlineData(typeof(VentasController), nameof(VentasController.GetPaged), ModuloSistema.Ventas)]
    [InlineData(typeof(ProveedoresController), nameof(ProveedoresController.GetAll), ModuloSistema.Proveedores)]
    [InlineData(typeof(ComprasController), nameof(ComprasController.GetPaged), ModuloSistema.Compras)]
    public async Task P2001F_Sin_header_y_sin_contexto_unico_rechaza_los_cinco_GET(
        Type controllerType, string actionName, ModuloSistema modulo)
    {
        var filtro = ReadPermissionFilter(controllerType, actionName, modulo);
        var scopes = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scopes.Setup(s => s.ObtenerUnicoActualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);

        using var services = new ServiceCollection()
            .AddSingleton(scopes.Object)
            .AddSingleton(permisos.Object)
            .BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = HttpMethods.Get;
        // Sin X-Empresa-Id y sin membresia unica no hay tenant autorizado.
        var (execution, filters) = NewExecution(http);
        var actionCalls = 0;
        ActionExecutionDelegate next = () =>
        {
            actionCalls++;
            return Task.FromResult(new ActionExecutedContext(execution, filters, execution.Controller));
        };

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => filtro.OnActionExecutionAsync(execution, next));
        Assert.Equal(0, actionCalls);
        Assert.False(http.Items.ContainsKey(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey));
        scopes.Verify(s => s.ObtenerUnicoActualAsync(It.IsAny<CancellationToken>()), Times.Once);
        permisos.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(typeof(ClientesController), nameof(ClientesController.GetAll), ModuloSistema.Clientes)]
    [InlineData(typeof(ProductosController), nameof(ProductosController.GetPaged), ModuloSistema.Productos)]
    [InlineData(typeof(VentasController), nameof(VentasController.GetPaged), ModuloSistema.Ventas)]
    [InlineData(typeof(ProveedoresController), nameof(ProveedoresController.GetAll), ModuloSistema.Proveedores)]
    [InlineData(typeof(ComprasController), nameof(ComprasController.GetPaged), ModuloSistema.Compras)]
    public async Task P2001F_Header_invalido_recae_solo_en_empresa_A_verificada(
        Type controllerType, string actionName, ModuloSistema modulo)
    {
        var filtro = ReadPermissionFilter(controllerType, actionName, modulo);
        const int empresaA = 101;
        var scopes = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scopes.Setup(s => s.ObtenerUnicoActualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(12, empresaA, 17, "Vendedor", false));
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(p => p.VerificarPermisoAsync(empresaA, modulo, AccionPermiso.Ver))
            .Returns(Task.CompletedTask);

        using var services = new ServiceCollection()
            .AddSingleton(scopes.Object)
            .AddSingleton(permisos.Object)
            .BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = HttpMethods.Get;
        http.Request.Headers["X-Empresa-Id"] = "-202"; // No es un tenant valido.
        var (execution, filters) = NewExecution(http);
        var actionCalls = 0;
        ActionExecutionDelegate next = () =>
        {
            actionCalls++;
            return Task.FromResult(new ActionExecutedContext(execution, filters, execution.Controller));
        };

        await filtro.OnActionExecutionAsync(execution, next);
        Assert.Equal(1, actionCalls);
        Assert.Equal(empresaA,
            Assert.IsType<int>(http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey]));
        scopes.Verify(s => s.ObtenerUnicoActualAsync(It.IsAny<CancellationToken>()), Times.Once);
        permisos.Verify(p => p.VerificarPermisoAsync(empresaA, modulo, AccionPermiso.Ver), Times.Once);
        permisos.VerifyNoOtherCalls();
    }

    private static RequierePermisoAttribute ReadPermissionFilter(
        Type controllerType, string actionName, ModuloSistema modulo)
    {
        var action = controllerType.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(action);
        var filtro = Assert.IsType<RequierePermisoAttribute>(
            action!.GetCustomAttribute<RequierePermisoAttribute>());
        Assert.Equal(modulo, filtro.Modulo);
        Assert.Equal(AccionPermiso.Ver, filtro.Accion);
        return filtro;
    }

    private static (ActionExecutingContext Execution, List<IFilterMetadata> Filters) NewExecution(HttpContext http)
    {
        var context = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        return (new ActionExecutingContext(
            context, filters, new Dictionary<string, object?>(), new object()), filters);
    }
}
