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
[Route("descuentos")]
public class DescuentosController : ControllerBase
{
    private readonly IDescuentoService _service;

    public DescuentosController(IDescuentoService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Descuentos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll([FromQuery] bool incluirEliminados = false)
    {
        var lista = await _service.GetAllAsync(incluirEliminados);
        return Ok(ApiResponse<List<DescuentoDto>>.Ok(lista));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Descuentos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var d = await _service.GetByIdAsync(id);
        if (d is null) return NotFound(ApiResponse<object>.Fail("Descuento no encontrado."));
        return Ok(ApiResponse<DescuentoDto>.Ok(d));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Descuentos, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] GuardarDescuentoDto dto)
    {
        var creado = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, ApiResponse<DescuentoDto>.Ok(creado, "Descuento creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Descuentos, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] GuardarDescuentoDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<DescuentoDto>.Ok(actualizado, "Descuento actualizado correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Descuentos, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var d = await _service.ActivarAsync(id);
        return Ok(ApiResponse<DescuentoDto>.Ok(d, "Descuento activado correctamente."));
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Descuentos, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var d = await _service.DesactivarAsync(id);
        return Ok(ApiResponse<DescuentoDto>.Ok(d, "Descuento desactivado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Descuentos, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> EliminarLogico(int id)
    {
        await _service.EliminarLogicoAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Descuento eliminado correctamente."));
    }

    [HttpDelete("{id:int}/permanente")]
    [RequierePermiso(ModuloSistema.Descuentos, AccionPermiso.EliminarPermanente)]
    public async Task<IActionResult> EliminarPermanente(int id)
    {
        await _service.EliminarPermanenteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Descuento eliminado permanentemente."));
    }

    [HttpPost("{id:int}/duplicar")]
    [RequierePermiso(ModuloSistema.Descuentos, AccionPermiso.Duplicar)]
    public async Task<IActionResult> Duplicar(int id, [FromBody] DuplicarDescuentoRequest request)
    {
        var copia = await _service.DuplicarAsync(id, request.NuevoNombre);
        return CreatedAtAction(nameof(GetById), new { id = copia.Id }, ApiResponse<DescuentoDto>.Ok(copia, "Descuento duplicado correctamente."));
    }
}

public class DuplicarDescuentoRequest
{
    public string NuevoNombre { get; set; } = string.Empty;
}
