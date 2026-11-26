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
[RequierePermiso(ModuloSistema.Auditoria, AccionPermiso.Ver)]
[Route("auditoria")]
public class AuditoriaController : ControllerBase
{
    private readonly IAuditoriaService _service;
    private readonly IUsuarioScopeService _scopeService;

    public AuditoriaController(IAuditoriaService service, IUsuarioScopeService scopeService)
    {
        _service = service;
        _scopeService = scopeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiltered([FromQuery] AuditoriaFiltroDto filtro)
    {
        // La auditoría contiene información transversal y sensible de toda la empresa.
        // Aun cuando una matriz de permisos se configure incorrectamente, nunca se
        // entrega a un rol no administrativo.
        var empresaId = TenantPermissionContext.GetAuthorizedEmpresaId(HttpContext);
        var scope = await _scopeService.ObtenerActualAsync(empresaId, HttpContext.RequestAborted);
        if (scope is null || scope.EmpresaId != empresaId || !scope.EsAdministrador)
            return Forbid();

        var resultado = await _service.GetFilteredAsync(filtro);
        return Ok(ApiResponse<PagedResult<RegistroAuditoriaDto>>.Ok(resultado));
    }
}
