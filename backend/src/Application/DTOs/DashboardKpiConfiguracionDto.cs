using FluentValidation;

namespace Solqaryn.Application.DTOs;

/// <summary>
/// Closed catalog of dashboard metrics that are already backed by the existing
/// DashboardResumenDto/DashboardService authorities. New financial formulas or
/// arbitrary expressions are intentionally not part of this contract.
/// </summary>
public static class DashboardKpiMetricKeys
{
    public const string IngresosMes = "INGRESOS_MES";
    public const string VentasMes = "VENTAS_MES";
    public const string ComprasMes = "COMPRAS_MES";
    public const string TotalProductos = "TOTAL_PRODUCTOS";
    public const string TotalUnidades = "TOTAL_UNIDADES";
    public const string ValorInventario = "VALOR_INVENTARIO";
    public const string UtilidadBruta = "UTILIDAD_BRUTA";
    public const string BalanceOperativo = "BALANCE_OPERATIVO";
    public const string CuentasPorCobrar = "CUENTAS_POR_COBRAR";
    public const string CuentasPorPagar = "CUENTAS_POR_PAGAR";
    public const string ProductosStockBajo = "PRODUCTOS_STOCK_BAJO";

    private static readonly HashSet<string> SupportedMetricKeys = new(StringComparer.Ordinal)
    {
        IngresosMes,
        VentasMes,
        ComprasMes,
        TotalProductos,
        TotalUnidades,
        ValorInventario,
        UtilidadBruta,
        BalanceOperativo,
        CuentasPorCobrar,
        CuentasPorPagar,
        ProductosStockBajo
    };

    public static IReadOnlySet<string> Supported => SupportedMetricKeys;

    public static bool IsSupported(string? metricKey) =>
        !string.IsNullOrWhiteSpace(metricKey) && SupportedMetricKeys.Contains(metricKey);
}

/// <summary>
/// Configuration for one supported dashboard KPI. Authorization and data
/// visibility continue to be resolved by the existing dashboard role/user
/// authorities; this DTO does not grant or broaden data access.
/// </summary>
public sealed class DashboardKpiConfiguracionDto
{
    public string MetricKey { get; set; } = string.Empty;
    public bool Habilitado { get; set; } = true;
    public int Orden { get; set; }
    public string? EtiquetaVisible { get; set; }
}

/// <summary>
/// Fail-closed validation for metric keys. Unknown, empty or differently-cased
/// keys are rejected rather than being interpreted as formulas or expressions.
/// </summary>
public sealed class DashboardKpiConfiguracionDtoValidator : AbstractValidator<DashboardKpiConfiguracionDto>
{
    public DashboardKpiConfiguracionDtoValidator()
    {
        RuleFor(x => x.MetricKey)
            .Must(DashboardKpiMetricKeys.IsSupported)
            .WithMessage("MetricKey no está soportada por el catálogo cerrado del dashboard.");
    }
}
