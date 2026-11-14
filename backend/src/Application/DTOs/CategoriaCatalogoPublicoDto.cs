namespace Solqaryn.Application.DTOs;

/// <summary>
/// Contrato minimo y seguro de categoria para navegacion publica.
/// El conteo es nullable: no se publica un cero ficticio cuando la consulta
/// de categorias activas no carga el conjunto de productos.
/// </summary>
public sealed class CategoriaCatalogoPublicoDto
{
    public int Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public int? TotalProductos { get; init; }
}
