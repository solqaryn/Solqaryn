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
[Route("almacenes")]
public sealed class AlmacenesController : ControllerBase
{
    private readonly IAlmacenService _almacenService;

    public AlmacenesController(IAlmacenService almacenService)
    {
        _almacenService = almacenService;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Almacenes, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] AlmacenFiltroDto filtro)
    {
        var pagina = await _almacenService.BuscarAsync(filtro);
        return Ok(ApiResponse<AlmacenPaginaDto>.Ok(pagina));
    }

    [HttpGet("activos")]
    [RequierePermiso(ModuloSistema.Almacenes, AccionPermiso.Ver)]
    public async Task<IActionResult> GetActivos([FromQuery] int? sucursalId = null)
    {
        var almacenes = await _almacenService.GetActivosAsync(sucursalId);
        return Ok(ApiResponse<List<AlmacenDto>>.Ok(almacenes));
    }

    [HttpGet("tipos")]
    [RequierePermiso(ModuloSistema.Almacenes, AccionPermiso.Ver)]
    public IActionResult GetTipos() =>
        Ok(ApiResponse<IReadOnlyList<TipoAlmacenDto>>.Ok(_almacenService.GetTipos()));

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Almacenes, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var almacen = await _almacenService.GetByIdAsync(id);
        if (almacen is null)
            return NotFound(ApiResponse<object>.Fail("Almacén no encontrado."));

        return Ok(ApiResponse<AlmacenDto>.Ok(almacen));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Almacenes, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateAlmacenDto dto)
    {
        var creado = await _almacenService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = creado.Id },
            ApiResponse<AlmacenDto>.Ok(creado, "Almacén creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Almacenes, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAlmacenDto dto)
    {
        var actualizado = await _almacenService.UpdateAsync(id, dto);
        if (actualizado is null)
            return NotFound(ApiResponse<object>.Fail("Almacén no encontrado."));

        return Ok(ApiResponse<AlmacenDto>.Ok(actualizado, "Almacén actualizado correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Almacenes, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var almacen = await _almacenService.CambiarEstadoAsync(id, true);
        if (almacen is null)
            return NotFound(ApiResponse<object>.Fail("Almacén no encontrado."));

        return Ok(ApiResponse<AlmacenDto>.Ok(almacen, "Almacén activado correctamente."));
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Almacenes, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var almacen = await _almacenService.CambiarEstadoAsync(id, false);
        if (almacen is null)
            return NotFound(ApiResponse<object>.Fail("Almacén no encontrado."));

        return Ok(ApiResponse<AlmacenDto>.Ok(almacen, "Almacén desactivado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Almacenes, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _almacenService.DeleteAsync(id);
        if (!eliminado)
            return NotFound(ApiResponse<object>.Fail("Almacén no encontrado."));

        return Ok(ApiResponse<object>.Ok(new { }, "Almacén eliminado correctamente."));
    }
}
