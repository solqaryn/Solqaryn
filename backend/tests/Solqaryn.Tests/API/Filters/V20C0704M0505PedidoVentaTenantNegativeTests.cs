using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using Solqaryn.API.Filters;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.API.Filters;

/// <summary>
/// C07-04.M05.05: ejercita el filtro real de permisos de pedidos.
/// No sustituye middleware JWT ni membresías MySQL reales.
/// </summary>
public sealed class V20C0704M0505PedidoVentaTenantNegativeTests
{
    private static readonly RequierePermisoAttribute FiltroCrear =
        new(ModuloSistema.Ventas, AccionPermiso.Crear);

    [Theory]
    [InlineData("0")]
    [InlineData("-71")]
    [InlineData("otro-tenant")]
    [InlineData("71,72")]
    [InlineData("")]
    public async Task SelectorDeTenantInvalidoFallaAntesDePermisosYDeAccion(string selector)
    {
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        using var services = Servicios(permisos, scope);
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = HttpMethods.Post;
        http.Request.Headers["X-Empresa-Id"] = selector;

        var (context, next, called) = Preparar(http);
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => FiltroCrear.OnActionExecutionAsync(context, next));

        Assert.Equal(0, called());
        permisos.VerifyNoOtherCalls();
        scope.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DosSelectorsNoPuedenAutorizarUnaAccionComercial()
    {
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        using var services = Servicios(permisos, scope);
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = HttpMethods.Post;
        http.Request.Headers["X-Empresa-Id"] = new StringValues(new[] { "71", "72" });

        var (context, next, called) = Preparar(http);
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => FiltroCrear.OnActionExecutionAsync(context, next));
        Assert.Equal(0, called());
        permisos.VerifyNoOtherCalls();
        scope.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SinEmpresaNiMembresiaUnicaSeRechazaSinConsultarPermisos()
    {
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerUnicoActualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);
        using var services = Servicios(permisos, scope);
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = HttpMethods.Post;

        var (context, next, called) = Preparar(http);
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => FiltroCrear.OnActionExecutionAsync(context, next));

        Assert.Equal(0, called());
        permisos.VerifyNoOtherCalls();
        scope.Verify(x => x.ObtenerUnicoActualAsync(It.IsAny<CancellationToken>()), Times.Once);
        scope.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TenantSeleccionadoSinGrantNoEjecutaLaAccionDePedido()
    {
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(x => x.VerificarPermisoAsync(72, ModuloSistema.Ventas, AccionPermiso.Crear))
            .Returns(Task.FromException(new ForbiddenAccessException("La membresia no autoriza esta accion.")));
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        using var services = Servicios(permisos, scope);
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = HttpMethods.Post;
        http.Request.Headers["X-Empresa-Id"] = "72";

        var (context, next, called) = Preparar(http);
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => FiltroCrear.OnActionExecutionAsync(context, next));

        Assert.Equal(0, called());
        permisos.Verify(x => x.VerificarPermisoAsync(72, ModuloSistema.Ventas, AccionPermiso.Crear), Times.Once);
        permisos.VerifyNoOtherCalls();
        scope.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TenantConGrantVerificadoPermiteUnaSolaAccion()
    {
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(x => x.VerificarPermisoAsync(71, ModuloSistema.Ventas, AccionPermiso.Crear))
            .Returns(Task.CompletedTask);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        using var services = Servicios(permisos, scope);
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = HttpMethods.Post;
        http.Request.Headers["X-Empresa-Id"] = "71";

        var (context, next, called) = Preparar(http);
        await FiltroCrear.OnActionExecutionAsync(context, next);

        Assert.Equal(1, called());
        permisos.Verify(x => x.VerificarPermisoAsync(71, ModuloSistema.Ventas, AccionPermiso.Crear), Times.Once);
        permisos.VerifyNoOtherCalls();
        scope.VerifyNoOtherCalls();
    }

    private static ServiceProvider Servicios(
        Mock<IPermisoService> permisos,
        Mock<IUsuarioScopeService> scope) =>
        new ServiceCollection()
            .AddSingleton(permisos.Object)
            .AddSingleton(scope.Object)
            .BuildServiceProvider();

    private static (ActionExecutingContext context, ActionExecutionDelegate next, Func<int> called)
        Preparar(HttpContext http)
    {
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var controller = new object();
        var context = new ActionExecutingContext(
            action, filters, new Dictionary<string, object?>(), controller);
        var calls = 0;
        ActionExecutionDelegate next = () =>
        {
            calls++;
            return Task.FromResult(new ActionExecutedContext(action, filters, controller));
        };
        return (context, next, () => calls);
    }
}
