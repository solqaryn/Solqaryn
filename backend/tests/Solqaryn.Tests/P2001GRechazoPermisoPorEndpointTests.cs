using System;
using System.Collections.Generic;
using System.Reflection;
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
/// P2-001.G: negative authorization regression for the five commercial GET endpoints.
/// A supplied positive X-Empresa-Id must not bypass an explicit RBAC denial:
/// the action cannot execute and no tenant can be marked authorized.
/// This is an action-filter test with a denying permission service, not an
/// end-to-end membership/JWT/MySQL proof.
/// </summary>
public sealed class P2001GRechazoPermisoPorEndpointTests
{
    [Theory]
    [InlineData(typeof(ClientesController), nameof(ClientesController.GetAll), ModuloSistema.Clientes)]
    [InlineData(typeof(ProductosController), nameof(ProductosController.GetPaged), ModuloSistema.Productos)]
    [InlineData(typeof(VentasController), nameof(VentasController.GetPaged), ModuloSistema.Ventas)]
    [InlineData(typeof(ProveedoresController), nameof(ProveedoresController.GetAll), ModuloSistema.Proveedores)]
    [InlineData(typeof(ComprasController), nameof(ComprasController.GetPaged), ModuloSistema.Compras)]
    public async Task Header_empresa_B_sin_permiso_de_lectura_no_ejecuta_ni_autoriza_tenant(
        Type controllerType, string actionName, ModuloSistema modulo)
    {
        const int empresaB = 202;
        var action = controllerType.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(action);
        var filtro = Assert.IsType<RequierePermisoAttribute>(
            action!.GetCustomAttribute<RequierePermisoAttribute>());
        Assert.Equal(modulo, filtro.Modulo);
        Assert.Equal(AccionPermiso.Ver, filtro.Accion);

        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(p => p.VerificarPermisoAsync(empresaB, modulo, AccionPermiso.Ver))
            .Returns(Task.FromException(new ForbiddenAccessException(
                "La identidad no tiene permiso para la empresa solicitada.")));

        using var services = new ServiceCollection()
            .AddSingleton(permisos.Object)
            .BuildServiceProvider();

        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = HttpMethods.Get;
        http.Request.Headers["X-Empresa-Id"] = empresaB.ToString();

        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var execution = new ActionExecutingContext(
            actionContext, filters, new Dictionary<string, object?>(), new object());
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
        permisos.Verify(p => p.VerificarPermisoAsync(empresaB, modulo, AccionPermiso.Ver), Times.Once);
        permisos.VerifyNoOtherCalls();
    }
}
