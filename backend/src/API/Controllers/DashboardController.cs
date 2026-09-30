using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("resumen")]
    [RequierePermiso(ModuloSistema.Dashboard, AccionPermiso.Ver)]
    public async Task<IActionResult> GetResumen()
    {
        var resumen = await _dashboardService.GetResumenAsync();
        return Ok(ApiResponse<DashboardResumenDto>.Ok(resumen));
    }

    [HttpGet("inventario/variantes")]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetInventarioVariantes(
        [FromQuery] int? productoId = null,
        [FromQuery] int? marcaId = null,
        [FromQuery] int? modeloId = null,
        [FromQuery] int? colorId = null,
        [FromQuery] int? tallaId = null,
        [FromQuery] bool incluirInactivas = true,
        CancellationToken cancellationToken = default)
    {
        var reporte = await _dashboardService.GetInventarioVariantesAsync(
            productoId, marcaId, modeloId, colorId, tallaId, incluirInactivas, cancellationToken);
        return Ok(ApiResponse<InventarioVariantesReporteDto>.Ok(reporte));
    }
}
