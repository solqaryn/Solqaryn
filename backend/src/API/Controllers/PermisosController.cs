using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("permisos")]
public class PermisosController : ControllerBase
{
    private readonly IPermisoService _service;
    private readonly IPermisoCatalogoService _catalogoService;

    public PermisosController(IPermisoService service, IPermisoCatalogoService catalogoService)
    {
        _service = service;
        _catalogoService = catalogoService;
    }

    [HttpGet("matriz/{rolId:int}")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Administrar)]
    public async Task<IActionResult> GetMatriz(int rolId)
    {
        var matriz = await _service.GetMatrizEmpresaAsync(rolId, TenantPermissionContext.GetAuthorizedEmpresaId(HttpContext));
        return Ok(ApiResponse<List<PermisoMatrizItemDto>>.Ok(matriz));
    }

    [HttpPut("matriz/{rolId:int}")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Administrar)]
    public async Task<IActionResult> UpdateMatriz(int rolId, [FromBody] UpdatePermisoMatrizDto dto)
    {
        var actualizada = await _service.UpdateMatrizEmpresaAsync(rolId, dto, TenantPermissionContext.GetAuthorizedEmpresaId(HttpContext));
        return Ok(ApiResponse<List<PermisoMatrizItemDto>>.Ok(actualizada, "Matriz de permisos actualizada correctamente."));
    }

    [HttpGet("mis-permisos")]
    public async Task<IActionResult> GetMisPermisos()
    {
        var permisos = await _service.GetMisPermisosAsync();
        return Ok(ApiResponse<MisPermisosDto>.Ok(permisos));
    }

    [HttpGet("mis-permisos/empresa/{empresaId:int}")]
    public async Task<IActionResult> GetMisPermisosEmpresa(int empresaId)
    {
        if (empresaId <= 0) return BadRequest(ApiResponse<object>.Fail("Empresa inválida."));

        var permisos = await _service.GetMisPermisosAsync(empresaId);
        if (string.IsNullOrWhiteSpace(permisos.Rol))
            return Forbid();

        return Ok(ApiResponse<MisPermisosDto>.Ok(permisos));
    }

    [HttpGet("catalogo")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetCatalogo([FromQuery] bool incluirEliminados = false)
    {
        var permisos = await _catalogoService.GetAllEmpresaAsync(TenantPermissionContext.GetAuthorizedEmpresaId(HttpContext), incluirEliminados);
        return Ok(ApiResponse<List<PermisoCatalogoDto>>.Ok(permisos));
    }

    [HttpGet("catalogo/{id:int}")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetCatalogoById(int id)
    {
        var permiso = await _catalogoService.GetByIdEmpresaAsync(id, TenantPermissionContext.GetAuthorizedEmpresaId(HttpContext));
        if (permiso is null)
            return NotFound(ApiResponse<object>.Fail("Permiso no encontrado."));

        return Ok(ApiResponse<PermisoCatalogoDto>.Ok(permiso));
    }

    [HttpPost("catalogo")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Crear)]
    public Task<IActionResult> CreateCatalogo([FromBody] CrearPermisoDto dto) => DenegarMutacionCatalogoAsync();

    [HttpPut("catalogo/{id:int}")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Editar)]
    public Task<IActionResult> UpdateCatalogo(int id, [FromBody] ActualizarPermisoDto dto) => DenegarMutacionCatalogoAsync();

    [HttpPatch("catalogo/{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Activar)]
    public Task<IActionResult> ActivarCatalogo(int id) => DenegarMutacionCatalogoAsync();

    [HttpPatch("catalogo/{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Desactivar)]
    public Task<IActionResult> DesactivarCatalogo(int id) => DenegarMutacionCatalogoAsync();

    [HttpDelete("catalogo/{id:int}")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.EliminarLogico)]
    public Task<IActionResult> EliminarLogicoCatalogo(int id) => DenegarMutacionCatalogoAsync();

    [HttpDelete("catalogo/{id:int}/permanente")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.EliminarPermanente)]
    public Task<IActionResult> EliminarPermanenteCatalogo(int id) => DenegarMutacionCatalogoAsync();

    [HttpPost("catalogo/{id:int}/duplicar")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Duplicar)]
    public Task<IActionResult> DuplicarCatalogo(int id, [FromBody] DuplicarPermisoRequest request) => DenegarMutacionCatalogoAsync();
    // Permiso is a global definition (no EmpresaId). Tenant RBAC authorizes
    // grants on owned roles, never mutation of definitions shared by all tenants.
    // A separate, verified platform authority is required for global management.
    private Task<IActionResult> DenegarMutacionCatalogoAsync()
    {
        _ = TenantPermissionContext.GetAuthorizedEmpresaId(HttpContext);
        throw new ForbiddenAccessException(
            "El catálogo de permisos es de solo lectura para las empresas. Administre las asignaciones desde la matriz de roles.");
    }
}

public class DuplicarPermisoRequest
{
    public string NuevoNombre { get; set; } = string.Empty;
    public string NuevaAccion { get; set; } = string.Empty;
}
