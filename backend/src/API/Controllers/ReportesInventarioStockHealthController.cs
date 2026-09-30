using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("inventario/reportes/stock-health")]
public sealed class ReportesInventarioStockHealthController : ControllerBase
{
    private readonly ReporteInventarioService _reportes;
    private readonly IUsuarioScopeService _usuarioScope;
    private readonly IAuditoriaService _auditoria;

    public ReportesInventarioStockHealthController(
        AppDbContext context,
        IUsuarioScopeService usuarioScope,
        IAuditoriaService auditoria)
    {
        _reportes = new ReporteInventarioService(context, usuarioScope);
        _usuarioScope = usuarioScope;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Ver)]
    public async Task<IActionResult> Get(
        [FromQuery] ReporteInventarioStockHealthFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var error = ReporteInventarioQueryRules.ValidateStockHealth(filtro);
        if (error is not null)
            return BadRequest(ApiResponse<object>.Fail("Consulta de stock-health inválida.", new() { error }));

        var empresaId = await TenantPermissionContext.RequireEmpresaIdAsync(HttpContext, cancellationToken);
        if (!await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(filtro, _usuarioScope))
            return Forbid();

        var resultado = await _reportes.ObtenerStockHealthAsync(empresaId, filtro, cancellationToken);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Ver,
            "Consulta autorizada de stock-health de inventario.",
            entidad: "ReporteInventarioStockHealth",
            valoresNuevos: new
            {
                EmpresaId = empresaId,
                filtro.AlmacenId,
                filtro.UbicacionAlmacenId,
                filtro.SucursalId,
                filtro.Desde,
                filtro.Hasta,
                filtro.Dias,
                filtro.Page,
                filtro.PageSize,
                CorrelationId = HttpContext.TraceIdentifier
            });

        return Ok(ApiResponse<PagedResult<ReporteInventarioStockHealthDto>>.Ok(resultado));
    }
}
