using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.DTOs;

/// <summary>
/// Unidad exacta de inventario capturada dentro del formulario principal.
/// ProductoVariante es la única autoridad de SKU, barcode, stock, costo, precio, umbral y dimensiones.
/// </summary>
public class ProductoVarianteFormularioDto
{
    public int? Id { get; set; }
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
    public bool Activo { get; set; } = true;
}

public class CreateProductoDto
{
    public string Nombre { get; set; } = string.Empty;

    // Compatibilidad de entrada para clientes anteriores. Estos campos NO son fuente de verdad:
    // el controlador los traduce a ProductoVariante y Producto no los usa operativamente.
    // Deben permanecer opcionales: una variante comercial solo exige al menos una dimension
    // entre Marca/Modelo/Color/Talla, y Modelo no es obligatorio cuando ya existe otra dimension.
    public string? Marca { get; set; }
    public string? Modelo { get; set; }

    public string? Descripcion { get; set; }
    public TipoInventario TipoInventario { get; set; } = TipoInventario.MercaderiaVenta;
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public int UmbralStockBajo { get; set; } = 5;
    public int? CategoriaId { get; set; }
    public bool EsDestacado { get; set; }
    public int? ColorId { get; set; }
    public int? TallaId { get; set; }
    public int? MarcaId { get; set; }
    public int? ModeloId { get; set; }

    /// <summary>
    /// Variantes físicas del producto. Cada fila puede combinar Marca/Modelo/Color/Talla.
    /// Se conserva compatibilidad con clientes anteriores que todavía no lo envían.
    /// </summary>
    public List<ProductoVarianteFormularioDto> Variantes { get; set; } = new();

    /// Máximo 5 imágenes. La primera se marca como principal.
    public List<IFormFile>? Imagenes { get; set; }
}
