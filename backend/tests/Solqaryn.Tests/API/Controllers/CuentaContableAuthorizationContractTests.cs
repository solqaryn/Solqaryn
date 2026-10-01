using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public class CuentaContableAuthorizationContractTests
{
    private readonly Type _controllerType = typeof(CuentaContableController);

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
        Assert.Equal("cuentas-contables", routeAttribute.Template);
    }

    [Theory]
    [InlineData(nameof(CuentaContableController.GetAll), typeof(HttpGetAttribute), ModuloSistema.Finanzas, AccionPermiso.Ver)]
    [InlineData(nameof(CuentaContableController.GetRaices), typeof(HttpGetAttribute), ModuloSistema.Finanzas, AccionPermiso.Ver)]
    [InlineData(nameof(CuentaContableController.GetById), typeof(HttpGetAttribute), ModuloSistema.Finanzas, AccionPermiso.Ver)]
    [InlineData(nameof(CuentaContableController.Create), typeof(HttpPostAttribute), ModuloSistema.Finanzas, AccionPermiso.Crear)]
    [InlineData(nameof(CuentaContableController.Update), typeof(HttpPutAttribute), ModuloSistema.Finanzas, AccionPermiso.Editar)]
    public void Endpoint_DebeTenerMetadatosDeRutaYPermisoCorrectos(
        string methodName,
        Type expectedHttpVerbAttributeType,
        ModuloSistema expectedModulo,
        AccionPermiso expectedAccion)
    {
        var methodInfo = _controllerType.GetMethod(methodName);
        Assert.NotNull(methodInfo);
        Assert.NotNull(methodInfo.GetCustomAttribute(expectedHttpVerbAttributeType));

        var permisoAttribute = methodInfo.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permisoAttribute);

        var moduloField = typeof(RequierePermisoAttribute)
            .GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute)
            .GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(expectedModulo, (ModuloSistema?)moduloField.GetValue(permisoAttribute));
        Assert.Equal(expectedAccion, (AccionPermiso?)accionField.GetValue(permisoAttribute));
    }

    [Theory]
    [InlineData(nameof(CuentaContableController.GetAll), null)]
    [InlineData(nameof(CuentaContableController.GetRaices), "raices")]
    [InlineData(nameof(CuentaContableController.GetById), "{id:int}")]
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
        var methodInfo = _controllerType.GetMethod(nameof(CuentaContableController.Create));
        Assert.NotNull(methodInfo);
        var attribute = methodInfo.GetCustomAttribute<HttpPostAttribute>();
        Assert.NotNull(attribute);
        Assert.Null(attribute.Template);
    }

    [Fact]
    public void Update_DebeTenerRutaEsperada()
    {
        var methodInfo = _controllerType.GetMethod(nameof(CuentaContableController.Update));
        Assert.NotNull(methodInfo);
        var attribute = methodInfo.GetCustomAttribute<HttpPutAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal("{id:int}", attribute.Template);
    }
}
