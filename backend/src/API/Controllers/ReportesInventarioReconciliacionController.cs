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
[Route("inventario/reportes/reconciliacion")]
public sealed class ReportesInventarioReconciliacionController : ControllerBase
{
    private readonly ReporteInventarioService _reportes;
    private readonly IUsuarioScopeService _usuarioScope;
    private readonly IPermisoService _permisos;
    private readonly IAuditoriaService _auditoria;

    public ReportesInventarioReconciliacionController(
        AppDbContext context,
        IUsuarioScopeService usuarioScope,
        IPermisoService permisos,
        IAuditoriaService auditoria)
    {
        _reportes = new ReporteInventarioService(context, usuarioScope);
        _usuarioScope = usuarioScope;
        _permisos = permisos;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Ver)]
    public async Task<IActionResult> Get(
        [FromQuery] ReporteInventarioReconciliacionFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var error = ReporteInventarioQueryRules.Validate(filtro, "Fecha");
        if (error is not null)
            return BadRequest(ApiResponse<object>.Fail("Consulta de reconciliación inválida.", new() { error }));

        var empresaId = await TenantPermissionContext.RequireEmpresaIdAsync(HttpContext, cancellationToken);
        if (!await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(filtro, _usuarioScope))
            return Forbid();

        var resultado = await _reportes.ObtenerReporteReconciliacionAsync(empresaId, filtro, cancellationToken);
        var puedeVerFinanzas = await _permisos.TienePermisoAsync(empresaId, ModuloSistema.Finanzas, AccionPermiso.Ver);
        if (!puedeVerFinanzas)
        {
            foreach (var row in resultado.Items)
            {
                row.CostoUnitario = null;
                row.ImpactoCosto = null;
            }
        }

        await _auditoria.RegistrarAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Ver,
            puedeVerFinanzas
                ? "Consulta autorizada de reconciliación de inventario."
                : "Consulta de reconciliación de inventario con campos financieros censurados.",
            entidad: "ReporteInventarioReconciliacion",
            valoresNuevos: new
            {
                EmpresaId = empresaId,
                filtro.ProductoId,
                filtro.ProductoVarianteId,
                filtro.AlmacenId,
                filtro.UbicacionAlmacenId,
                filtro.SucursalId,
                filtro.Desde,
                filtro.Hasta,
                filtro.Page,
                filtro.PageSize,
                FinanzasCensuradas = !puedeVerFinanzas
            });

        return Ok(ApiResponse<PagedResult<ReporteInventarioReconciliacionDto>>.Ok(resultado));
    }
}
