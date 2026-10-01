using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class MovimientoFinanciero
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public TipoMovimientoFinanciero Tipo { get; set; }
    public CategoriaMovimientoFinanciero Categoria { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public EstadoMovimientoFinanciero Estado { get; set; } = EstadoMovimientoFinanciero.Pagado;

    // Compatibilidad ERP-N0: el enum nullable sigue siendo leído/escrito por los
    // servicios actuales hasta migrarlos al catálogo relacional.
    public MetodoPago? MetodoPago { get; set; }
    public int? MetodoPagoId { get; set; }
    public Solqaryn.Domain.Entities.Catalogos.MetodoPago? MetodoPagoCatalogo { get; set; }

    public bool EsAutomatico { get; set; }
    /// "Compra" | "Venta" | "Factura" | "Reversion" | "Manual"
    public string ModuloOrigen { get; set; } = string.Empty;
    public int? ReferenciaId { get; set; }

    public int? CompraId { get; set; }
    public int? VentaId { get; set; }
    public int? FacturaId { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }

    public int? AnuladoPorUsuarioId { get; set; }
    public string? AnuladoPorNombreUsuario { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? MotivoAnulacion { get; set; }
}
