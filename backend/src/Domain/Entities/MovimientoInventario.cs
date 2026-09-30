using System.ComponentModel.DataAnnotations.Schema;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class MovimientoInventario
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int? ProductoVarianteId { get; set; }
    public ProductoVariante? ProductoVariante { get; set; }

    // Contexto físico de la existencia afectada. Nullable para movimientos
    // históricos anteriores al cutover N1.4; los movimientos nuevos deben fijar
    // la clave exacta utilizada por la operación.
    public int? AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public UbicacionAlmacen? UbicacionAlmacen { get; set; }

    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
    public string? ProductoSkuSnapshot { get; set; }

    // Correlación durable del evento de inventario. Los movimientos nuevos deben
    // persistirla para trazabilidad end-to-end; el vacío sólo puede existir en
    // histórico anterior al cutover N1.5.
    public string CorrelationId { get; set; } = string.Empty;

    public TipoMovimientoInventario Tipo { get; set; }
    public CausaMovimientoInventario Causa { get; set; } = CausaMovimientoInventario.NoEspecificada;
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockNuevo { get; set; }
    public decimal? CostoUnitario { get; set; }
    public decimal? PrecioUnitario { get; set; }

    // Snapshots legacy de compatibilidad/correlación. No son la autoridad relacional.
    public string ReferenciaTipo { get; set; } = string.Empty;
    public int ReferenciaId { get; set; }

    // FKs tipadas físicas. Debe existir como máximo una por movimiento.
    public int? CompraId { get; set; }
    public int? VentaId { get; set; }
    public int? ConsumoInsumoId { get; set; }
    public int? AjusteInventarioId { get; set; }
    public int? TransferenciaInventarioId { get; set; }
    public int? RecepcionCompraId { get; set; }

    [NotMapped]
    public OrigenMovimientoInventario? OrigenTipado
    {
        get
        {
            var cantidadOrigenes =
                (CompraId.HasValue ? 1 : 0) +
                (VentaId.HasValue ? 1 : 0) +
                (ConsumoInsumoId.HasValue ? 1 : 0) +
                (AjusteInventarioId.HasValue ? 1 : 0) +
                (TransferenciaInventarioId.HasValue ? 1 : 0) +
                (RecepcionCompraId.HasValue ? 1 : 0);

            if (cantidadOrigenes == 0)
                return null;

            return OrigenMovimientoInventario.DesdeIds(
                CompraId,
                VentaId,
                ConsumoInsumoId,
                AjusteInventarioId,
                TransferenciaInventarioId,
                RecepcionCompraId);
        }
    }

    public string? Descripcion { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
