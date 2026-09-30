using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("inventario/reportes")]
public sealed class ReportesInventarioController : ControllerBase
{
    private readonly IAuditoriaService _auditoria;

    public ReportesInventarioController(IAuditoriaService auditoria)
    {
        _auditoria = auditoria;
    }

    [HttpGet("estado")]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetEstado()
    {
        var correlationId = HttpContext.TraceIdentifier;

        await _auditoria.RegistrarAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Ver,
            "Consulta del estado del shell seguro de reportes de inventario.",
            entidad: "ReportesInventario",
            valoresNuevos: new { CorrelationId = correlationId });

        return Ok(ApiResponse<object>.Ok(new
        {
            CorrelationId = correlationId,
            Familias = new[] { "valorizacion", "kardex", "stock-health", "reconciliacion" }
        }));
    }
}
