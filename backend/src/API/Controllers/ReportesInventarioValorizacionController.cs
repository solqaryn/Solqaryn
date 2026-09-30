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
[Route("inventario/reportes/valorizacion")]
public sealed class ReportesInventarioValorizacionController : ControllerBase
{
    private readonly IFinanzasService _finanzas;
    private readonly IPermisoService _permisos;
    private readonly IAuditoriaService _auditoria;

    public ReportesInventarioValorizacionController(
        IFinanzasService finanzas,
        IPermisoService permisos,
        IAuditoriaService auditoria)
    {
        _finanzas = finanzas;
        _permisos = permisos;
        _auditoria = auditoria;
    }

    [HttpGet("resumen")]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetResumen()
    {
        if (!await _permisos.TienePermisoAsync(ModuloSistema.Finanzas, AccionPermiso.Ver))
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Inventario,
                AccionPermiso.Ver,
                "Consulta de valorización de inventario con campos financieros censurados.",
                entidad: "ReporteInventarioValorizacion",
                resultado: "Censurado");

            return Ok(ApiResponse<ReporteInventarioValorizacionResumenDto>.Ok(
                new ReporteInventarioValorizacionResumenDto()));
        }

        var resumen = await _finanzas.GetResumenAsync();

        var resultado = new ReporteInventarioValorizacionResumenDto
        {
            ValorInventarioCosto = resumen.ValorInventarioCosto,
            ValorInventarioCostoMercaderia = resumen.ValorInventarioCostoMercaderia,
            ValorInventarioCostoInsumosAdministrativos = resumen.ValorInventarioCostoInsumosAdministrativos,
            ValorPotencialVentaMercaderia = resumen.ValorPotencialVentaMercaderia
        };

        await _auditoria.RegistrarAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Ver,
            "Consulta autorizada de valorización de inventario.",
            entidad: "ReporteInventarioValorizacion");

        return Ok(ApiResponse<ReporteInventarioValorizacionResumenDto>.Ok(resultado));
    }
}
