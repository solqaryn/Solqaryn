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
[Route("ordenes-compra")]
public sealed class OrdenesCompraController : ControllerBase
{
    private readonly IOrdenCompraService _service;

    public OrdenesCompraController(IOrdenCompraService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] OrdenCompraFiltroDto filtro)
    {
        var pagina = await _service.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<OrdenCompraDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var orden = await _service.GetByIdAsync(id);
        return orden is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Orden de compra no encontrada", detail: "No existe una orden de compra con el identificador indicado.")
            : Ok(ApiResponse<OrdenCompraDto>.Ok(orden));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Crear)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrdenCompraDto dto,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Idempotency-Key requerido", detail: "La creación de una orden de compra exige el encabezado Idempotency-Key.");

        var creada = await _service.CreateAsync(dto, idempotencyKey);
        return CreatedAtAction(nameof(GetById), new { id = creada.Id },
            ApiResponse<OrdenCompraDto>.Ok(creada, "Orden de compra creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrdenCompraDto dto)
    {
        var actualizada = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<OrdenCompraDto>.Ok(actualizada, "Orden de compra actualizada correctamente."));
    }

    [HttpPost("{id:int}/enviar-aprobacion")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Confirmar)]
    public async Task<IActionResult> EnviarAprobacion(int id)
    {
        var orden = await _service.EnviarAprobacionAsync(id);
        return Ok(ApiResponse<OrdenCompraDto>.Ok(orden, "Orden enviada a aprobación correctamente."));
    }

    [HttpPost("{id:int}/aprobar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Aprobar)]
    public async Task<IActionResult> Aprobar(int id)
    {
        var orden = await _service.AprobarAsync(id);
        return Ok(ApiResponse<OrdenCompraDto>.Ok(orden, "Orden de compra aprobada correctamente."));
    }

    [HttpPost("{id:int}/cancelar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Anular)]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarOrdenCompraDto dto)
    {
        var orden = await _service.CancelarAsync(id, dto);
        return Ok(ApiResponse<OrdenCompraDto>.Ok(orden, "Orden de compra cancelada correctamente."));
    }
}
