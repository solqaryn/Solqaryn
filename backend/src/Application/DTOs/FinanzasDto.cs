namespace Solqaryn.Application.DTOs;

public class MovimientoFinancieroDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Concepto { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MetodoPago { get; set; }
    public bool EsAutomatico { get; set; }
    public string ModuloOrigen { get; set; } = string.Empty;
    public string? CreadoPorNombreUsuario { get; set; }
    public string? AnuladoPorNombreUsuario { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? MotivoAnulacion { get; set; }
}

public class CreateMovimientoManualDto
{
    public string Tipo { get; set; } = "Egreso"; // Ingreso | Egreso | Ajuste
    public string Categoria { get; set; } = "GastoOperativo";
    public string Concepto { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
}

public class RevisionFinancieraDto
{
    public int Id { get; set; }
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public string RevisadoPorNombreUsuario { get; set; } = string.Empty;
    public DateTime FechaRevision { get; set; }
    public string EstadoRevision { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

public class CreateRevisionFinancieraDto
{
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public string EstadoRevision { get; set; } = "Revisado"; // Revisado | ConObservaciones
    public string? Observaciones { get; set; }
}

public class FinanzasResumenDto
{
    public decimal IngresosTotales { get; set; }
    public decimal EgresosTotales { get; set; }
    public decimal GastosOperativos { get; set; }
    public decimal UtilidadBruta { get; set; }
    public decimal MargenUtilidadBruta { get; set; }
    public decimal UtilidadNeta { get; set; }
    public decimal ValorInventarioCosto { get; set; }
    public decimal ValorInventarioCostoMercaderia { get; set; }
    public decimal ValorInventarioCostoInsumosAdministrativos { get; set; }
    public decimal ValorPotencialVenta { get; set; }
    public decimal ValorPotencialVentaMercaderia { get; set; }
    public decimal UtilidadInventarioPotencial { get; set; }
    public decimal MargenInventarioPotencial { get; set; }
    public decimal CuentasPorCobrar { get; set; }
    public decimal CuentasPorPagar { get; set; }
    public decimal BalanceOperativo { get; set; }
    public int VentasDelMes { get; set; }
    public int ComprasDelMes { get; set; }
    public decimal IngresosDelMes { get; set; }

    public RevisionFinancieraDto? UltimaRevision { get; set; }
}
