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
[Route("preparaciones-pedido-venta")]
public sealed class PreparacionesPedidoVentaController : ControllerBase
{
    private readonly IPreparacionPedidoVentaService _service;

    public PreparacionesPedidoVentaController(IPreparacionPedidoVentaService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id) =>
        Ok(ApiResponse<PreparacionPedidoVentaDto>.Ok(await _service.GetByIdAsync(id)));

    [HttpGet("pedido/{pedidoVentaId:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetByPedido(int pedidoVentaId) =>
        Ok(ApiResponse<PreparacionPedidoVentaDto>.Ok(await _service.GetByPedidoVentaIdAsync(pedidoVentaId)));

    [HttpPost("pedido/{pedidoVentaId:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Crear)]
    public async Task<IActionResult> Iniciar(int pedidoVentaId)
    {
        var creado = await _service.IniciarAsync(pedidoVentaId);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<PreparacionPedidoVentaDto>.Ok(creado, "Preparación logística iniciada correctamente."));
    }

    [HttpPost("{id:int}/picking")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Editar)]
    public async Task<IActionResult> CompletarPicking(int id) =>
        Ok(ApiResponse<PreparacionPedidoVentaDto>.Ok(await _service.CompletarPickingAsync(id), "Picking completado correctamente."));

    [HttpPost("{id:int}/packing")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Editar)]
    public async Task<IActionResult> CompletarPacking(int id) =>
        Ok(ApiResponse<PreparacionPedidoVentaDto>.Ok(await _service.CompletarPackingAsync(id), "Packing completado correctamente."));

    [HttpPost("{id:int}/despachar")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Despachar(int id) =>
        Ok(ApiResponse<PreparacionPedidoVentaDto>.Ok(await _service.MarcarDespachadoAsync(id), "Preparación marcada como despachada."));

    [HttpPost("{id:int}/entregar")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Entregar(int id) =>
        Ok(ApiResponse<PreparacionPedidoVentaDto>.Ok(await _service.MarcarEntregadoAsync(id), "Preparación marcada como entregada."));

    [HttpPost("{id:int}/cancelar")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Anular)]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarPreparacionPedidoVentaDto dto) =>
        Ok(ApiResponse<PreparacionPedidoVentaDto>.Ok(await _service.CancelarAsync(id, dto.Motivo), "Preparación cancelada correctamente."));
}
