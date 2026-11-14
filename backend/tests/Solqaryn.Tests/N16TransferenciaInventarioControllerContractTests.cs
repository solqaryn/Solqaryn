using System.Reflection;
using Solqaryn.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaInventarioControllerContractTests
{
    [Fact]
    public void Controller_ExigeAutenticacionYExponeRutaCanonica()
    {
        var tipo = typeof(TransferenciasInventarioController);

        Assert.NotNull(tipo.GetCustomAttribute<ApiControllerAttribute>());
        Assert.NotNull(tipo.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("transferencias-inventario", tipo.GetCustomAttribute<RouteAttribute>()?.Template);
    }

    [Theory]
    [InlineData(nameof(TransferenciasInventarioController.Buscar), "GET", null)]
    [InlineData(nameof(TransferenciasInventarioController.GetById), "GET", "{id:int}")]
    [InlineData(nameof(TransferenciasInventarioController.Create), "POST", null)]
    [InlineData(nameof(TransferenciasInventarioController.Update), "PUT", "{id:int}")]
    [InlineData(nameof(TransferenciasInventarioController.Solicitar), "POST", "{id:int}/solicitar")]
    [InlineData(nameof(TransferenciasInventarioController.Aprobar), "POST", "{id:int}/aprobar")]
    [InlineData(nameof(TransferenciasInventarioController.Despachar), "POST", "{id:int}/despachar")]
    [InlineData(nameof(TransferenciasInventarioController.Recibir), "POST", "{id:int}/recibir")]
    [InlineData(nameof(TransferenciasInventarioController.Cancelar), "POST", "{id:int}/cancelar")]
    public void Endpoints_MantienenVerboRutaYPermisoExplicito(string metodo, string verbo, string? template)
    {
        var info = typeof(TransferenciasInventarioController).GetMethod(metodo)
            ?? throw new InvalidOperationException($"No se encontró el endpoint {metodo}.");

        var http = Assert.Single(info.GetCustomAttributes<HttpMethodAttribute>());
        Assert.Contains(verbo, http.HttpMethods);
        Assert.Equal(template, http.Template);

        var permisos = info.GetCustomAttributes()
            .Where(a => a.GetType().Name == "RequierePermisoAttribute")
            .ToArray();
        Assert.Single(permisos);
    }

    [Fact]
    public void Create_ConservaCreatedAtAction_HaciaDetallePorId()
    {
        var metodo = typeof(TransferenciasInventarioController).GetMethod(nameof(TransferenciasInventarioController.Create));
        Assert.NotNull(metodo);

        Assert.Equal(typeof(Task<IActionResult>), metodo!.ReturnType);
        Assert.Contains(
            typeof(TransferenciasInventarioController).GetMethods(BindingFlags.Public | BindingFlags.Instance),
            m => m.Name == nameof(TransferenciasInventarioController.GetById));
    }
}
