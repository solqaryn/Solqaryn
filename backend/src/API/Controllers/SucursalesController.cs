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
[Route("sucursales")]
public sealed class SucursalesController : ControllerBase
{
    private readonly ISucursalService _sucursalService;

    public SucursalesController(ISucursalService sucursalService)
    {
        _sucursalService = sucursalService;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Sucursales, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] SucursalFiltroDto filtro)
    {
        var pagina = await _sucursalService.BuscarAsync(filtro);
        return Ok(ApiResponse<SucursalPaginaDto>.Ok(pagina));
    }

    [HttpGet("activas")]
    [RequierePermiso(ModuloSistema.Sucursales, AccionPermiso.Ver)]
    public async Task<IActionResult> GetActivas([FromQuery] int? empresaId = null)
    {
        var sucursales = await _sucursalService.GetActivasAsync(empresaId);
        return Ok(ApiResponse<List<SucursalDto>>.Ok(sucursales));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Sucursales, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var sucursal = await _sucursalService.GetByIdAsync(id);
        if (sucursal is null)
            return NotFound(ApiResponse<object>.Fail("Sucursal no encontrada."));

        return Ok(ApiResponse<SucursalDto>.Ok(sucursal));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Sucursales, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateSucursalDto dto)
    {
        var creada = await _sucursalService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = creada.Id },
            ApiResponse<SucursalDto>.Ok(creada, "Sucursal creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Sucursales, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSucursalDto dto)
    {
        var actualizada = await _sucursalService.UpdateAsync(id, dto);
        if (actualizada is null)
            return NotFound(ApiResponse<object>.Fail("Sucursal no encontrada."));

        return Ok(ApiResponse<SucursalDto>.Ok(actualizada, "Sucursal actualizada correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Sucursales, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var sucursal = await _sucursalService.CambiarEstadoAsync(id, true);
        if (sucursal is null)
            return NotFound(ApiResponse<object>.Fail("Sucursal no encontrada."));

        return Ok(ApiResponse<SucursalDto>.Ok(sucursal, "Sucursal activada correctamente."));
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Sucursales, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var sucursal = await _sucursalService.CambiarEstadoAsync(id, false);
        if (sucursal is null)
            return NotFound(ApiResponse<object>.Fail("Sucursal no encontrada."));

        return Ok(ApiResponse<SucursalDto>.Ok(sucursal, "Sucursal desactivada correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Sucursales, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminada = await _sucursalService.DeleteAsync(id);
        if (!eliminada)
            return NotFound(ApiResponse<object>.Fail("Sucursal no encontrada."));

        return Ok(ApiResponse<object>.Ok(new { }, "Sucursal eliminada correctamente."));
    }
}
