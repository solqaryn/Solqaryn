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
[Route("reservas-inventario")]
public sealed class ReservasInventarioController : ControllerBase
{
    private readonly IReservaInventarioService _service;

    public ReservasInventarioController(IReservaInventarioService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] ReservaInventarioQueryDto query)
    {
        var pagina = await _service.GetPagedAsync(query);
        return Ok(ApiResponse<PagedResult<ReservaInventarioDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var reserva = await _service.GetByIdAsync(id);
        return reserva is null
            ? NotFound(ApiResponse<object>.Fail("Reserva de inventario no encontrada."))
            : Ok(ApiResponse<ReservaInventarioDto>.Ok(reserva));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateReservaInventarioDto dto)
    {
        var creada = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creada.Id },
            ApiResponse<ReservaInventarioDto>.Ok(creada, "Reserva creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReservaInventarioDto dto)
    {
        var reserva = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<ReservaInventarioDto>.Ok(reserva, "Reserva actualizada correctamente."));
    }

    [HttpPost("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Confirmar)]
    public Task<IActionResult> Activar(int id) => EjecutarTransicionAsync(
        id, _service.ActivarAsync, "Reserva activada correctamente.");

    [HttpPost("{id:int}/consumir")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Confirmar)]
    public Task<IActionResult> Consumir(int id) => EjecutarTransicionAsync(
        id, _service.ConsumirAsync, "Reserva consumida correctamente.");

    [HttpPost("{id:int}/liberar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Anular)]
    public async Task<IActionResult> Liberar(int id, [FromBody] LiberarReservaInventarioDto dto)
    {
        var reserva = await _service.LiberarAsync(id, dto);
        return Ok(ApiResponse<ReservaInventarioDto>.Ok(reserva, "Reserva liberada correctamente."));
    }

    [HttpPost("{id:int}/expirar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.CambiarEstado)]
    public Task<IActionResult> Expirar(int id) => EjecutarTransicionAsync(
        id, _service.ExpirarAsync, "Reserva expirada correctamente.");

    [HttpPost("{id:int}/cancelar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Anular)]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarReservaInventarioDto dto)
    {
        var reserva = await _service.CancelarAsync(id, dto);
        return Ok(ApiResponse<ReservaInventarioDto>.Ok(reserva, "Reserva cancelada correctamente."));
    }

    private async Task<IActionResult> EjecutarTransicionAsync(
        int id,
        Func<int, Task<ReservaInventarioDto>> accion,
        string mensaje)
    {
        var reserva = await accion(id);
        return Ok(ApiResponse<ReservaInventarioDto>.Ok(reserva, mensaje));
    }
}
