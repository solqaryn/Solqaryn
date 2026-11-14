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
/// P2-011.D: borrar un comprobante privado exige el tenant verificado.
/// El token cancelado impide contactos externos y comprueba el orden del rechazo.
/// </summary>
public sealed class P2011DCloudinaryBorradoAisladoTests
{
    [Theory]
    [InlineData("solqaryn/compras/empresas/32/factura.pdf")]
    [InlineData("solqaryn/compras/empresas/310/factura.pdf")]
    [InlineData("solqaryn/compras/empresas/31-alt/factura.pdf")]
    [InlineData("solqaryn/compras/empresas/31")]
    [InlineData("solqaryn/productos/empresas/31/factura.pdf")]
    public async Task BorradoComprobante_LocatorAjenoSeRechazaAntesDeRed(
        string publicId)
    {
        var storage = CrearStorage();
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            storage.DeleteAsync(CrearTenant(31), publicId, "raw", cancelacion.Token));
    }

    [Fact]
    public async Task BorradoComprobante_DelMismoTenantPasaLocatorSinRed()
    {
        var storage = CrearStorage();
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            storage.DeleteAsync(
                CrearTenant(31),
                "solqaryn/compras/empresas/31/factura.pdf",
                "raw",
                cancelacion.Token));
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
        var tenant = ContextoTenantActual.DesdeMembresia(membresia, 7, empresaId);
        return StorageTenantContext.Desde(tenant);
    }
}
