using Solqaryn.Application.Exceptions;
using Solqaryn.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Solqaryn.Tests;

public class ImagenUploadSecurityTests
{
    [Fact]
    public async Task ProcesarAsync_PngValido_RecodificaYConservaFormatoSeguro()
    {
        var archivo = await CrearPngAsync(32, 24, "foto.png", "image/png");

        using var resultado = await ImagenUploadSecurity.ProcesarAsync(archivo);

        Assert.Equal("image/png", resultado.ContentType);
        Assert.EndsWith(".png", resultado.NombreArchivo, StringComparison.OrdinalIgnoreCase);
        Assert.True(resultado.Contenido.Length > 0);

        resultado.Contenido.Position = 0;
        var info = await Image.IdentifyAsync(resultado.Contenido);
        Assert.Equal(32, info.Width);
        Assert.Equal(24, info.Height);
    }

    [Fact]
    public async Task ProcesarAsync_EjecutableRenombradoAPng_RechazaFirmaBinaria()
    {
        var bytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
        var archivo = CrearArchivo(bytes, "malicioso.png", "image/png");

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            ImagenUploadSecurity.ProcesarAsync(archivo));

        Assert.Contains("firma", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesarAsync_ContenidoPngConMimeJpeg_RechazaInconsistencia()
    {
        var archivo = await CrearPngAsync(16, 16, "foto.png", "image/jpeg");

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            ImagenUploadSecurity.ProcesarAsync(archivo));

        Assert.Contains("no coinciden", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidarDimensiones_MayorA4096_Rechaza()
    {
        Assert.Throws<BusinessRuleException>(() =>
            ImagenUploadSecurity.ValidarDimensiones(4097, 100));
    }

    [Fact]
    public void ValidarDimensiones_MasDe16Megapixeles_Rechaza()
    {
        Assert.Throws<BusinessRuleException>(() =>
            ImagenUploadSecurity.ValidarDimensiones(4001, 4001));
    }

    [Fact]
    public void ValidarDimensiones_16MegapixelesExactos_EsValido()
    {
        var error = Record.Exception(() => ImagenUploadSecurity.ValidarDimensiones(4000, 4000));
        Assert.Null(error);
    }

    [Fact]
    public async Task ProcesarAsync_MayorA10Mb_RechazaAntesDeDecodificar()
    {
        var stream = new MemoryStream(new byte[ImagenUploadSecurity.MaximoBytes + 1]);
        var archivo = new FormFile(stream, 0, stream.Length, "archivo", "foto.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            ImagenUploadSecurity.ProcesarAsync(archivo));

        Assert.Contains("10 MB", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<IFormFile> CrearPngAsync(int ancho, int alto, string nombre, string contentType)
    {
        using var imagen = new Image<Rgba32>(ancho, alto);
        var stream = new MemoryStream();
        await imagen.SaveAsPngAsync(stream);
        stream.Position = 0;
        return new FormFile(stream, 0, stream.Length, "archivo", nombre)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static IFormFile CrearArchivo(byte[] bytes, string nombre, string contentType)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "archivo", nombre)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
