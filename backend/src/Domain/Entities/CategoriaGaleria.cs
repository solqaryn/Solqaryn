namespace Solqaryn.Domain.Entities;

/// <summary>Galería exclusiva de una empresa para una categoría del catálogo.</summary>
public sealed class CategoriaGaleria
{
    public int EmpresaId { get; set; }
    public int CategoriaId { get; set; }
    public string Version { get; set; } = string.Empty;
    public string ImagenesJson { get; set; } = "[]";
}
