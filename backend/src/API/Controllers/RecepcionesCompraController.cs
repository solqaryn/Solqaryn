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
[Route("recepciones-compra")]
public sealed class RecepcionesCompraController : ControllerBase
{
    private readonly IRecepcionCompraService _service;

    public RecepcionesCompraController(IRecepcionCompraService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] RecepcionCompraQueryDto filtro)
    {
        var pagina = await _service.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<RecepcionCompraDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var recepcion = await _service.GetByIdAsync(id);
        return recepcion is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Recepción de compra no encontrada", detail: "No existe una recepción con el identificador indicado.")
            : Ok(ApiResponse<RecepcionCompraDto>.Ok(recepcion));
    }

    [HttpGet("ordenes/{ordenCompraId:int}/saldo")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetSaldoOrden(int ordenCompraId)
    {
        var saldo = await _service.GetSaldoOrdenAsync(ordenCompraId);
        return saldo is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Orden de compra no encontrada", detail: "No existe una orden de compra con el identificador indicado.")
            : Ok(ApiResponse<RecepcionCompraSaldoOrdenDto>.Ok(saldo));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Crear)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRecepcionCompraDto dto,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Idempotency-Key requerido",
                detail: "La creación de una recepción exige el encabezado Idempotency-Key.");

        var creada = await _service.CreateAsync(dto, idempotencyKey);
        return CreatedAtAction(nameof(GetById), new { id = creada.Id },
            ApiResponse<RecepcionCompraDto>.Ok(creada, "Recepción creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRecepcionCompraDto dto)
    {
        var actualizada = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<RecepcionCompraDto>.Ok(actualizada, "Recepción actualizada correctamente."));
    }

    [HttpPost("{id:int}/confirmar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Confirmar(int id)
    {
        var recepcion = await _service.ConfirmarAsync(id);
        return Ok(ApiResponse<RecepcionCompraDto>.Ok(recepcion, "Recepción confirmada y stock físico materializado correctamente."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Anular)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularRecepcionCompraDto dto)
    {
        var recepcion = await _service.AnularAsync(id, dto);
        return Ok(ApiResponse<RecepcionCompraDto>.Ok(recepcion, "Recepción anulada y stock físico revertido correctamente."));
    }
}
