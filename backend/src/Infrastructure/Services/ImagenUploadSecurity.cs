using Solqaryn.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using SkiaSharp;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// Valida y sanitiza imágenes no confiables antes de enviarlas a almacenamiento externo.
/// Nunca confía únicamente en extensión o Content-Type declarados por el cliente.
/// </summary>
public static class ImagenUploadSecurity
{
    public const long MaximoBytes = 10L * 1024 * 1024;
    public const int MaximoDimension = 4096;
    public const long MaximoPixeles = 16_000_000;

    private enum FormatoSeguro
    {
        Jpeg,
        Png,
        Webp
    }

    public sealed class ImagenSanitizada : IDisposable
    {
        public ImagenSanitizada(MemoryStream contenido, string nombreArchivo, string contentType)
        {
            Contenido = contenido;
            NombreArchivo = nombreArchivo;
            ContentType = contentType;
        }

        public MemoryStream Contenido { get; }
        public string NombreArchivo { get; }
        public string ContentType { get; }

        public void Dispose() => Contenido.Dispose();
    }

    public static async Task<ImagenSanitizada> ProcesarAsync(
        IFormFile archivo,
        CancellationToken cancellationToken = default)
    {
        if (archivo is null || archivo.Length <= 0)
            throw new BusinessRuleException("Selecciona una imagen válida.");

        if (archivo.Length > MaximoBytes)
            throw new BusinessRuleException("La imagen no puede superar 10 MB.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp")
            throw new BusinessRuleException("La imagen debe ser JPG, JPEG, PNG o WEBP.");

        await using var original = new MemoryStream(capacity: (int)Math.Min(archivo.Length, MaximoBytes));
        await archivo.CopyToAsync(original, cancellationToken);
        if (original.Length == 0 || original.Length > MaximoBytes)
            throw new BusinessRuleException("El tamaño real de la imagen no es válido.");

        original.Position = 0;
        var cabecera = new byte[Math.Min(12, (int)original.Length)];
        _ = await original.ReadAsync(cabecera.AsMemory(0, cabecera.Length), cancellationToken);
        var formato = DetectarFormato(cabecera);
        ValidarDeclaracion(archivo.ContentType, extension, formato);

        cancellationToken.ThrowIfCancellationRequested();
        original.Position = 0;

        using var codec = SKCodec.Create(original);
        if (codec is null)
            throw new BusinessRuleException("La imagen está dañada, incompleta o utiliza un formato no permitido.");

        var formatoCodec = MapearFormato(codec.EncodedFormat);
        if (formatoCodec != formato)
            throw new BusinessRuleException("La extensión, el tipo MIME y el contenido real de la imagen no coinciden.");

        ValidarDimensiones(codec.Info.Width, codec.Info.Height);

        original.Position = 0;
        using var bitmap = SKBitmap.Decode(original);
        if (bitmap is null)
            throw new BusinessRuleException("La imagen está dañada, incompleta o utiliza un formato no permitido.");

        ValidarDimensiones(bitmap.Width, bitmap.Height);
        cancellationToken.ThrowIfCancellationRequested();

        // Re-encode desde píxeles decodificados: no se conservan EXIF/ICC/IPTC/XMP
        // del archivo de entrada antes de persistirlo externamente.
        using var imagen = SKImage.FromBitmap(bitmap);
        var (formatoSalida, calidad, contentType, extensionSalida) = CrearEncoder(formato);
        using var data = imagen.Encode(formatoSalida, calidad);
        if (data is null || data.Size <= 0)
            throw new BusinessRuleException("No fue posible sanitizar la imagen.");

        var salida = new MemoryStream(capacity: checked((int)data.Size));
        data.SaveTo(salida);
        salida.Position = 0;

        return new ImagenSanitizada(
            salida,
            $"imagen-{Guid.NewGuid():N}{extensionSalida}",
            contentType);
    }

    public static void ValidarDimensiones(int ancho, int alto)
    {
        if (ancho <= 0 || alto <= 0)
            throw new BusinessRuleException("Las dimensiones de la imagen no son válidas.");

        if (ancho > MaximoDimension || alto > MaximoDimension)
            throw new BusinessRuleException($"La imagen no puede superar {MaximoDimension} x {MaximoDimension} píxeles.");

        var pixeles = (long)ancho * alto;
        if (pixeles > MaximoPixeles)
            throw new BusinessRuleException("La imagen no puede superar 16 megapíxeles.");
    }

    private static FormatoSeguro DetectarFormato(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return FormatoSeguro.Jpeg;

        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            return FormatoSeguro.Png;
        }

        if (bytes.Length >= 12 &&
            bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
        {
            return FormatoSeguro.Webp;
        }

        throw new BusinessRuleException("La firma binaria del archivo no corresponde a una imagen JPG, PNG o WEBP permitida.");
    }

    private static void ValidarDeclaracion(string? contentType, string extension, FormatoSeguro formato)
    {
        var mimeNormalizado = contentType?.Trim().ToLowerInvariant();
        var coincide = formato switch
        {
            FormatoSeguro.Jpeg => extension is ".jpg" or ".jpeg" && mimeNormalizado == "image/jpeg",
            FormatoSeguro.Png => extension == ".png" && mimeNormalizado == "image/png",
            FormatoSeguro.Webp => extension == ".webp" && mimeNormalizado == "image/webp",
            _ => false
        };

        if (!coincide)
            throw new BusinessRuleException("La extensión, el tipo MIME y el contenido real de la imagen no coinciden.");
    }

    private static FormatoSeguro MapearFormato(SKEncodedImageFormat formato) =>
        formato switch
        {
            SKEncodedImageFormat.Jpeg => FormatoSeguro.Jpeg,
            SKEncodedImageFormat.Png => FormatoSeguro.Png,
            SKEncodedImageFormat.Webp => FormatoSeguro.Webp,
            _ => throw new BusinessRuleException("La imagen utiliza un formato no permitido.")
        };

    private static (SKEncodedImageFormat Formato, int Calidad, string ContentType, string Extension) CrearEncoder(FormatoSeguro formato) =>
        formato switch
        {
            FormatoSeguro.Jpeg => (SKEncodedImageFormat.Jpeg, 90, "image/jpeg", ".jpg"),
            FormatoSeguro.Png => (SKEncodedImageFormat.Png, 100, "image/png", ".png"),
            FormatoSeguro.Webp => (SKEncodedImageFormat.Webp, 90, "image/webp", ".webp"),
            _ => throw new InvalidOperationException("Formato de imagen no soportado.")
        };
}
