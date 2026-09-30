namespace Solqaryn.Application.DTOs;

public class ProductoVarianteDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int? MarcaId { get; set; }
    public string? MarcaNombre { get; set; }
    public int? ModeloId { get; set; }
    public string? ModeloNombre { get; set; }
    public int? ColorId { get; set; }
    public string? ColorNombre { get; set; }
    public string? ColorCodigoVisual { get; set; }
    public int? TallaId { get; set; }
    public string? TallaNombre { get; set; }
    public string Etiqueta { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public int Cantidad { get; set; }
    public int UmbralStockBajo { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; }
    public bool EsTecnica { get; set; }
    public bool TieneStockBajo { get; set; }
    public bool EstaAgotada { get; set; }
    public string EstadoInventario { get; set; } = string.Empty;
    public List<ProductoImagenDto> Imagenes { get; set; } = new();
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class CreateProductoVarianteDto
{
    public int? MarcaId { get; set; }
    public int? ModeloId { get; set; }
    public int? ColorId { get; set; }
    public int? TallaId { get; set; }
    public string? Sku { get; set; }
    public string? CodigoBarras { get; set; }
    public int Cantidad { get; set; }
    public int UmbralStockBajo { get; set; } = 5;
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
}

public class UpdateProductoVarianteDto : CreateProductoVarianteDto
{
}

public class CambiarEstadoProductoVarianteDto
{
    public bool Activo { get; set; }
}
