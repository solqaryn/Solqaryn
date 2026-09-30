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
[Route("devoluciones-clientes")]
public sealed class DevolucionesClienteController : ControllerBase
{
    private readonly IDevolucionClienteService _service;

    public DevolucionesClienteController(IDevolucionClienteService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] DevolucionClienteFiltroDto filtro)
    {
        var pagina = await _service.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<DevolucionClienteDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<DevolucionClienteDto>.Ok(item));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Crear)]
    public async Task<IActionResult> Crear([FromBody] CreateDevolucionClienteDto dto, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Idempotency-Key requerido", detail: "La creación de una devolución exige el encabezado Idempotency-Key.");
        var creado = await _service.CrearAsync(dto, idempotencyKey);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, ApiResponse<DevolucionClienteDto>.Ok(creado, "Devolución de cliente creada correctamente."));
    }

    [HttpPost("{id:int}/confirmar")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Confirmar(int id)
    {
        var item = await _service.ConfirmarAsync(id);
        return Ok(ApiResponse<DevolucionClienteDto>.Ok(item, "Devolución de cliente confirmada correctamente."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Anular)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularDevolucionClienteDto dto)
    {
        var item = await _service.AnularAsync(id, dto.Motivo);
        return Ok(ApiResponse<DevolucionClienteDto>.Ok(item, "Devolución de cliente anulada correctamente."));
    }
}
