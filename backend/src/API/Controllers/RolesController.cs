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
[Route("roles")]
public class RolesController : ControllerBase
{
    private readonly IRolService _rolService;

    public RolesController(IRolService rolService)
    {
        _rolService = rolService;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Roles, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll([FromQuery] bool incluirEliminados = false)
    {
        var roles = await _rolService.GetAllAsync(incluirEliminados);
        return Ok(ApiResponse<List<RolDto>>.Ok(roles));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Roles, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var rol = await _rolService.GetByIdAsync(id);
        if (rol is null)
            return NotFound(ApiResponse<object>.Fail("Rol no encontrado."));

        return Ok(ApiResponse<RolDto>.Ok(rol));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Roles, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CrearRolDto dto)
    {
        var creado = await _rolService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<RolDto>.Ok(creado, "Rol creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Roles, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] ActualizarRolDto dto)
    {
        var actualizado = await _rolService.UpdateAsync(id, dto);
        return Ok(ApiResponse<RolDto>.Ok(actualizado, "Rol actualizado correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Roles, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var rol = await _rolService.ActivarAsync(id);
        return Ok(ApiResponse<RolDto>.Ok(rol, "Rol activado correctamente."));
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Roles, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var rol = await _rolService.DesactivarAsync(id);
        return Ok(ApiResponse<RolDto>.Ok(rol, "Rol desactivado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Roles, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> EliminarLogico(int id)
    {
        await _rolService.EliminarLogicoAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Rol eliminado correctamente."));
    }

    [HttpDelete("{id:int}/permanente")]
    [RequierePermiso(ModuloSistema.Roles, AccionPermiso.EliminarPermanente)]
    public async Task<IActionResult> EliminarPermanente(int id)
    {
        await _rolService.EliminarPermanenteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Rol eliminado permanentemente."));
    }

    [HttpPost("{id:int}/duplicar")]
    [RequierePermiso(ModuloSistema.Roles, AccionPermiso.Duplicar)]
    public async Task<IActionResult> Duplicar(int id, [FromBody] DuplicarRolRequest request)
    {
        var copia = await _rolService.DuplicarAsync(id, request.NuevoNombre);
        return CreatedAtAction(nameof(GetById), new { id = copia.Id },
            ApiResponse<RolDto>.Ok(copia, "Rol duplicado correctamente."));
    }
}

public class DuplicarRolRequest
{
    public string NuevoNombre { get; set; } = string.Empty;
}
