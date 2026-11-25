using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.DTOs;

public sealed record CategoriaImagenDto(string Id, string Url);
public sealed record CategoriaGaleriaDto(string Version, IReadOnlyList<CategoriaImagenDto> Imagenes);

public sealed class ActualizarCategoriaGaleriaDto
{
    public string? Version { get; set; }
    // Cada posición referencia "e:<id persistido>" o "n:<índice del archivo nuevo>".
    public List<string> Orden { get; set; } = new();
    public List<IFormFile> Archivos { get; set; } = new();
}
