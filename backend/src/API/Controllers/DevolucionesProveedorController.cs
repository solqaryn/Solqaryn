using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("devoluciones-proveedor")]
public sealed class DevolucionesProveedorController : ControllerBase
{
    private readonly IDevolucionProveedorService _service;

    public DevolucionesProveedorController(
        AppDbContext db,
        IRecepcionCompraRepository recepciones,
        IFacturaProveedorRepository facturas,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        ArgumentNullException.ThrowIfNull(db);
        _service = new DevolucionProveedorService(
            new DevolucionProveedorRepository(db),
            recepciones,
            facturas,
            currentUser,
            unitOfWork,
            auditoria);
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] DevolucionProveedorQueryDto filtro)
    {
        var pagina = await _service.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<DevolucionProveedorDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var devolucion = await _service.GetByIdAsync(id);
        return devolucion is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Devolución a proveedor no encontrada", detail: "No existe una devolución con el identificador indicado.")
            : Ok(ApiResponse<DevolucionProveedorDto>.Ok(devolucion));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Crear)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDevolucionProveedorDto dto,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Idempotency-Key requerido",
                detail: "La creación de una devolución a proveedor exige el encabezado Idempotency-Key.");

        var creada = await _service.CreateAsync(dto, idempotencyKey);
        return CreatedAtAction(
            nameof(GetById),
            new { id = creada.Id },
            ApiResponse<DevolucionProveedorDto>.Ok(creada, "Devolución a proveedor creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDevolucionProveedorDto dto)
    {
        var actualizada = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<DevolucionProveedorDto>.Ok(actualizada, "Devolución a proveedor actualizada correctamente."));
    }

    [HttpPost("{id:int}/confirmar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Confirmar(int id)
    {
        var devolucion = await _service.ConfirmarAsync(id);
        return Ok(ApiResponse<DevolucionProveedorDto>.Ok(devolucion, "Devolución a proveedor confirmada correctamente."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Anular)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularDevolucionProveedorDto dto)
    {
        var devolucion = await _service.AnularAsync(id, dto);
        return Ok(ApiResponse<DevolucionProveedorDto>.Ok(devolucion, "Devolución a proveedor anulada correctamente."));
    }
}
