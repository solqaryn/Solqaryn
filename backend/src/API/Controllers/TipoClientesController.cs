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
[Route("tipo-clientes")]
public class TipoClientesController : ControllerBase
{
    private readonly ITipoClienteService _service;

    public TipoClientesController(ITipoClienteService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.TiposClientes, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()
    {
        var tipos = await _service.GetAllAsync();
        return Ok(ApiResponse<List<TipoClienteDto>>.Ok(tipos));
    }

    [HttpGet("activos")]
    [RequierePermiso(ModuloSistema.TiposClientes, AccionPermiso.Ver)]
    public async Task<IActionResult> GetActivos()
    {
        var tipos = await _service.GetActivosAsync();
        return Ok(ApiResponse<List<TipoClienteDto>>.Ok(tipos));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.TiposClientes, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var tipo = await _service.GetByIdAsync(id);
        if (tipo is null) return NotFound(ApiResponse<object>.Fail("Tipo de cliente no encontrado."));
        return Ok(ApiResponse<TipoClienteDto>.Ok(tipo));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.TiposClientes, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateTipoClienteDto dto)
    {
        var creado = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<TipoClienteDto>.Ok(creado, "Tipo de cliente creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.TiposClientes, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTipoClienteDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto);
        if (actualizado is null) return NotFound(ApiResponse<object>.Fail("Tipo de cliente no encontrado."));
        return Ok(ApiResponse<TipoClienteDto>.Ok(actualizado, "Tipo de cliente actualizado correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.TiposClientes, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var tipo = await _service.CambiarEstadoAsync(id, true);
        if (tipo is null) return NotFound(ApiResponse<object>.Fail("Tipo de cliente no encontrado."));
        return Ok(ApiResponse<TipoClienteDto>.Ok(tipo, "Tipo de cliente activado correctamente."));
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.TiposClientes, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var tipo = await _service.CambiarEstadoAsync(id, false);
        if (tipo is null) return NotFound(ApiResponse<object>.Fail("Tipo de cliente no encontrado."));
        return Ok(ApiResponse<TipoClienteDto>.Ok(tipo, "Tipo de cliente desactivado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.TiposClientes, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteAsync(id);
        if (!eliminado) return NotFound(ApiResponse<object>.Fail("Tipo de cliente no encontrado."));
        return Ok(ApiResponse<object>.Ok(new { }, "Tipo de cliente eliminado correctamente."));
    }
}
