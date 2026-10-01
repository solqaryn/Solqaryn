namespace Solqaryn.Domain.Entities;

public class VentaDetalle
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }

    // Contexto físico N1.4. Nullable sólo por compatibilidad histórica.
    public int? AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public UbicacionAlmacen? UbicacionAlmacen { get; set; }

    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal CostoUnitarioSnapshot { get; set; }
    public decimal Subtotal { get; set; }
    public decimal UtilidadBruta { get; set; }

    public string ProductoNombreSnapshot { get; set; } = string.Empty;
    public string ProductoMarcaSnapshot { get; set; } = string.Empty;
    public string ProductoModeloSnapshot { get; set; } = string.Empty;
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
}
