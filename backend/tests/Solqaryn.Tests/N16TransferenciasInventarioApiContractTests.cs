using Solqaryn.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciasInventarioApiContractTests
{
    [Fact]
    public void Controller_ExponeRutaEmpresarialYExigeAutenticacion()
    {
        var type = typeof(TransferenciasInventarioController);
        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());

        Assert.Equal("transferencias-inventario", route.Template);
        Assert.NotEmpty(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
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
    public void Controller_ConservaContratoHttpEsperado(string methodName, string verb, string? template)
    {
        var method = typeof(TransferenciasInventarioController).GetMethod(methodName);
        Assert.NotNull(method);

        var http = Assert.Single(method!.GetCustomAttributes(inherit: true).OfType<HttpMethodAttribute>());
        Assert.Contains(verb, http.HttpMethods);
        Assert.Equal(template, http.Template);
    }
}
