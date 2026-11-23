using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.Security;
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
    private readonly ICurrentUserService _currentUser;
    private readonly IUsuarioScopeService _tenantScopes;

    public AuditoriaController(IAuditoriaService service, ICurrentUserService currentUser, IUsuarioScopeService tenantScopes)
    {
        _service = service;
        _currentUser = currentUser;
        _tenantScopes = tenantScopes;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiltered([FromQuery] AuditoriaFiltroDto filtro)
    {
        // La auditoría contiene información transversal y sensible de toda la empresa.
        // Aun cuando una matriz de permisos se configure incorrectamente, nunca se
        // entrega a un rol no administrativo.
        if (!_currentUser.EsAdministrador)
            return Forbid();

        // A signed JWT's esAdministrador claim is only a hint. A role may have
        // been demoted or revoked in MySQL while the token remains valid.
        // Resolve the tenant only from the previous server-side RBAC marker,
        // never from query params or the client-controlled X-Empresa-Id.
        var tenantId = VerifiedCommercialTenant.Resolve(HttpContext);
        var actorId = _currentUser.UsuarioId;
        if (tenantId is not > 0 || actorId is not > 0)
            return Forbid();

        var currentScope = await _tenantScopes.ObtenerActualAsync(
            tenantId.Value, HttpContext.RequestAborted);
        if (currentScope is null ||
            currentScope.UsuarioId != actorId.Value ||
            currentScope.EmpresaId != tenantId.Value ||
            !currentScope.EsAdministrador)
            return Forbid();

        var resultado = await _service.GetFilteredAsync(filtro);
        return Ok(ApiResponse<PagedResult<RegistroAuditoriaDto>>.Ok(resultado));
    }
}
