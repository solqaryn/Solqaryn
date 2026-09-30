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
[Route("pedidos-venta")]
public sealed class PedidosVentaController : ControllerBase
{
    private readonly IPedidoVentaService _service;

    public PedidosVentaController(IPedidoVentaService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] PedidoVentaFiltroDto filtro)
    {
        var pagina = await _service.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<PedidoVentaDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var pedido = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<PedidoVentaDto>.Ok(pedido));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Crear)]
    public async Task<IActionResult> Crear(
        [FromBody] CreatePedidoVentaDto dto,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Idempotency-Key requerido",
                detail: "La creación de un pedido exige el encabezado Idempotency-Key.");

        var creado = await _service.CrearDesdeCotizacionAsync(dto, idempotencyKey);
        return CreatedAtAction(
            nameof(GetById),
            new { id = creado.Id },
            ApiResponse<PedidoVentaDto>.Ok(creado, "Pedido de venta creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Editar)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] UpdatePedidoVentaDto dto)
    {
        dto.Id = id;
        var actualizado = await _service.ActualizarAsync(dto);
        return Ok(ApiResponse<PedidoVentaDto>.Ok(actualizado, "Pedido de venta actualizado correctamente."));
    }

    [HttpPost("{id:int}/confirmar")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Confirmar(int id, [FromBody] ConfirmarPedidoVentaDto dto)
    {
        var pedido = await _service.ConfirmarAsync(id, dto);
        return Ok(ApiResponse<PedidoVentaDto>.Ok(pedido, "Pedido de venta confirmado y reserva de inventario activada correctamente."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Anular)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularPedidoVentaDto dto)
    {
        var pedido = await _service.AnularAsync(id, dto.Motivo);
        return Ok(ApiResponse<PedidoVentaDto>.Ok(pedido, "Pedido de venta anulado correctamente."));
    }
}
