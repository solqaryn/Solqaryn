using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("centros-costo")]
public sealed class CentrosCostoController : ControllerBase
{
    private readonly ICentroCostoService _service;

    public CentrosCostoController(ICentroCostoService service) => _service = service;

    [HttpGet]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] string? termino = null, [FromQuery] TipoCentroCosto? tipo = null, [FromQuery] int? sucursalId = null, [FromQuery] bool? activo = null, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        var (items, total) = await _service.BuscarAsync(termino, tipo, sucursalId, activo, pagina, tamanoPagina);
        return Ok(ApiResponse<object>.Ok(new { items, total, pagina = Math.Max(1, pagina), tamanoPagina = Math.Clamp(tamanoPagina, 1, 100) }));
    }

    [HttpGet("activos")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetActivos([FromQuery] TipoCentroCosto? tipo = null, [FromQuery] int? sucursalId = null)
        => Ok(ApiResponse<List<CentroCostoDto>>.Ok(await _service.GetActivosAsync(tipo, sucursalId)));

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound(ApiResponse<object>.Fail("Centro de costo no encontrado.")) : Ok(ApiResponse<CentroCostoDto>.Ok(item));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateCentroCostoDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<CentroCostoDto>.Ok(created, "Centro de costo creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCentroCostoDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);
        return updated is null ? NotFound(ApiResponse<object>.Fail("Centro de costo no encontrado.")) : Ok(ApiResponse<CentroCostoDto>.Ok(updated, "Centro de costo actualizado correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var item = await _service.CambiarEstadoAsync(id, true);
        return item is null ? NotFound(ApiResponse<object>.Fail("Centro de costo no encontrado.")) : Ok(ApiResponse<CentroCostoDto>.Ok(item));
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var item = await _service.CambiarEstadoAsync(id, false);
        return item is null ? NotFound(ApiResponse<object>.Fail("Centro de costo no encontrado.")) : Ok(ApiResponse<CentroCostoDto>.Ok(item));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? Ok(ApiResponse<object>.Ok(new { }, "Centro de costo eliminado correctamente.")) : NotFound(ApiResponse<object>.Fail("Centro de costo no encontrado."));
}
