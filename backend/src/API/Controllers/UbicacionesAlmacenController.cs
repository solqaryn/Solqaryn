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
[Route("ubicaciones-almacen")]
public sealed class UbicacionesAlmacenController : ControllerBase
{
    private readonly IUbicacionAlmacenService _service;

    public UbicacionesAlmacenController(IUbicacionAlmacenService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.UbicacionesAlmacen, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] UbicacionAlmacenFiltroDto filtro)
    {
        var pagina = await _service.BuscarAsync(filtro);
        return Ok(ApiResponse<UbicacionAlmacenPaginaDto>.Ok(pagina));
    }

    [HttpGet("activas")]
    [RequierePermiso(ModuloSistema.UbicacionesAlmacen, AccionPermiso.Ver)]
    public async Task<IActionResult> GetActivas([FromQuery] int? almacenId = null, [FromQuery] int? ubicacionPadreId = null)
    {
        var ubicaciones = await _service.GetActivasAsync(almacenId, ubicacionPadreId);
        return Ok(ApiResponse<List<UbicacionAlmacenDto>>.Ok(ubicaciones));
    }

    [HttpGet("tipos")]
    [RequierePermiso(ModuloSistema.UbicacionesAlmacen, AccionPermiso.Ver)]
    public IActionResult GetTipos() =>
        Ok(ApiResponse<IReadOnlyList<TipoUbicacionAlmacenDto>>.Ok(_service.GetTipos()));

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.UbicacionesAlmacen, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var ubicacion = await _service.GetByIdAsync(id);
        if (ubicacion is null)
            return NotFound(ApiResponse<object>.Fail("Ubicación no encontrada."));

        return Ok(ApiResponse<UbicacionAlmacenDto>.Ok(ubicacion));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.UbicacionesAlmacen, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateUbicacionAlmacenDto dto)
    {
        var creada = await _service.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = creada.Id },
            ApiResponse<UbicacionAlmacenDto>.Ok(creada, "Ubicación creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.UbicacionesAlmacen, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUbicacionAlmacenDto dto)
    {
        var actualizada = await _service.UpdateAsync(id, dto);
        if (actualizada is null)
            return NotFound(ApiResponse<object>.Fail("Ubicación no encontrada."));

        return Ok(ApiResponse<UbicacionAlmacenDto>.Ok(actualizada, "Ubicación actualizada correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.UbicacionesAlmacen, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var ubicacion = await _service.CambiarEstadoAsync(id, true);
        if (ubicacion is null)
            return NotFound(ApiResponse<object>.Fail("Ubicación no encontrada."));

        return Ok(ApiResponse<UbicacionAlmacenDto>.Ok(ubicacion, "Ubicación activada correctamente."));
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.UbicacionesAlmacen, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var ubicacion = await _service.CambiarEstadoAsync(id, false);
        if (ubicacion is null)
            return NotFound(ApiResponse<object>.Fail("Ubicación no encontrada."));

        return Ok(ApiResponse<UbicacionAlmacenDto>.Ok(ubicacion, "Ubicación desactivada correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.UbicacionesAlmacen, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminada = await _service.DeleteAsync(id);
        if (!eliminada)
            return NotFound(ApiResponse<object>.Fail("Ubicación no encontrada."));

        return Ok(ApiResponse<object>.Ok(new { }, "Ubicación eliminada correctamente."));
    }
}
