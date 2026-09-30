namespace InventoryApp.Application.DTOs;

public sealed class ProductoCatalogoReadModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public string? MarcaFallback { get; set; }
    public string? ModeloFallback { get; set; }
    public int CantidadFallback { get; set; }
    public decimal PrecioFallback { get; set; }
    public bool Activo { get; set; }
    public bool EsDestacado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? ImagenPrincipalUrl { get; set; }
    public List<ProductoImagenPublicaDto> Imagenes { get; set; } = new();
    public List<ProductoVarianteCatalogoReadModel> Variantes { get; set; } = new();
}

public sealed class ProductoVarianteCatalogoReadModel
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int? ModeloId { get; set; }
    public string? ModeloNombre { get; set; }
    public string? MarcaNombre { get; set; }
    public string? Sku { get; set; }
    public int CantidadFallback { get; set; }
    public int UmbralStockBajo { get; set; }
    public decimal Precio { get; set; }
    public List<ProductoImagenPublicaDto> Imagenes { get; set; } = new();
}
