using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Solqaryn.Tests.Application.Storage;

public class StorageTenantContextTests
{
    [Fact]
    public void Desde_ContextoVerificado_ConstruyeScopeTenantCanonico()
    {
        var scope = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);

        Assert.Equal(31, scope.EmpresaId);
        Assert.Equal(7, scope.UsuarioId);
        Assert.Equal("empresas/31", scope.TenantPrefix);
        Assert.Equal("empresas/31/productos/imagenes", scope.ConstruirPrefijo("/productos/imagenes/"));
    }

    [Fact]
    public void Desde_SinContexto_FallaCerrado()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => StorageTenantContext.Desde((ContextoTenantActual?)null));

        Assert.Contains("contexto tenant verificado", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExigirEmpresa_ConTenantDistinto_FallaCerrado()
    {
        var scope = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);

        var exception = Assert.Throws<InvalidOperationException>(() => scope.ExigirEmpresa(32));

        Assert.Contains("no pertenece", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../otra-empresa")]
    [InlineData("productos\\imagenes")]
    public void ConstruirPrefijo_SegmentoInseguro_SeRechaza(string segmento)
    {
        var scope = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);

        Assert.Throws<ArgumentException>(() => scope.ConstruirPrefijo(segmento));
    }

    [Fact]
    public void StorageContracts_ExponenOverloadsTenantAware()
    {
        Assert.Contains(
            typeof(IImageStorageService).GetMethods(),
            method => method.Name == nameof(IImageStorageService.UploadAsync) &&
                      method.GetParameters().FirstOrDefault()?.ParameterType == typeof(StorageTenantContext));

        Assert.Contains(
            typeof(ICompraDocumentoStorageService).GetMethods(),
            method => method.Name == nameof(ICompraDocumentoStorageService.DownloadAsync) &&
                      method.GetParameters().FirstOrDefault()?.ParameterType == typeof(StorageTenantContext));
    }

    [Fact]
    public async Task ImplementacionLegacy_NoPuedeUsarOverloadTenantSinOptInExplicito()
    {
        IImageStorageService storage = new LegacyImageStorageService();
        var scope = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);
        await using var content = new MemoryStream(new byte[] { 1, 2, 3 });
        IFormFile file = new FormFile(content, 0, content.Length, "file", "producto.png");

        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => storage.UploadAsync(scope, file));

        Assert.Contains("tenant-aware", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static StorageTenantContext CrearScope(int usuarioId, int empresaId, int rolId)
    {
        var membresia = new UsuarioEmpresa(usuarioId, empresaId, rolId);
        var tenant = ContextoTenantActual.DesdeMembresia(membresia, usuarioId, empresaId);
        return StorageTenantContext.Desde(tenant);
    }

    private sealed class LegacyImageStorageService : IImageStorageService
    {
        public Task<(string Url, string PublicId)> UploadAsync(IFormFile file) =>
            Task.FromResult(("https://example.test/producto.png", "producto"));

        public Task DeleteAsync(string publicId) => Task.CompletedTask;

        public Task<(Stream Contenido, string ContentType)?> DownloadAsync(string url) =>
            Task.FromResult<(Stream Contenido, string ContentType)?>(null);
    }
}
