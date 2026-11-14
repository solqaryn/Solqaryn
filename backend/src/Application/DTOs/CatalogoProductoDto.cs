namespace Solqaryn.Application.DTOs;

public class CatalogoProductoDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CodigoVisual { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; }
    public int? CatalogoPadreId { get; set; }
    public string? CatalogoPadreNombre { get; set; }
    public int TotalProductos { get; set; }
    public int TotalModelos { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public string? ActualizadoPorNombreUsuario { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class CreateCatalogoProductoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CodigoVisual { get; set; }
    public int Orden { get; set; }
    public int? CatalogoPadreId { get; set; }
}

public class UpdateCatalogoProductoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CodigoVisual { get; set; }
    public int Orden { get; set; }
    public int? CatalogoPadreId { get; set; }
}
