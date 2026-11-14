using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

public sealed class DevCloudinaryLegacyLocatorTests
{
    private const string OldId = "solqaryn_dev/solqaryn_old/productos/empresas/1/foto1";
    private const string OldUrl = "https://res.cloudinary.com/riyrzmob/image/upload/v12345678/solqaryn_dev/solqaryn_old/productos/empresas/1/foto1.webp";
    private const string CurrentId = "solqaryn_dev/solqaryn/productos/empresas/1/normalizada-9-abcdef012345";
    private const string CurrentUrl = "https://res.cloudinary.com/riyrzmob/image/upload/v12345679/solqaryn_dev/solqaryn/productos/empresas/1/normalizada-9-abcdef012345.webp";

    [Fact]
    public void LocatorHistoricoExclusivoDevAceptaLectura_YNoEsCanonico()
    {
        Assert.True(DevCloudinaryLegacyLocator.TryValidate(OldUrl, OldId, 1, out var extension, out var canonical));
        Assert.Equal(".webp", extension);
        Assert.False(canonical);
    }

    [Fact]
    public void LocatorNuevoValidaFolderEmpresaExacto()
    {
        Assert.True(DevCloudinaryLegacyLocator.TryValidate(CurrentUrl, CurrentId, 1, out var extension, out var canonical));
        Assert.True(canonical);
        Assert.Equal(".webp", extension);
    }

    [Theory]
    [InlineData(2, "https://res.cloudinary.com/riyrzmob/image/upload/v12345678/solqaryn_dev/solqaryn_old/productos/empresas/1/foto1.webp",
        "solqaryn_dev/solqaryn_old/productos/empresas/1/foto1")]
    [InlineData(1, "https://res.cloudinary.com/riyrzmob/image/upload/v12345678/solqaryn_prod/solqaryn_old/productos/empresas/1/foto1.webp",
        "solqaryn_prod/solqaryn_old/productos/empresas/1/foto1")]
    [InlineData(1, "https://res.cloudinary.com/other/image/upload/v12345678/solqaryn_dev/solqaryn_old/productos/empresas/1/foto1.webp",
        "solqaryn_dev/solqaryn_old/productos/empresas/1/foto1")]
    [InlineData(1, "https://res.cloudinary.com/riyrzmob/image/upload/v12345678/solqaryn_dev/solqaryn_old/productos/empresas/1/foto1.webp?size=2",
        "solqaryn_dev/solqaryn_old/productos/empresas/1/foto1")]
    [InlineData(1, "https://res.cloudinary.com/riyrzmob/image/upload/v12345678/solqaryn_dev/solqaryn_old/productos/empresas/1/foto1.webp",
        "solqaryn_dev/solqaryn_old/productos/empresas/2/foto1")]
    [InlineData(1, "https://res.cloudinary.com/riyrzmob/image/upload/v12345678/solqaryn_dev/%2e%2e/productos/empresas/1/foto1.webp",
        "solqaryn_dev/../productos/empresas/1/foto1")]
    public void RechazaEntornoCloudTenantQueryYTaversal(
        int tenant, string url, string publicId)
    {
        Assert.False(DevCloudinaryLegacyLocator.TryValidate(url, publicId, tenant, out _, out _));
    }

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".png")]
    [InlineData(".webp")]
    public void SoloAdmiteFormatosEsperados(string extension)
    {
        var url = OldUrl.Replace(".webp", extension);
        Assert.True(DevCloudinaryLegacyLocator.TryValidate(url, OldId, 1, out var received, out _));
        Assert.Equal(extension, received);
    }

    [Fact]
    public void FirmasBinariasEstrictasRechazanSuplantaciones()
    {
        Assert.True(DevCloudinaryLegacyLocator.MatchesImageSignature(
            new byte[] { 0xFF, 0xD8, 0xFF, 0, 1, 2, 3, 4, 5, 6, 7, 8 }, ".jpeg"));
        Assert.True(DevCloudinaryLegacyLocator.MatchesImageSignature(
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 1 }, ".png"));
        Assert.True(DevCloudinaryLegacyLocator.MatchesImageSignature(
            new byte[] { 82, 73, 70, 70, 0, 0, 0, 0, 87, 69, 66, 80 }, ".webp"));
        Assert.False(DevCloudinaryLegacyLocator.MatchesImageSignature(
            new byte[] { 0xFF, 0xD8, 0xFF, 0, 1, 2, 3, 4, 5, 6, 7, 8 }, ".png"));
        Assert.False(DevCloudinaryLegacyLocator.MatchesImageSignature(
            new byte[] { 82, 73, 70, 70, 0, 0, 0, 0, 87, 69, 66, 80 }, ".jpg"));
    }
}
