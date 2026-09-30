using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class Producto : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;

    // Campos de compatibilidad y snapshot histórico. La fuente administrable
    // pasa a ser MarcaId/ModeloId, pero se conservan para datos anteriores.
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
    public TipoInventario TipoInventario { get; set; } = TipoInventario.MercaderiaVenta;

    // Compatibilidad durante la migración hacia inventario por variantes.
    // Cuando existan variantes, la cantidad consolidada se mantiene como la
    // suma física de todas las variantes no eliminadas, incluso si alguna está
    // temporalmente inactiva para nuevas operaciones.
    public int Cantidad { get; set; }

    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public int UmbralStockBajo { get; set; } = 5;

    public bool Activo { get; set; } = true;
    public bool EsDestacado { get; set; }
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    // Proyección familiar de compatibilidad. Desde ERP-N0.2 estos IDs apuntan
    // directamente a los maestros normalizados; la autoridad operacional de
    // dimensiones continúa en ProductoVariante.
    public int? ColorId { get; set; }
    public Color? ColorCatalogo { get; set; }
    public int? TallaId { get; set; }
    public Talla? TallaCatalogo { get; set; }
    public int? MarcaId { get; set; }
    public Marca? MarcaCatalogo { get; set; }
    public int? ModeloId { get; set; }
    public Modelo? ModeloCatalogo { get; set; }

    // Alias no persistentes para consumidores históricos internos. Ambos
    // resuelven al maestro normalizado y EF los ignora explícitamente.
    public Color? Color
    {
        get => ColorCatalogo;
        set => ColorCatalogo = value;
    }

    public Talla? Talla
    {
        get => TallaCatalogo;
        set => TallaCatalogo = value;
    }

    public ICollection<ProductoImagen> Imagenes { get; set; } = new List<ProductoImagen>();
    public ICollection<ProductoVariante> Variantes { get; set; } = new List<ProductoVariante>();

    public int CantidadVariantesActivas => Variantes
        .Where(v => v.Activo && !v.Eliminado)
        .Sum(v => v.Cantidad);

    public bool TieneStockBajo => Activo && !Eliminado && Cantidad > 0 && Cantidad < UmbralStockBajo;
    public bool EstaAgotado => Activo && !Eliminado && Cantidad <= 0;

    // La galería general del Producto y las galerías de variantes son ámbitos
    // distintos. Una imagen específica nunca debe convertirse accidentalmente
    // en la portada general del producto.
    public ProductoImagen? ImagenPrincipal =>
        Imagenes.Where(i => i.ProductoVarianteId == null && i.EsPrincipal).FirstOrDefault()
        ?? Imagenes.Where(i => i.ProductoVarianteId == null).OrderBy(i => i.Orden).FirstOrDefault();
}
