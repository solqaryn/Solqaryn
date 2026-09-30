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
[Route("ventas/reportes")]
public sealed class ReportesVentasController : ControllerBase
{
    private readonly IReporteVentasService _service;
    private readonly IAuditoriaService? _auditoria;

    public ReportesVentasController(
        IReporteVentasService service,
        IAuditoriaService? auditoria = null)
    {
        _service = service;
        _auditoria = auditoria;
    }

    [HttpGet("resumen")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetResumen(
        [FromQuery] ReporteVentasFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var errores = ReporteVentasQueryRules.Validate(filtro);
        if (errores.Count > 0)
            return BadRequest(ApiResponse<object>.Fail(string.Join(" ", errores)));

        await RegistrarConsultaAsync("resumen");

        var resultado = await _service.ObtenerResumenAsync(filtro, cancellationToken);
        return Ok(ApiResponse<ReporteVentasResumenDto>.Ok(resultado));
    }

    [HttpGet("detalle")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetDetalle(
        [FromQuery] ReporteVentasFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var errores = ReporteVentasQueryRules.Validate(filtro);
        if (errores.Count > 0)
            return BadRequest(ApiResponse<object>.Fail(string.Join(" ", errores)));

        await RegistrarConsultaAsync("detalle");

        var resultado = await _service.ObtenerDetallePaginadoAsync(filtro, cancellationToken);
        return Ok(ApiResponse<PagedResult<ReporteVentasDetalleDto>>.Ok(resultado));
    }

    private async Task RegistrarConsultaAsync(string reporte)
    {
        if (_auditoria is null)
            return;

        var correlationId = HttpContext.TraceIdentifier;

        await _auditoria.RegistrarAsync(
            ModuloSistema.Ventas,
            AccionPermiso.Ver,
            $"Consulta del reporte de ventas '{reporte}'.",
            entidad: "ReportesVentas",
            valoresNuevos: new
            {
                CorrelationId = correlationId,
                Reporte = reporte
            });
    }
}
