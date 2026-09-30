using Solqaryn.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public class N17ConteosInventarioControllerHttpContractTests
{
    [Theory]
    [InlineData(nameof(ConteosInventarioController.Buscar), typeof(HttpGetAttribute), null)]
    [InlineData(nameof(ConteosInventarioController.GetById), typeof(HttpGetAttribute), "{id:int}")]
    [InlineData(nameof(ConteosInventarioController.Create), typeof(HttpPostAttribute), null)]
    [InlineData(nameof(ConteosInventarioController.Update), typeof(HttpPutAttribute), "{id:int}")]
    [InlineData(nameof(ConteosInventarioController.Iniciar), typeof(HttpPostAttribute), "{id:int}/iniciar")]
    [InlineData(nameof(ConteosInventarioController.Capturar), typeof(HttpPutAttribute), "{id:int}/detalles/{detalleId:int}/captura")]
    [InlineData(nameof(ConteosInventarioController.CapturarLote), typeof(HttpPutAttribute), "{id:int}/detalles/captura-lote")]
    [InlineData(nameof(ConteosInventarioController.Cerrar), typeof(HttpPostAttribute), "{id:int}/cerrar")]
    [InlineData(nameof(ConteosInventarioController.Aprobar), typeof(HttpPostAttribute), "{id:int}/aprobar")]
    [InlineData(nameof(ConteosInventarioController.GenerarAjuste), typeof(HttpPostAttribute), "{id:int}/generar-ajuste")]
    [InlineData(nameof(ConteosInventarioController.Cancelar), typeof(HttpPostAttribute), "{id:int}/cancelar")]
    public void Endpoints_PreservanVerboYRutaCanonicos(string metodo, Type atributoEsperado, string? plantillaEsperada)
    {
        var methodInfo = typeof(ConteosInventarioController).GetMethod(metodo);
        Assert.NotNull(methodInfo);

        var atributo = Assert.Single(methodInfo!.GetCustomAttributes(inherit: true)
            .Where(atributoEsperado.IsInstanceOfType));

        var plantilla = atributo switch
        {
            HttpGetAttribute http => http.Template,
            HttpPostAttribute http => http.Template,
            HttpPutAttribute http => http.Template,
            _ => throw new InvalidOperationException($"Atributo HTTP no soportado: {atributo.GetType().Name}")
        };

        Assert.Equal(plantillaEsperada, plantilla);
    }

    [Fact]
    public void Controller_PreservaRutaBaseCanonica()
    {
        var ruta = Assert.Single(typeof(ConteosInventarioController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>());

        Assert.Equal("conteos-inventario", ruta.Template);
    }
}
