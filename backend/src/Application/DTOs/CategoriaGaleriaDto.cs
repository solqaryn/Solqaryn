using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.DTOs;

public sealed record CategoriaImagenDto(string Id, string Url);
public sealed record CategoriaGaleriaDto(string Version, IReadOnlyList<CategoriaImagenDto> Imagenes);

public sealed class ActualizarCategoriaGaleriaDto
{
    // Límite individual y agregado de la galería de categorías.
    public const long MaxBytesPorImagen = 15L * 1024 * 1024;
    public const long MaxTotalBytes = MaxBytesPorImagen;
    public const long MaxRequestBytes = MaxTotalBytes + 1024 * 1024;
    public string? Version { get; set; }
    // Cada posición referencia "e:<id persistido>" o "n:<índice del archivo nuevo>".
    public List<string> Orden { get; set; } = new();
    public List<IFormFile> Archivos { get; set; } = new();
}
