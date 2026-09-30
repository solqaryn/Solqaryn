namespace Solqaryn.Domain.Entities;

public class CompraDetalle
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public Compra? Compra { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }

    // Contexto físico N1.4. Nullable exclusivamente para preservar documentos
    // históricos previos al cutover; las operaciones nuevas lo validan de forma
    // explícita y nunca infieren un almacén arbitrario.
    public int? AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public UbicacionAlmacen? UbicacionAlmacen { get; set; }

    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public string ProductoNombreSnapshot { get; set; } = string.Empty;
    public string ProductoMarcaSnapshot { get; set; } = string.Empty;
    public string ProductoModeloSnapshot { get; set; } = string.Empty;
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
    public string? ProductoSkuSnapshot { get; set; }

    // Snapshots de valoración capturados al confirmar la compra. Permanecen
    // nullable para no alterar compras históricas anteriores a 2E.2.
    public decimal? CostoProductoAnteriorSnapshot { get; set; }
    public decimal? CostoProductoNuevoSnapshot { get; set; }
    public decimal? CostoVarianteAnteriorSnapshot { get; set; }
    public decimal? CostoVarianteNuevoSnapshot { get; set; }
    public int? StockProductoAnteriorSnapshot { get; set; }
    public int? StockProductoNuevoSnapshot { get; set; }
    public int? StockVarianteAnteriorSnapshot { get; set; }
    public int? StockVarianteNuevoSnapshot { get; set; }
}
