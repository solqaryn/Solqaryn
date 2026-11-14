using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class ConsumoInsumoDetalle : BaseEntity
{
    public int ConsumoInsumoId { get; set; }
    public ConsumoInsumo ConsumoInsumo { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }

    // Contexto físico N1.4. Nullable para históricos anteriores al cutover.
    public int? AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public UbicacionAlmacen? UbicacionAlmacen { get; set; }

    public int Cantidad { get; set; }
    public decimal CostoUnitarioSnapshot { get; set; }
    public decimal CostoTotalSnapshot { get; set; }
    public string NombreSnapshot { get; set; } = string.Empty;
    public string? SkuSnapshot { get; set; }
    public string? ColorSnapshot { get; set; }
}
