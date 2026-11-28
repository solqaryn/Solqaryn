using System.Buffers.Binary;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using SkiaSharp;
using Xunit;

namespace Solqaryn.Tests;

public class ImagenUploadSecurityTests
{
    [Theory]
    [InlineData("png", "image/png")]
    [InlineData("jpg", "image/jpeg")]
    [InlineData("webp", "image/webp")]
    public async Task ProcesarAsync_FormatoValido_RecodificaYConservaFormatoSeguro(string extension, string contentType)
    {
        var archivo = CrearImagen(32, 24, $"foto.{extension}", contentType, extension);

        using var resultado = await ImagenUploadSecurity.ProcesarAsync(archivo);

        Assert.Equal(contentType, resultado.ContentType);
        Assert.EndsWith($".{extension}", resultado.NombreArchivo, StringComparison.OrdinalIgnoreCase);
        Assert.True(resultado.Contenido.Length > 0);

        resultado.Contenido.Position = 0;
        using var codec = SKCodec.Create(resultado.Contenido);
        Assert.NotNull(codec);
        Assert.Equal(32, codec.Info.Width);
        Assert.Equal(24, codec.Info.Height);
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
        var archivo = CrearImagen(16, 16, "foto.png", "image/jpeg", "png");

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            ImagenUploadSecurity.ProcesarAsync(archivo));

        Assert.Contains("no coinciden", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesarAsync_PngTruncado_RechazaContenidoInvalido()
    {
        var archivoValido = CrearImagen(16, 16, "foto.png", "image/png", "png");
        using var original = new MemoryStream();
        await archivoValido.CopyToAsync(original);
        var bytes = original.ToArray()[..Math.Min(24, (int)original.Length)];
        var archivo = CrearArchivo(bytes, "truncado.png", "image/png");

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            ImagenUploadSecurity.ProcesarAsync(archivo));
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

    [Fact]
    public async Task ProcesarGaleriaCategoriaAsync_PngValidoDe15MiB_AceptaYEliminaMetadata()
    {
        var archivo = CrearPngConComentarios((int)ActualizarCategoriaGaleriaDto.MaxBytesPorImagen);
        Assert.Equal(ActualizarCategoriaGaleriaDto.MaxBytesPorImagen, archivo.Length);
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => ImagenUploadSecurity.ProcesarAsync(archivo));
        Assert.Contains("10 MB", error.Message);

        using var resultado = await ImagenUploadSecurity.ProcesarGaleriaCategoriaAsync(archivo);
        Assert.True(resultado.Contenido.Length < 64 * 1024);
        using var codec = SKCodec.Create(resultado.Contenido);
        Assert.NotNull(codec);
        Assert.Equal(32, codec.Info.Width);
        Assert.Equal(24, codec.Info.Height);
    }

    [Fact]
    public async Task ProcesarGaleriaCategoriaAsync_QuinceMiBMasUnByte_Rechaza()
    {
        var bytes = new byte[ActualizarCategoriaGaleriaDto.MaxBytesPorImagen + 1];
        var archivo = CrearArchivo(bytes, "foto.png", "image/png");
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            ImagenUploadSecurity.ProcesarGaleriaCategoriaAsync(archivo));
        Assert.Contains("15 MB", error.Message);
    }

    [Fact]
    public async Task ProcesarGaleriaCategoriaAsync_ReduceParaStorageSinCambiarProductos()
    {
        var archivo = CrearImagen(1200, 600, "foto.png", "image/png", "png");
        using var galeria = await ImagenUploadSecurity.ProcesarGaleriaCategoriaAsync(archivo);
        using var codecGaleria = SKCodec.Create(galeria.Contenido);
        Assert.NotNull(codecGaleria);
        Assert.Equal(800, codecGaleria.Info.Width);
        Assert.Equal(400, codecGaleria.Info.Height);

        using var producto = await ImagenUploadSecurity.ProcesarAsync(archivo);
        using var codecProducto = SKCodec.Create(producto.Contenido);
        Assert.NotNull(codecProducto);
        Assert.Equal(1200, codecProducto.Info.Width);
        Assert.Equal(600, codecProducto.Info.Height);
    }

    private static IFormFile CrearPngConComentarios(int longitud)
    {
        var basePng = CrearImagen(32, 24, "foto.png", "image/png", "png");
        using var original = new MemoryStream();
        basePng.CopyTo(original);
        var png = original.ToArray();
        using var salida = new MemoryStream(longitud);
        // Insertar chunks tEXt válidos antes de IEND; cada chunk incluye longitud, tipo y CRC.
        salida.Write(png.AsSpan(0, png.Length - 12));
        var restante = longitud - png.Length;
        byte[] tipo = "tEXt"u8.ToArray();
        byte[] longitudCrc = new byte[4];
        while (restante > 0)
        {
            var tamano = Math.Min(restante, 1024 * 1024);
            if (restante - tamano is > 0 and < 15)
                tamano -= 15 - (restante - tamano);
            var contenido = new byte[tamano - 12];
            contenido[0] = (byte)'Q';
            contenido[1] = 0;
            Array.Fill(contenido, (byte)'A', 2, contenido.Length - 2);
            BinaryPrimitives.WriteInt32BigEndian(longitudCrc, contenido.Length);
            salida.Write(longitudCrc);
            salida.Write(tipo);
            salida.Write(contenido);
            var crc = 0xffffffffu;
            foreach (var b in tipo) crc = ActualizarCrc(crc, b);
            foreach (var b in contenido) crc = ActualizarCrc(crc, b);
            BinaryPrimitives.WriteUInt32BigEndian(longitudCrc, ~crc);
            salida.Write(longitudCrc);
            restante -= tamano;
        }
        salida.Write(png.AsSpan(png.Length - 12));
        return CrearArchivo(salida.ToArray(), "foto.png", "image/png");
    }

    private static uint ActualizarCrc(uint crc, byte valor)
    {
        crc ^= valor;
        for (var bit = 0; bit < 8; bit++)
            crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320u : crc >> 1;
        return crc;
    }

    private static IFormFile CrearImagen(int ancho, int alto, string nombre, string contentType, string extension)
    {
        using var bitmap = new SKBitmap(ancho, alto, SKColorType.Rgba8888, SKAlphaType.Premul);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        var format = extension switch
        {
            "jpg" => SKEncodedImageFormat.Jpeg,
            "webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png
        };
        using var data = image.Encode(format, 90);
        Assert.NotNull(data);
        var stream = new MemoryStream(data.ToArray());
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
