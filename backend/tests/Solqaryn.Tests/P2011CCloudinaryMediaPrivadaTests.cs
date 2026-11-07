using Microsoft.Extensions.Configuration;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-011.C: un locator privado de comprobante Cloudinary no debe cruzar
/// el limite del tenant ni provocar una llamada HTTP antes del rechazo.
/// </summary>
public sealed class P2011CCloudinaryMediaPrivadaTests
{
    private const string UrlPropia =
        "https://res.cloudinary.com/unit-test/raw/upload/v1/solqaryn/compras/empresas/31/factura.pdf";

    [Theory]
    [InlineData("https://res.cloudinary.com/unit-test/raw/upload/v1/solqaryn/compras/empresas/32/factura.pdf")]
    [InlineData("https://res.cloudinary.com/unit-test/raw/upload/v1/solqaryn/compras/empresas/310/factura.pdf")]
    [InlineData("https://res.cloudinary.com.atacante.test/unit-test/raw/upload/v1/solqaryn/compras/empresas/31/factura.pdf")]
    [InlineData("http://res.cloudinary.com/unit-test/raw/upload/v1/solqaryn/compras/empresas/31/factura.pdf")]
    [InlineData("https://intruso@res.cloudinary.com/unit-test/raw/upload/v1/solqaryn/compras/empresas/31/factura.pdf")]
    public async Task DescargaDeComprobantePrivado_LocatorNoAutorizadoSeRechazaAntesDeRed(
        string locator)
    {
        var storage = CrearStorage();
        var tenantVerificado = CrearTenant(31);
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            storage.DownloadAsync(tenantVerificado, locator, cancelacion.Token));
    }

    [Fact]
    public async Task DescargaDeComprobantePrivado_LocatorMismoTenantPasaValidacionSinRed()
    {
        var storage = CrearStorage();
        var tenantVerificado = CrearTenant(31);
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        // El token se verifica despues del locator; la cancelacion impide el HTTP.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            storage.DownloadAsync(tenantVerificado, UrlPropia, cancelacion.Token));
    }

    private static CloudinaryCompraDocumentoStorageService CrearStorage()
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
        var tenantVerificado = ContextoTenantActual.DesdeMembresia(membresia, 7, empresaId);
        return StorageTenantContext.Desde(tenantVerificado);
    }
}
