namespace Solqaryn.Application.DTOs;

public class DashboardResumenDto
{
    // Inventario
    public int TotalProductos { get; set; }
    public int TotalUnidades { get; set; }
    public decimal ValorTotalInventario { get; set; }
    public decimal ValorPotencialVenta { get; set; }
    public int TotalProductosMercaderia { get; set; }
    public int TotalProductosInsumosAdministrativos { get; set; }
    public int TotalUnidadesMercaderia { get; set; }
    public int TotalUnidadesInsumosAdministrativos { get; set; }
    public decimal ValorInventarioCostoMercaderia { get; set; }
    public decimal ValorInventarioCostoInsumosAdministrativos { get; set; }
    public decimal ValorPotencialVentaMercaderia { get; set; }
    public List<ProductoDto> ProductosStockBajo { get; set; } = new();
    public List<ProductoDto> UltimosAgregados { get; set; } = new();

    // Operación
    public int ComprasDelMes { get; set; }
    public int VentasDelMes { get; set; }
    public List<CompraResumenDto> UltimasCompras { get; set; } = new();
    public List<VentaResumenDto> UltimasVentas { get; set; } = new();

    // Finanzas
    public decimal IngresosDelMes { get; set; }
    public decimal UtilidadBruta { get; set; }
    public decimal BalanceOperativo { get; set; }
    public decimal CuentasPorCobrar { get; set; }
    public decimal CuentasPorPagar { get; set; }

    // Auditoría
    public string? UltimaVentaPor { get; set; }
    public string? UltimaCompraPor { get; set; }
    public string? UltimaRevisionFinancieraPor { get; set; }
    public string? UltimoProductoRegistradoPor { get; set; }
}

public class CompraResumenDto
{
    public string NumeroCompra { get; set; } = string.Empty;
    public string ProveedorNombre { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}

public class VentaResumenDto
{
    public string NumeroVenta { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
