using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("dashboard/kpis")]
public sealed class DashboardKpiConfiguracionController : ControllerBase
{
    private static readonly string[] MetricOrder =
    {
        DashboardKpiMetricKeys.IngresosMes,
        DashboardKpiMetricKeys.VentasMes,
        DashboardKpiMetricKeys.ComprasMes,
        DashboardKpiMetricKeys.TotalProductos,
        DashboardKpiMetricKeys.TotalUnidades,
        DashboardKpiMetricKeys.ValorInventario,
        DashboardKpiMetricKeys.UtilidadBruta,
        DashboardKpiMetricKeys.BalanceOperativo,
        DashboardKpiMetricKeys.CuentasPorCobrar,
        DashboardKpiMetricKeys.CuentasPorPagar,
        DashboardKpiMetricKeys.ProductosStockBajo
    };

    private readonly AppDbContext _db;
    private readonly IUsuarioScopeService _usuarioScope;
    private readonly IDashboardService _dashboardService;

    public DashboardKpiConfiguracionController(
        AppDbContext db,
        IUsuarioScopeService usuarioScope,
        IDashboardService dashboardService)
    {
        _db = db;
        _usuarioScope = usuarioScope;
        _dashboardService = dashboardService;
    }

    [HttpGet("configuracion")]
    [RequierePermiso(ModuloSistema.Dashboard, AccionPermiso.Ver)]
    public async Task<ActionResult<IReadOnlyList<DashboardKpiConfiguracionDto>>> GetConfiguracionAsync(
        CancellationToken cancellationToken)
    {
        var scope = await ResolverScopeAsync();
        if (scope is null)
        {
            return Forbid();
        }

        return Ok(await ObtenerConfiguracionEfectivaAsync(scope, cancellationToken));
    }

