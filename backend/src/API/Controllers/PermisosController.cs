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
        var matriz = await _service.GetMatrizAsync(rolId);
        return Ok(ApiResponse<List<PermisoMatrizItemDto>>.Ok(matriz));
    }

    [HttpPut("matriz/{rolId:int}")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Administrar)]
    public async Task<IActionResult> UpdateMatriz(int rolId, [FromBody] UpdatePermisoMatrizDto dto)
    {
        var actualizada = await _service.UpdateMatrizAsync(rolId, dto);
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
        var permisos = await _catalogoService.GetAllAsync(incluirEliminados);
        return Ok(ApiResponse<List<PermisoCatalogoDto>>.Ok(permisos));
    }

    [HttpGet("catalogo/{id:int}")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetCatalogoById(int id)
    {
        var permiso = await _catalogoService.GetByIdAsync(id);
        if (permiso is null)
            return NotFound(ApiResponse<object>.Fail("Permiso no encontrado."));

        return Ok(ApiResponse<PermisoCatalogoDto>.Ok(permiso));
    }

    [HttpPost("catalogo")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Crear)]
    public async Task<IActionResult> CreateCatalogo([FromBody] CrearPermisoDto dto)
    {
        var creado = await _catalogoService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetCatalogoById), new { id = creado.Id },
            ApiResponse<PermisoCatalogoDto>.Ok(creado, "Permiso creado correctamente."));
    }

    [HttpPut("catalogo/{id:int}")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Editar)]
    public async Task<IActionResult> UpdateCatalogo(int id, [FromBody] ActualizarPermisoDto dto)
    {
        var actualizado = await _catalogoService.UpdateAsync(id, dto);
        return Ok(ApiResponse<PermisoCatalogoDto>.Ok(actualizado, "Permiso actualizado correctamente."));
    }

    [HttpPatch("catalogo/{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Activar)]
    public async Task<IActionResult> ActivarCatalogo(int id)
    {
        var permiso = await _catalogoService.ActivarAsync(id);
        return Ok(ApiResponse<PermisoCatalogoDto>.Ok(permiso, "Permiso activado correctamente."));
    }

    [HttpPatch("catalogo/{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Desactivar)]
    public async Task<IActionResult> DesactivarCatalogo(int id)
    {
        var permiso = await _catalogoService.DesactivarAsync(id);
        return Ok(ApiResponse<PermisoCatalogoDto>.Ok(permiso, "Permiso desactivado correctamente."));
    }

    [HttpDelete("catalogo/{id:int}")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> EliminarLogicoCatalogo(int id)
    {
        await _catalogoService.EliminarLogicoAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Permiso eliminado correctamente."));
    }

    [HttpDelete("catalogo/{id:int}/permanente")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.EliminarPermanente)]
    public async Task<IActionResult> EliminarPermanenteCatalogo(int id)
    {
        await _catalogoService.EliminarPermanenteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Permiso eliminado permanentemente."));
    }

    [HttpPost("catalogo/{id:int}/duplicar")]
    [RequierePermiso(ModuloSistema.Permisos, AccionPermiso.Duplicar)]
    public async Task<IActionResult> DuplicarCatalogo(int id, [FromBody] DuplicarPermisoRequest request)
    {
        var copia = await _catalogoService.DuplicarAsync(id, request.NuevoNombre, request.NuevaAccion);
        return CreatedAtAction(nameof(GetCatalogoById), new { id = copia.Id },
            ApiResponse<PermisoCatalogoDto>.Ok(copia, "Permiso duplicado correctamente."));
    }
}

public class DuplicarPermisoRequest
{
    public string NuevoNombre { get; set; } = string.Empty;
    public string NuevaAccion { get; set; } = string.Empty;
}
