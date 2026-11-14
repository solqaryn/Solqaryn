using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public class CuentaBancariaAuthorizationContractTests
{
    private readonly Type _controllerType = typeof(CuentaBancariaController);

    [Fact]
    public void Controller_DebeRequerirAutenticacion()
    {
        var authorizeAttribute = _controllerType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttribute);
    }

    [Fact]
    public void Controller_DebeTenerRutaBase()
    {
        var routeAttribute = _controllerType.GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(routeAttribute);
        Assert.Equal("cuentas-bancarias", routeAttribute.Template);
    }

    [Theory]
    [InlineData(nameof(CuentaBancariaController.GetAll), typeof(HttpGetAttribute), ModuloSistema.Finanzas, AccionPermiso.Ver)]
    [InlineData(nameof(CuentaBancariaController.GetActivas), typeof(HttpGetAttribute), ModuloSistema.Finanzas, AccionPermiso.Ver)]
    [InlineData(nameof(CuentaBancariaController.GetById), typeof(HttpGetAttribute), ModuloSistema.Finanzas, AccionPermiso.Ver)]
    [InlineData(nameof(CuentaBancariaController.Create), typeof(HttpPostAttribute), ModuloSistema.Finanzas, AccionPermiso.Crear)]
    [InlineData(nameof(CuentaBancariaController.Activar), typeof(HttpPatchAttribute), ModuloSistema.Finanzas, AccionPermiso.Activar)]
    [InlineData(nameof(CuentaBancariaController.Desactivar), typeof(HttpPatchAttribute), ModuloSistema.Finanzas, AccionPermiso.Desactivar)]
    public void Endpoint_DebeTenerMetadatosDeRutaYPermisoCorrectos(string methodName, Type expectedHttpVerbAttributeType, ModuloSistema expectedModulo, AccionPermiso expectedAccion)
    {
        var methodInfo = _controllerType.GetMethod(methodName);
        Assert.NotNull(methodInfo);

        Assert.NotNull(methodInfo.GetCustomAttribute(expectedHttpVerbAttributeType));

        var permisoAttribute = methodInfo.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permisoAttribute);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);

        Assert.Equal(expectedModulo, (ModuloSistema?)moduloField.GetValue(permisoAttribute));
        Assert.Equal(expectedAccion, (AccionPermiso?)accionField.GetValue(permisoAttribute));
    }

    [Theory]
    [InlineData(nameof(CuentaBancariaController.GetAll), null)]
    [InlineData(nameof(CuentaBancariaController.GetActivas), "activas")]
    [InlineData(nameof(CuentaBancariaController.GetById), "{id:int}")]
    public void GetEndpoints_DebenTenerRutaEsperada(string methodName, string? expectedTemplate)
    {
        var methodInfo = _controllerType.GetMethod(methodName);
        Assert.NotNull(methodInfo);
        var attribute = methodInfo.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(expectedTemplate, attribute.Template);
    }

    [Fact]
    public void Create_DebeTenerVerboHttpPostSinRutaAdicional()
    {
        var methodInfo = _controllerType.GetMethod(nameof(CuentaBancariaController.Create));
        Assert.NotNull(methodInfo);
        var attribute = methodInfo.GetCustomAttribute<HttpPostAttribute>();
        Assert.NotNull(attribute);
        Assert.Null(attribute.Template);
    }

    [Theory]
    [InlineData(nameof(CuentaBancariaController.Activar), "{id:int}/activar")]
    [InlineData(nameof(CuentaBancariaController.Desactivar), "{id:int}/desactivar")]
    public void PatchEndpoints_DebenTenerRutaEsperada(string methodName, string expectedTemplate)
    {
        var methodInfo = _controllerType.GetMethod(methodName);
        Assert.NotNull(methodInfo);
        var attribute = methodInfo.GetCustomAttribute<HttpPatchAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(expectedTemplate, attribute.Template);
    }
}
