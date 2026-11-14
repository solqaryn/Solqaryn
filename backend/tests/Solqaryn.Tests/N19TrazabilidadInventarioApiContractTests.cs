using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N19TrazabilidadInventarioApiContractTests
{
    [Fact]
    public void Controller_exige_autenticacion_y_ruta_canonica()
    {
        var tipo = typeof(TrazabilidadInventarioController);

        Assert.NotNull(tipo.GetCustomAttribute<AuthorizeAttribute>());
        var route = tipo.GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(route);
        Assert.Equal("trazabilidad-inventario", route!.Template);
    }

    [Fact]
    public void Todas_las_acciones_publicas_exigen_permiso_explicito()
    {
        var acciones = typeof(TrazabilidadInventarioController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToArray();

        Assert.NotEmpty(acciones);
        Assert.All(acciones, metodo =>
            Assert.NotNull(metodo.GetCustomAttribute<RequierePermisoAttribute>()));
    }

    [Theory]
    [InlineData(nameof(TrazabilidadInventarioController.GetLotes), "lotes")]
    [InlineData(nameof(TrazabilidadInventarioController.CrearLote), "lotes")]
    [InlineData(nameof(TrazabilidadInventarioController.GetSeries), "series")]
    [InlineData(nameof(TrazabilidadInventarioController.CrearSerie), "series")]
    public void Endpoints_principales_conservan_rutas_estables(string metodo, string plantilla)
    {
        var action = typeof(TrazabilidadInventarioController).GetMethod(metodo);
        Assert.NotNull(action);

        var http = action!.GetCustomAttributes<HttpMethodAttribute>().Single();
        Assert.Equal(plantilla, http.Template);
    }
}
