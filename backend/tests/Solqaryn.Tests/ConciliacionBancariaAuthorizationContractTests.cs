using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public class ConciliacionBancariaAuthorizationContractTests
{
    private readonly Type _controllerType = typeof(ConciliacionBancariaController);

    [Fact]
    public void Controller_DebeRequerirAutenticacion()
    {
        Assert.NotNull(_controllerType.GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void Controller_DebeTenerRutaBase()
    {
        var routeAttribute = _controllerType.GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(routeAttribute);
        Assert.Equal("conciliaciones-bancarias", routeAttribute.Template);
    }

    [Theory]
    [InlineData(nameof(ConciliacionBancariaController.ImportarEstadoCuenta), ModuloSistema.Finanzas, AccionPermiso.Importar, "importaciones-estado-cuenta")]
    [InlineData(nameof(ConciliacionBancariaController.ConciliarMovimientos), ModuloSistema.Finanzas, AccionPermiso.Crear, "matches")]
    [InlineData(nameof(ConciliacionBancariaController.SolicitarAjuste), ModuloSistema.Finanzas, AccionPermiso.Crear, "ajustes")]
    [InlineData(nameof(ConciliacionBancariaController.CerrarPeriodo), ModuloSistema.Finanzas, AccionPermiso.Cerrar, "cierre-periodo")]
    [InlineData(nameof(ConciliacionBancariaController.ReabrirPeriodo), ModuloSistema.Finanzas, AccionPermiso.Reabrir, "reapertura-periodo")]
    public void Endpoint_DebeTenerMetadatosDeRutaYPermisoCorrectos(string methodName, ModuloSistema expectedModulo, AccionPermiso expectedAccion, string expectedTemplate)
    {
        var methodInfo = _controllerType.GetMethod(methodName);
        Assert.NotNull(methodInfo);

        var httpPost = methodInfo.GetCustomAttribute<HttpPostAttribute>();
        Assert.NotNull(httpPost);
        Assert.Equal(expectedTemplate, httpPost.Template);

        var permiso = methodInfo.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);
        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(expectedModulo, (ModuloSistema?)moduloField.GetValue(permiso));
        Assert.Equal(expectedAccion, (AccionPermiso?)accionField.GetValue(permiso));
    }
}
