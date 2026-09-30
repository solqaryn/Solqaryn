using System.Reflection;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class Priority3FileSecurityTests
{
    [Fact]
    public async Task CompraDocumento_RejectsSpoofedPdfBeforeStorage()
    {
        var compraRepository = new Mock<ICompraRepository>();
        compraRepository.Setup(x => x.GetByIdAsync(7))
            .ReturnsAsync(new Compra { Id = 7, NumeroCompra = "COM-000007" });
        var documentoRepository = new Mock<ICompraDocumentoRepository>();
        var storage = new Mock<ICompraDocumentoStorageService>();
        var currentUser = new Mock<ICurrentUserService>();
        var auditoria = new Mock<IAuditoriaService>();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        httpContextAccessor.HttpContext.Request.Headers[StorageTenantContextResolver.EmpresaHeader] = "3";
        var usuarioScope = new Mock<IUsuarioScopeService>();
        usuarioScope
            .Setup(x => x.ObtenerActualAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(5, 3, 1, "Admin", true));
        var service = new CompraDocumentoService(
            compraRepository.Object,
            documentoRepository.Object,
            storage.Object,
            currentUser.Object,
            auditoria.Object,
            httpContextAccessor,
            usuarioScope.Object);

        var bytes = "<html>not-a-pdf</html>"u8.ToArray();
        await using var stream = new MemoryStream(bytes);
        IFormFile archivo = new FormFile(stream, 0, bytes.Length, "archivo", "comprobante.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.UploadAsync(7, archivo));
        Assert.Contains("contenido real", error.Message, StringComparison.OrdinalIgnoreCase);
        storage.Verify(
            x => x.UploadAsync(
                It.IsAny<StorageTenantContext>(),
                It.IsAny<IFormFile>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("http://127.0.0.1/internal", false)]
    [InlineData("https://169.254.169.254/latest/meta-data/", false)]
    [InlineData("https://res.cloudinary.com.evil.example/demo/image/upload/file.pdf", false)]
    [InlineData("https://res.cloudinary.com/other-cloud/raw/upload/file.pdf", false)]
    [InlineData("https://user@res.cloudinary.com/demo/raw/upload/file.pdf", false)]
    [InlineData("https://res.cloudinary.com/demo/raw/upload/v1/compras/file.pdf", true)]
    [InlineData("https://res.cloudinary.com/demo/image/upload/v1/compras/file.png", true)]
    public void CompraDocumentoStorage_CloudinaryUrlAllowlistIsFailClosed(string url, bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cloudinary:CloudName"] = "demo",
                ["Cloudinary:ApiKey"] = "test-key",
                ["Cloudinary:ApiSecret"] = "test-secret",
                ["Cloudinary:EnvironmentPrefix"] = "dev"
            })
            .Build();
        var service = new CloudinaryCompraDocumentoStorageService(configuration);
        var method = typeof(CloudinaryCompraDocumentoStorageService)
            .GetMethod("EsUrlCloudinaryPermitida", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var actual = (bool)method!.Invoke(service, new object[] { url })!;
        Assert.Equal(expected, actual);
    }
}
