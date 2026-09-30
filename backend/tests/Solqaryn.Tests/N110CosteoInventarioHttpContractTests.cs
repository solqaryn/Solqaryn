using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N110CosteoInventarioHttpContractTests
{
    [Fact]
    public void Controller_es_autenticado_y_usa_ruta_canonica()
    {
        var type = typeof(CosteoInventarioController);
        Assert.NotNull(type.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true).SingleOrDefault());
        Assert.NotNull(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        Assert.Equal("costeo-inventario", route.Template);
    }

    [Theory]
    [InlineData(nameof(CosteoInventarioController.GetVigente), "politica-vigente")]
    [InlineData(nameof(CosteoInventarioController.GetHistorial), "politicas")]
    [InlineData(nameof(CosteoInventarioController.GetMetodos), "metodos")]
    public void Lecturas_exigen_permiso_y_get_estable(string metodo, string template)
    {
        var method = typeof(CosteoInventarioController).GetMethod(metodo)!;
        var http = Assert.Single(method.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true).Cast<HttpGetAttribute>());
        Assert.Equal(template, http.Template);
        Assert.Single(method.GetCustomAttributes(typeof(RequierePermisoAttribute), inherit: true));
    }

    [Fact]
    public void Cambio_de_politica_es_put_idempotente_y_exige_permiso_explicito()
    {
        var method = typeof(CosteoInventarioController).GetMethod(nameof(CosteoInventarioController.Cambiar))!;
        var http = Assert.Single(method.GetCustomAttributes(typeof(HttpPutAttribute), inherit: true).Cast<HttpPutAttribute>());
        Assert.Equal("politica-vigente", http.Template);
        Assert.Single(method.GetCustomAttributes(typeof(RequierePermisoAttribute), inherit: true));
    }

    [Fact]
    public void Ningun_endpoint_publico_de_costeo_queda_sin_permiso()
    {
        var endpoints = typeof(CosteoInventarioController)
            .GetMethods()
            .Where(m => m.GetCustomAttributes(inherit: true).Any(a => a is HttpMethodAttribute))
            .ToList();

        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, method =>
            Assert.Single(method.GetCustomAttributes(typeof(RequierePermisoAttribute), inherit: true)));
    }
}
