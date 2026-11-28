using Solqaryn.Application.Exceptions;
using Solqaryn.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Solqaryn.Tests;

public class CloudinaryEnvironmentIsolationTests
{
    private static IConfiguration CrearConfiguracionDev()
    {
        var valores = new Dictionary<string, string?>
        {
            ["Cloudinary:CloudName"] = "solqaryn-test",
            ["Cloudinary:ApiKey"] = "test-key",
            ["Cloudinary:ApiSecret"] = "test-secret",
            ["Cloudinary:EnvironmentPrefix"] = "solqaryn_dev"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();
    }

    [Fact]
    public async Task Productos_DevNoPuedeEliminarImagenProductiva()
    {
        var service = new CloudinaryImageStorageService(CrearConfiguracionDev());

        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.DeleteAsync("solqaryn/productos/producto-productivo"));

        Assert.Contains("Producción", excepcion.Message);
    }

    [Fact]
    public async Task Compras_DevNoPuedeEliminarComprobanteProductivo()
    {
        var service = new CloudinaryCompraDocumentoStorageService(CrearConfiguracionDev());

        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.DeleteAsync("solqaryn/compras/comprobante-productivo", "raw"));

        Assert.Contains("Producción", excepcion.Message);
    }

    [Theory]
    [InlineData("ok", true)]
    [InlineData("not found", false)]
    public void Imagenes_ValidaAcuseYNoConfundeAusenciaConEliminacion(string respuesta, bool eliminado)
    {
        Assert.Equal(eliminado, CloudinaryImageStorageService.ValidarResultadoEliminacion(respuesta, false));
    }

    [Theory]
    [InlineData("ok", true)]
    [InlineData("not found", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("error", false)]
    [InlineData("OK", false)]
    public void Imagenes_RespuestaNoConfirmadaFallaCerrado(string? respuesta, bool errorProveedor)
    {
        Assert.Throws<BusinessRuleException>(() =>
            CloudinaryImageStorageService.ValidarResultadoEliminacion(respuesta, errorProveedor));
    }
}
