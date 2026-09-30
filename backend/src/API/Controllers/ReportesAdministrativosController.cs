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
[Route("reportes-administrativos")]
public class ReportesAdministrativosController : ControllerBase
{
    private readonly IReporteAdministrativoService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public ReportesAdministrativosController(
        IReporteAdministrativoService service,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _service = service;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    [HttpGet("resumen")]
    [RequierePermiso(ModuloSistema.ReportesAdministrativos, AccionPermiso.Ver)]
    public async Task<IActionResult> Resumen(
        [FromQuery] ReporteAdministrativoFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.EsAdministrador) return Forbid();
        var resultado = await _service.ObtenerResumenAsync(filtro, cancellationToken);
        return Ok(ApiResponse<ResumenAdministrativoDto>.Ok(resultado));
    }

    [HttpGet("usuarios-accesos")]
    [RequierePermiso(ModuloSistema.ReportesAdministrativos, AccionPermiso.Ver)]
    public async Task<IActionResult> UsuariosAccesos(CancellationToken cancellationToken)
    {
        if (!_currentUser.EsAdministrador) return Forbid();
        var resultado = await _service.ObtenerUsuariosAccesoAsync(cancellationToken);
        return Ok(ApiResponse<List<UsuarioAccesoReporteDto>>.Ok(resultado));
    }

    [HttpGet("roles-permisos")]
    [RequierePermiso(ModuloSistema.ReportesAdministrativos, AccionPermiso.Ver)]
    public async Task<IActionResult> RolesPermisos(CancellationToken cancellationToken)
    {
        if (!_currentUser.EsAdministrador) return Forbid();
        var resultado = await _service.ObtenerRolesPermisosAsync(cancellationToken);
        return Ok(ApiResponse<List<RolPermisosReporteDto>>.Ok(resultado));
    }

    [HttpGet("auditoria-resumen")]
    [RequierePermiso(ModuloSistema.ReportesAdministrativos, AccionPermiso.Ver)]
    public async Task<IActionResult> AuditoriaResumen(
        [FromQuery] ReporteAdministrativoFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.EsAdministrador) return Forbid();
        var resultado = await _service.ObtenerResumenAuditoriaAsync(filtro, cancellationToken);
        return Ok(ApiResponse<AuditoriaResumenDto>.Ok(resultado));
    }

    [HttpGet("exportar/{tipo}")]
    [RequierePermiso(ModuloSistema.ReportesAdministrativos, AccionPermiso.Exportar)]
    public async Task<IActionResult> Exportar(
        string tipo,
        [FromQuery] string formato,
        [FromQuery] ReporteAdministrativoFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.EsAdministrador) return Forbid();
        var archivo = await _service.ExportarAsync(tipo, formato, filtro, cancellationToken);

        await _auditoria.RegistrarAsync(
            ModuloSistema.ReportesAdministrativos,
            AccionPermiso.Exportar,
            $"Reporte administrativo '{tipo}' exportado en formato {formato}.",
            entidad: "ReporteAdministrativo",
            valoresNuevos: new { Tipo = tipo, Formato = formato, filtro.Desde, filtro.Hasta });

        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }
}
