using Solqaryn.Application.Common;

namespace Solqaryn.Application.DTOs;

public sealed class ReporteInventarioKardexFiltroDto : ReporteInventarioFiltroBaseDto
{
    public int? ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public string? Tipo { get; set; }
    public string? Causa { get; set; }
    public string? CorrelationId { get; set; }
    public string? OrigenTipo { get; set; }
    public int? OrigenId { get; set; }
}

public sealed class ReporteInventarioValorizacionResumenDto
{
    public decimal? ValorInventarioCosto { get; set; }
    public decimal? ValorInventarioCostoMercaderia { get; set; }
    public decimal? ValorInventarioCostoInsumosAdministrativos { get; set; }
    public decimal? ValorPotencialVentaMercaderia { get; set; }
}

public enum TipoReporteStockHealth
{
    StockBajo = 1,
    Agotado = 2,
    SinMovimiento = 3,
    Aging = 4,
    Rotacion = 5
}

public sealed class ReporteInventarioStockHealthFiltroDto : ReporteInventarioFiltroBaseDto
{
    public TipoReporteStockHealth TipoReporte { get; set; } = TipoReporteStockHealth.StockBajo;
    public int? ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int Dias { get; set; } = 30;
}

public sealed class ReporteInventarioStockHealthDto
{
    public int ProductoVarianteId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int? UbicacionAlmacenId { get; set; }
    public string? UbicacionNombre { get; set; }
    public int StockDisponible { get; set; }
    public int StockMinimo { get; set; }
    public bool StockBajo { get; set; }
    public bool Agotado { get; set; }
    public DateTime? UltimoMovimientoUtc { get; set; }
    public int DiasSinMovimiento { get; set; }
    public int UnidadesSalidaPeriodo { get; set; }
    public decimal RotacionPeriodo { get; set; }
}
