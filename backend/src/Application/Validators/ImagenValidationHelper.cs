using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Validators;

public static class ImagenValidationHelper
{
    public const int MaxImagenes = 5;
    public const long MaxBytesPorImagen = 10L * 1024 * 1024; // 10 MB; validación profunda en Infraestructura.

    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    /// <summary>
    /// Prevalidación barata del contrato HTTP. La validación de seguridad real
    /// (magic numbers, dimensiones, megapíxeles, decodificación y recodificación)
    /// se ejecuta en Infraestructura inmediatamente antes de Cloudinary.
    /// </summary>
    public static bool EsImagenValida(IFormFile archivo)
    {
        if (archivo is null || archivo.Length <= 0 || archivo.Length > MaxBytesPorImagen)
            return false;

        var extension = Path.GetExtension(archivo.FileName);
        return TiposPermitidos.Contains(archivo.ContentType ?? string.Empty) &&
               ExtensionesPermitidas.Contains(extension);
    }
}
