using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public class ThreeWayMatchSecurityObservabilityTests
{
    [Fact]
    public void ConciliacionController_RequireAuthorizeAttribute()
    {
        var controllerType = typeof(ConciliacionController);
        var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttribute);
    }

    [Fact]
    public void ConciliacionController_HasRouteAttribute()
    {
        var controllerType = typeof(ConciliacionController);
        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(routeAttribute);
        Assert.Equal("conciliacion", routeAttribute.Template);
    }

    [Fact]
    public void EvaluarThreeWayMatch_RequirePermisoAttribute()
    {
        var controllerType = typeof(ConciliacionController);
        var methodInfo = controllerType.GetMethod(nameof(ConciliacionController.EvaluarThreeWayMatch));
        Assert.NotNull(methodInfo);
        var requiresPermisoAttribute = methodInfo.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(requiresPermisoAttribute);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);

        var modulo = (ModuloSistema)moduloField.GetValue(requiresPermisoAttribute)!;
        var accion = (AccionPermiso)accionField.GetValue(requiresPermisoAttribute)!;
        Assert.Equal(ModuloSistema.Compras, modulo);
        Assert.Equal(AccionPermiso.Ver, accion);
    }

    [Fact]
    public void EvaluarThreeWayMatch_HasHttpGetAttribute()
    {
        var controllerType = typeof(ConciliacionController);
        var methodInfo = controllerType.GetMethod(nameof(ConciliacionController.EvaluarThreeWayMatch));
        Assert.NotNull(methodInfo);
        var httpGetAttribute = methodInfo.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(httpGetAttribute);
        Assert.Equal("ordenes-compra/{ordenCompraId:int}/three-way-match", httpGetAttribute.Template);
    }
}
