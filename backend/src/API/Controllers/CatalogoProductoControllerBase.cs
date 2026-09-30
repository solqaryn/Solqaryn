using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

public abstract class CatalogoProductoControllerBase : ControllerBase
{
    private readonly ICatalogoProductoService _service;
    protected abstract TipoCatalogoProducto Tipo { get; }

    protected CatalogoProductoControllerBase(ICatalogoProductoService service)
    {
        _service = service;
    }

    protected async Task<IActionResult> Listar(string? buscar = null, int? padreId = null) =>
        Ok(ApiResponse<List<CatalogoProductoDto>>.Ok(await _service.GetAllAsync(Tipo, buscar, padreId)));

    protected async Task<IActionResult> ListarActivos(int? padreId = null) =>
        Ok(ApiResponse<List<CatalogoProductoDto>>.Ok(await _service.GetActivosAsync(Tipo, padreId)));

    protected async Task<IActionResult> Obtener(int id)
    {
        var elemento = await _service.GetByIdAsync(Tipo, id);
        return elemento is null
            ? NotFound(ApiResponse<object>.Fail("Elemento no encontrado."))
            : Ok(ApiResponse<CatalogoProductoDto>.Ok(elemento));
    }

    protected async Task<IActionResult> Crear(CreateCatalogoProductoDto dto)
    {
        var creado = await _service.CreateAsync(Tipo, dto);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<CatalogoProductoDto>.Ok(creado, "Elemento creado correctamente."));
    }

    protected async Task<IActionResult> Actualizar(int id, UpdateCatalogoProductoDto dto)
    {
        var actualizado = await _service.UpdateAsync(Tipo, id, dto);
        return actualizado is null
            ? NotFound(ApiResponse<object>.Fail("Elemento no encontrado."))
            : Ok(ApiResponse<CatalogoProductoDto>.Ok(actualizado, "Elemento actualizado correctamente."));
    }

    protected async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        var actualizado = await _service.CambiarEstadoAsync(Tipo, id, activo);
        return actualizado is null
            ? NotFound(ApiResponse<object>.Fail("Elemento no encontrado."))
            : Ok(ApiResponse<CatalogoProductoDto>.Ok(actualizado,
                activo ? "Elemento activado correctamente." : "Elemento desactivado correctamente."));
    }

    protected async Task<IActionResult> Eliminar(int id)
    {
        var eliminado = await _service.DeleteAsync(Tipo, id);
        return eliminado
            ? Ok(ApiResponse<object>.Ok(new { }, "Elemento eliminado correctamente."))
            : NotFound(ApiResponse<object>.Fail("Elemento no encontrado."));
    }
}
