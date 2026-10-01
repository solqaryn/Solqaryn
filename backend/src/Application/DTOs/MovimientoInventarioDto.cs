namespace Solqaryn.Application.DTOs;

public class MovimientoInventarioDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int? AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string? ProductoColor { get; set; }
    public string? ProductoSku { get; set; }
    public string? ProductoImagenPrincipalUrl { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Causa { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockNuevo { get; set; }
    public decimal? CostoUnitario { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public string CorrelationId { get; set; } = string.Empty;

    // Origen tipado autoritativo. ReferenciaTipo/ReferenciaId se conservan
    // temporalmente como snapshot de compatibilidad para históricos legacy.
    public string? OrigenTipo { get; set; }
    public int? OrigenId { get; set; }
    public int? CompraId { get; set; }
    public int? VentaId { get; set; }
    public int? ConsumoInsumoId { get; set; }
    public int? AjusteInventarioId { get; set; }
    public int? TransferenciaInventarioId { get; set; }

    public string ReferenciaTipo { get; set; } = string.Empty;
    public int ReferenciaId { get; set; }
    public string? Descripcion { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public DateTime Fecha { get; set; }
}
