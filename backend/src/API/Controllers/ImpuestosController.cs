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
[Route("impuestos")]
public class ImpuestosController : ControllerBase
{
    private readonly IImpuestoService _service;

    public ImpuestosController(IImpuestoService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Impuestos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll([FromQuery] bool incluirEliminados = false)
    {
        var lista = await _service.GetAllAsync(incluirEliminados);
        return Ok(ApiResponse<List<ImpuestoDto>>.Ok(lista));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Impuestos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var i = await _service.GetByIdAsync(id);
        if (i is null) return NotFound(ApiResponse<object>.Fail("Impuesto no encontrado."));
        return Ok(ApiResponse<ImpuestoDto>.Ok(i));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Impuestos, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] GuardarImpuestoDto dto)
    {
        var creado = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, ApiResponse<ImpuestoDto>.Ok(creado, "Impuesto creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Impuestos, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] GuardarImpuestoDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<ImpuestoDto>.Ok(actualizado, "Impuesto actualizado correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Impuestos, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var i = await _service.ActivarAsync(id);
        return Ok(ApiResponse<ImpuestoDto>.Ok(i, "Impuesto activado correctamente."));
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Impuestos, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var i = await _service.DesactivarAsync(id);
        return Ok(ApiResponse<ImpuestoDto>.Ok(i, "Impuesto desactivado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Impuestos, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> EliminarLogico(int id)
    {
        await _service.EliminarLogicoAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Impuesto eliminado correctamente."));
    }

    [HttpDelete("{id:int}/permanente")]
    [RequierePermiso(ModuloSistema.Impuestos, AccionPermiso.EliminarPermanente)]
    public async Task<IActionResult> EliminarPermanente(int id)
    {
        await _service.EliminarPermanenteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Impuesto eliminado permanentemente."));
    }

    [HttpPost("{id:int}/duplicar")]
    [RequierePermiso(ModuloSistema.Impuestos, AccionPermiso.Duplicar)]
    public async Task<IActionResult> Duplicar(int id, [FromBody] DuplicarImpuestoRequest request)
    {
        var copia = await _service.DuplicarAsync(id, request.NuevoNombre, request.NuevoCodigo);
        return CreatedAtAction(nameof(GetById), new { id = copia.Id }, ApiResponse<ImpuestoDto>.Ok(copia, "Impuesto duplicado correctamente."));
    }
}

public class DuplicarImpuestoRequest
{
    public string NuevoNombre { get; set; } = string.Empty;
    public string NuevoCodigo { get; set; } = string.Empty;
}
