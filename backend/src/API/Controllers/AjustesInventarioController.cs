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
[Route("inventario/ajustes")]
public sealed class AjustesInventarioController : ControllerBase
{
    private readonly IAjusteInventarioService _service;
    private readonly IAjusteInventarioConsultaService _consulta;

    public AjustesInventarioController(
        IAjusteInventarioService service,
        IAjusteInventarioConsultaService consulta)
    {
        _service = service;
        _consulta = consulta;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Ver)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AjusteInventarioDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] AjusteInventarioFiltroDto filtro)
    {
        var resultado = await _consulta.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<AjusteInventarioDto>>.Ok(resultado));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Ver)]
    [ProducesResponseType(typeof(ApiResponse<AjusteInventarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ajuste = await _service.GetByIdAsync(id);
        if (ajuste is null)
            return NotFound(ApiResponse<object>.Fail("Ajuste de inventario no encontrado."));
        return Ok(ApiResponse<AjusteInventarioDto>.Ok(ajuste));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Crear)]
    [ProducesResponseType(typeof(ApiResponse<AjusteInventarioDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateAjusteInventarioDto dto)
    {
        var creado = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<AjusteInventarioDto>.Ok(creado, "Borrador de ajuste creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Editar)]
    [ProducesResponseType(typeof(ApiResponse<AjusteInventarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAjusteInventarioDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto);
        if (actualizado is null)
            return NotFound(ApiResponse<object>.Fail("Ajuste de inventario no encontrado."));
        return Ok(ApiResponse<AjusteInventarioDto>.Ok(actualizado, "Borrador de ajuste actualizado correctamente."));
    }

    [HttpPost("{id:int}/confirmar")]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Confirmar)]
    [ProducesResponseType(typeof(ApiResponse<AjusteInventarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirmar(int id)
    {
        var confirmado = await _service.ConfirmarAsync(id);
        if (confirmado is null)
            return NotFound(ApiResponse<object>.Fail("Ajuste de inventario no encontrado."));
        return Ok(ApiResponse<AjusteInventarioDto>.Ok(
            confirmado,
            "Ajuste confirmado y movimientos de inventario registrados correctamente."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Anular)]
    [ProducesResponseType(typeof(ApiResponse<AjusteInventarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularAjusteInventarioDto dto)
    {
        var anulado = await _service.AnularAsync(id, dto.MotivoAnulacion);
        if (anulado is null)
            return NotFound(ApiResponse<object>.Fail("Ajuste de inventario no encontrado."));
        return Ok(ApiResponse<AjusteInventarioDto>.Ok(
            anulado,
            "Ajuste anulado mediante movimientos inversos de inventario."));
    }
}
