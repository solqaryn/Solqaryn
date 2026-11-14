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
[Route("insumos-administrativos/consumos")]
public class InsumosAdministrativosController : ControllerBase
{
    private readonly IConsumoInsumoService _service;

    public InsumosAdministrativosController(IConsumoInsumoService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.InsumosAdministrativos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()
    {
        var consumos = await _service.GetAllAsync();
        return Ok(ApiResponse<List<ConsumoInsumoDto>>.Ok(consumos));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.InsumosAdministrativos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var consumo = await _service.GetByIdAsync(id);
        if (consumo is null)
            return NotFound(ApiResponse<object>.Fail("Consumo administrativo no encontrado."));
        return Ok(ApiResponse<ConsumoInsumoDto>.Ok(consumo));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.InsumosAdministrativos, AccionPermiso.RegistrarConsumo)]
    public async Task<IActionResult> Create([FromBody] CreateConsumoInsumoDto dto)
    {
        var creado = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<ConsumoInsumoDto>.Ok(creado, "Borrador de consumo creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.InsumosAdministrativos, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateConsumoInsumoDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto);
        if (actualizado is null)
            return NotFound(ApiResponse<object>.Fail("Consumo administrativo no encontrado."));
        return Ok(ApiResponse<ConsumoInsumoDto>.Ok(actualizado, "Consumo actualizado correctamente."));
    }

    [HttpPost("{id:int}/confirmar")]
    [RequierePermiso(ModuloSistema.InsumosAdministrativos, AccionPermiso.RegistrarConsumo)]
    public async Task<IActionResult> Confirmar(int id)
    {
        var confirmado = await _service.ConfirmarAsync(id);
        if (confirmado is null)
            return NotFound(ApiResponse<object>.Fail("Consumo administrativo no encontrado."));
        return Ok(ApiResponse<ConsumoInsumoDto>.Ok(confirmado, "Consumo confirmado y stock descontado correctamente."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.InsumosAdministrativos, AccionPermiso.RegistrarConsumo)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularConsumoInsumoDto dto)
    {
        var anulado = await _service.AnularAsync(id, dto.MotivoAnulacion);
        if (anulado is null)
            return NotFound(ApiResponse<object>.Fail("Consumo administrativo no encontrado."));
        return Ok(ApiResponse<ConsumoInsumoDto>.Ok(anulado, "Consumo anulado y stock restaurado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.InsumosAdministrativos, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteBorradorAsync(id);
        if (!eliminado)
            return NotFound(ApiResponse<object>.Fail("Consumo administrativo no encontrado."));
        return Ok(ApiResponse<object>.Ok(new { }, "Borrador de consumo eliminado correctamente."));
    }
}
