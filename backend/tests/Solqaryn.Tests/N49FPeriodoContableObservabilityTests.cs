using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N49FPeriodoContableObservabilityTests
{
    [Fact]
    public void PeriodosContablesController_RequireAuthorizeAttribute()
    {
        var controllerType = typeof(PeriodosContablesController);
        var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttribute);
    }

    [Fact]
    public void PeriodosContablesController_HasRouteAttribute()
    {
        var controllerType = typeof(PeriodosContablesController);
        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(routeAttribute);
        Assert.Equal("api/periodos-contables", routeAttribute.Template);
    }

    [Theory]
    [InlineData(nameof(PeriodosContablesController.GetAll), AccionPermiso.Ver)]
    [InlineData(nameof(PeriodosContablesController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(PeriodosContablesController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(PeriodosContablesController.Cerrar), AccionPermiso.Cerrar)]
    public void Acciones_RequierenPermisoConfiguracion_Correcto(string methodName, AccionPermiso accionEsperada)
    {
        var methodInfo = typeof(PeriodosContablesController).GetMethod(methodName);
        Assert.NotNull(methodInfo);

        var requiresPermisoAttribute = methodInfo.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(requiresPermisoAttribute);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);

        var modulo = (ModuloSistema)moduloField.GetValue(requiresPermisoAttribute)!;
        var accion = (AccionPermiso)accionField.GetValue(requiresPermisoAttribute)!;

        Assert.Equal(ModuloSistema.Configuracion, modulo);
        Assert.Equal(accionEsperada, accion);
    }

    [Fact]
    public async Task Correlation_id_se_propaga_correctamente_en_middleware()
    {
        const string esperado = "req-PeriodoContable-123";
        string? observadoEnPipeline = null;

        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = $"  {esperado}  ";
        var middleware = new CorrelationIdMiddleware(
            next: ctx =>
            {
                observadoEnPipeline = ctx.TraceIdentifier;
                Assert.Equal(esperado, ctx.Items[CorrelationIdMiddleware.ItemKey]);
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(esperado, observadoEnPipeline);
        Assert.Equal(esperado, context.TraceIdentifier);
        Assert.Equal(esperado, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task Exception_middleware_returns_bad_request_for_argument_exception()
    {
        var mockService = new Mock<IPeriodoContableService>();
        mockService.Setup(s => s.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new ArgumentException("Periodo ID inválido."));

        var middleware = new ExceptionHandlingMiddleware(
            next: async ctx =>
            {
                await mockService.Object.GetByIdAsync(1);
            },
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("Periodo ID inv\\u00E1lido.", responseBody);
    }
}
