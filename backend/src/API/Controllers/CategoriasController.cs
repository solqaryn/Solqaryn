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
[Route("categorias")]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaService _categoriaService;

    public CategoriasController(ICategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Categorias, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()
    {
        var categorias = await _categoriaService.GetAllAsync();
        return Ok(ApiResponse<List<CategoriaDto>>.Ok(categorias));
    }

    [HttpGet("activas")]
    [RequierePermiso(ModuloSistema.Categorias, AccionPermiso.Ver)]
    public async Task<IActionResult> GetActivas()
    {
        var categorias = await _categoriaService.GetActivasAsync();
        return Ok(ApiResponse<List<CategoriaDto>>.Ok(categorias));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Categorias, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var categoria = await _categoriaService.GetByIdAsync(id);
        if (categoria is null)
            return NotFound(ApiResponse<object>.Fail("Categoría no encontrada."));

        return Ok(ApiResponse<CategoriaDto>.Ok(categoria));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Categorias, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateCategoriaDto dto)
    {
        var creada = await _categoriaService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creada.Id },
            ApiResponse<CategoriaDto>.Ok(creada, "Categoría creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Categorias, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoriaDto dto)
    {
        var actualizada = await _categoriaService.UpdateAsync(id, dto);
        if (actualizada is null)
            return NotFound(ApiResponse<object>.Fail("Categoría no encontrada."));

        return Ok(ApiResponse<CategoriaDto>.Ok(actualizada, "Categoría actualizada correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Categorias, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var categoria = await _categoriaService.CambiarEstadoAsync(id, true);
        if (categoria is null) return NotFound(ApiResponse<object>.Fail("Categoría no encontrada."));
        return Ok(ApiResponse<CategoriaDto>.Ok(categoria, "Categoría activada correctamente."));
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Categorias, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var categoria = await _categoriaService.CambiarEstadoAsync(id, false);
        if (categoria is null) return NotFound(ApiResponse<object>.Fail("Categoría no encontrada."));
        return Ok(ApiResponse<CategoriaDto>.Ok(categoria, "Categoría desactivada correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Categorias, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminada = await _categoriaService.DeleteAsync(id);
        if (!eliminada)
            return NotFound(ApiResponse<object>.Fail("Categoría no encontrada."));

        return Ok(ApiResponse<object>.Ok(new { }, "Categoría eliminada correctamente."));
    }
}
