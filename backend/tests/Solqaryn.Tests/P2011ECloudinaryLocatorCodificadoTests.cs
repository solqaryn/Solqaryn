using Microsoft.Extensions.Configuration;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-011.E: query, fragmento y delimitadores codificados no son identidad
/// del tenant para documentos privados Cloudinary.
/// Prueba unitaria sin red, sin JWT/HTTP ni MySQL reales.
/// </summary>
public sealed class P2011ECloudinaryLocatorCodificadoTests
{
    private const string UrlPropia =
        "https://res.cloudinary.com/unit-test/raw/upload/v1/solqaryn/compras/empresas/31/comprobante.pdf";

    [Theory]
    [InlineData("https://res.cloudinary.com/unit-test/raw/upload/v1/solqaryn/compras/empresas/32/comprobante.pdf?empresa=31")]
    [InlineData("https://res.cloudinary.com/unit-test/raw/upload/v1/solqaryn/compras/empresas/32/comprobante.pdf#solqaryn/compras/empresas/31/comprobante.pdf")]
    [InlineData("https://res.cloudinary.com/unit-test/raw/upload/v1/solqaryn/compras/empresas/31-alt/comprobante.pdf")]
    [InlineData("https://res.cloudinary.com/unit-test/raw/upload/v1/solqaryn/compras/empresas/31%2Fcomprobante.pdf")]
    [InlineData("https://res.cloudinary.com/unit-test/raw/upload/v1/solqaryn/compras/empresas%2F31/comprobante.pdf")]
    [InlineData("https://res.cloudinary.com/unit-test/raw/upload/v1/comprobantes/comprobante.pdf?target=/solqaryn/compras/empresas/31/comprobante.pdf")]
    public async Task LocatorAjenoODelimitadorCodificado_RechazaAntesDeCualquierRed(
        string locator)
    {
        var servicio = CrearServicio();
        var tenant = CrearTenant(31);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servicio.DownloadAsync(tenant, locator, cts.Token));
    }

    [Fact]
    public async Task LocatorPropioValido_SuperaAlcancePeroRespetaCancelacionSinRed()
    {
        var servicio = CrearServicio();
        var tenant = CrearTenant(31);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            servicio.DownloadAsync(tenant, UrlPropia, cts.Token));
    }

    private static CloudinaryCompraDocumentoStorageService CrearServicio()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cloudinary:CloudName"] = "unit-test",
                ["Cloudinary:ApiKey"] = "unit-test-key",
                ["Cloudinary:ApiSecret"] = "unit-test-secret"
            })
            .Build();
        return new CloudinaryCompraDocumentoStorageService(config);
    }

    private static StorageTenantContext CrearTenant(int empresaId)
    {
        var membresia = new UsuarioEmpresa(7, empresaId, 4);
        var contexto = ContextoTenantActual.DesdeMembresia(membresia, 7, empresaId);
        return StorageTenantContext.Desde(contexto);
    }
}
