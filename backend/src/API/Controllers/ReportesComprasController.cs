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
[Route("compras/reportes")]
public sealed class ReportesComprasController : ControllerBase
{
    private readonly IReporteComprasService _service;
    private readonly IAuditoriaService? _auditoria;

    public ReportesComprasController(
        IReporteComprasService service,
        IAuditoriaService? auditoria = null)
    {
        _service = service;
        _auditoria = auditoria;
    }

    [HttpGet("detalle")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetDetalle(
        [FromQuery] ReporteComprasFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resultado = await _service.ObtenerDetallePaginadoAsync(filtro, cancellationToken);
            await RegistrarConsultaAsync("detalle");
            return Ok(ApiResponse<PagedResult<ReporteComprasDetalleDto>>.Ok(resultado));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    private async Task RegistrarConsultaAsync(string reporte)
    {
        if (_auditoria is null)
            return;

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Ver,
            $"Consulta del reporte de compras '{reporte}'.",
            entidad: "ReportesCompras",
            valoresNuevos: new
            {
                CorrelationId = HttpContext.TraceIdentifier,
                Reporte = reporte
            });
    }
}
