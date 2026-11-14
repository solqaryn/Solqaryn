using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N47FAsientosContablesSecurityAuditTests
{
    [Fact]
    public void Controller_exige_autenticacion_y_no_expone_AllowAnonymous()
    {
        var type = typeof(AsientosContablesController);
        Assert.NotEmpty(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        Assert.Empty(type.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
        Assert.All(type.GetMethods().Where(m => m.DeclaringType == type), method =>
            Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)));
    }

    [Fact]
    public void Contrato_HTTP_mantiene_ruta_canonica_y_ApiController()
    {
        var type = typeof(AsientosContablesController);
        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), inherit: true)) as RouteAttribute;
        Assert.NotNull(route);
        Assert.Equal("asientos-contables", route.Template);
        Assert.Single(type.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true));
    }

    [Fact]
    public void Todos_los_endpoints_HTTP_exigen_exactamente_un_permiso_relacional()
    {
        var type = typeof(AsientosContablesController);
        var endpoints = type.GetMethods()
            .Where(method => method.DeclaringType == type)
            .Where(method => method.GetCustomAttributes(inherit: true).Any(attribute =>
                attribute.GetType().Name.StartsWith("Http", StringComparison.Ordinal) &&
                attribute.GetType().Name.EndsWith("Attribute", StringComparison.Ordinal)))
            .ToList();
        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, method =>
            Assert.Single(method.GetCustomAttributes(typeof(RequierePermisoAttribute), inherit: true)));
    }

    [Theory]
    [InlineData(nameof(AsientosContablesController.GetAll), typeof(HttpGetAttribute), null)]
    [InlineData(nameof(AsientosContablesController.GetById), typeof(HttpGetAttribute), "{id:int}")]
    [InlineData(nameof(AsientosContablesController.Create), typeof(HttpPostAttribute), null)]
    public void Cada_endpoint_mantiene_verbo_y_ruta_estables(string metodo, Type atributoHttpEsperado, string? rutaEsperada)
    {
        var method = typeof(AsientosContablesController).GetMethod(metodo)
            ?? throw new InvalidOperationException($"No se encontró {metodo}.");
        var atributo = Assert.Single(method.GetCustomAttributes(atributoHttpEsperado, inherit: true)) as HttpMethodAttribute;
        Assert.NotNull(atributo);
        Assert.Equal(rutaEsperada, atributo.Template);
    }

    [Theory]
    [InlineData(nameof(AsientosContablesController.GetAll), AccionPermiso.Ver)]
    [InlineData(nameof(AsientosContablesController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(AsientosContablesController.Create), AccionPermiso.Crear)]
    public void Cada_endpoint_mantiene_permiso_relacional_especifico(string metodo, AccionPermiso accionEsperada)
    {
        var method = typeof(AsientosContablesController).GetMethod(metodo)
            ?? throw new InvalidOperationException($"No se encontró {metodo}.");
        var atributo = Assert.Single(method.GetCustomAttributes(typeof(RequierePermisoAttribute), inherit: true))
            as RequierePermisoAttribute;
        Assert.NotNull(atributo);
        var modulo = (ModuloSistema?)typeof(RequierePermisoAttribute)
            .GetField("_modulo", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(atributo);
        var accion = (AccionPermiso?)typeof(RequierePermisoAttribute)
            .GetField("_accion", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(atributo);
        Assert.Equal(ModuloSistema.Finanzas, modulo);
        Assert.Equal(accionEsperada, accion);
    }
}