    [HttpPut("configuracion")]
    [RequierePermiso(ModuloSistema.Dashboard, AccionPermiso.Ver)]
    public async Task<ActionResult<IReadOnlyList<DashboardKpiConfiguracionDto>>> PutConfiguracionAsync(
        [FromBody] IReadOnlyList<DashboardKpiConfiguracionDto>? configuracion,
        CancellationToken cancellationToken)
    {
        var scope = await ResolverScopeAsync();
        if (scope is null)
        {
            return Forbid();
        }

        if (configuracion is null)
        {
            return BadRequest(new { message = "La configuración es obligatoria." });
        }

        var duplicate = configuracion
            .GroupBy(x => x.MetricKey, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            return BadRequest(new { message = $"MetricKey duplicada: {duplicate.Key}." });
        }

        foreach (var item in configuracion)
        {
            if (!DashboardKpiMetricKeys.IsSupported(item.MetricKey))
            {
                return BadRequest(new { message = $"MetricKey no soportada: {item.MetricKey}." });
            }

            if (item.Orden < 0)
            {
                return BadRequest(new { message = $"Orden inválido para {item.MetricKey}." });
            }

            if (item.EtiquetaVisible?.Trim().Length > 150)
            {
                return BadRequest(new { message = $"EtiquetaVisible excede 150 caracteres para {item.MetricKey}." });
            }
        }

        var existentes = await _db.DashboardKpiConfiguraciones
            .Where(x => x.UsuarioId == scope.UsuarioId && x.RolId == null)
            .ToListAsync(cancellationToken);

        var solicitadas = configuracion.ToDictionary(x => x.MetricKey, StringComparer.Ordinal);

        foreach (var existente in existentes)
        {
            if (!solicitadas.ContainsKey(existente.MetricKey))
            {
                _db.DashboardKpiConfiguraciones.Remove(existente);
            }
        }

        foreach (var item in configuracion)
        {
            var existente = existentes.FirstOrDefault(x => x.MetricKey == item.MetricKey);
            if (existente is null)
            {
                _db.DashboardKpiConfiguraciones.Add(new DashboardKpiConfiguracion
                {
                    MetricKey = item.MetricKey,
                    UsuarioId = scope.UsuarioId,
                    RolId = null,
                    Habilitado = item.Habilitado,
                    Orden = item.Orden,
                    EtiquetaVisible = NormalizarEtiqueta(item.EtiquetaVisible)
                });
            }
            else
            {
                existente.Habilitado = item.Habilitado;
                existente.Orden = item.Orden;
                existente.EtiquetaVisible = NormalizarEtiqueta(item.EtiquetaVisible);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(await ObtenerConfiguracionEfectivaAsync(scope, cancellationToken));
    }

    [HttpGet("resueltos")]
    [RequierePermiso(ModuloSistema.Dashboard, AccionPermiso.Ver)]
    public async Task<ActionResult<IReadOnlyList<object>>> GetKpisResueltosAsync(
        CancellationToken cancellationToken)
    {
        var scope = await ResolverScopeAsync();
        if (scope is null)
        {
            return Forbid();
        }

        var configuracion = await ObtenerConfiguracionEfectivaAsync(scope, cancellationToken);
        var resumen = await _dashboardService.GetResumenAsync();

        var valores = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [DashboardKpiMetricKeys.IngresosMes] = resumen.IngresosDelMes,
            [DashboardKpiMetricKeys.VentasMes] = resumen.VentasDelMes,
            [DashboardKpiMetricKeys.ComprasMes] = resumen.ComprasDelMes,
            [DashboardKpiMetricKeys.TotalProductos] = resumen.TotalProductos,
            [DashboardKpiMetricKeys.TotalUnidades] = resumen.TotalUnidades,
            [DashboardKpiMetricKeys.ValorInventario] = resumen.ValorTotalInventario,
            [DashboardKpiMetricKeys.UtilidadBruta] = resumen.UtilidadBruta,
            [DashboardKpiMetricKeys.BalanceOperativo] = resumen.BalanceOperativo,
            [DashboardKpiMetricKeys.CuentasPorCobrar] = resumen.CuentasPorCobrar,
            [DashboardKpiMetricKeys.CuentasPorPagar] = resumen.CuentasPorPagar,
            [DashboardKpiMetricKeys.ProductosStockBajo] = resumen.ProductosStockBajo.Count
        };

        var result = configuracion
            .Where(x => x.Habilitado)
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.MetricKey)
            .Select(x => (object)new
            {
                x.MetricKey,
                x.Orden,
                x.EtiquetaVisible,
                Valor = valores[x.MetricKey]
            })
            .ToList();

        return Ok(result);
    }

    private async Task<IReadOnlyList<DashboardKpiConfiguracionDto>> ObtenerConfiguracionEfectivaAsync(
        UsuarioScopeActual scope,
        CancellationToken cancellationToken)
    {
        var rows = await _db.DashboardKpiConfiguraciones
            .AsNoTracking()
            .Where(x =>
                (x.UsuarioId == scope.UsuarioId && x.RolId == null) ||
                (x.UsuarioId == null && x.RolId == scope.RolId))
            .ToListAsync(cancellationToken);

        var user = rows
            .Where(x => x.UsuarioId == scope.UsuarioId)
            .ToDictionary(x => x.MetricKey, StringComparer.Ordinal);
        var role = rows
            .Where(x => x.RolId == scope.RolId)
            .ToDictionary(x => x.MetricKey, StringComparer.Ordinal);

        return MetricOrder
            .Select((metricKey, defaultOrder) =>
            {
                var row = user.GetValueOrDefault(metricKey) ?? role.GetValueOrDefault(metricKey);
                return new DashboardKpiConfiguracionDto
                {
                    MetricKey = metricKey,
                    Habilitado = row?.Habilitado ?? true,
                    Orden = row?.Orden ?? defaultOrder,
                    EtiquetaVisible = row?.EtiquetaVisible
                };
            })
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.MetricKey)
            .ToList();
    }

    private async Task<UsuarioScopeActual?> ResolverScopeAsync()
    {
        return await _usuarioScope.ObtenerActualAsync();
    }

    private static string? NormalizarEtiqueta(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
