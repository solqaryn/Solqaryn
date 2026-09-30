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
using Microsoft.Extensions.Logging;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("notas-credito-proveedor")]
public sealed class NotasCreditoProveedorController : ControllerBase
{
    private readonly INotaCreditoProveedorService _service;

    public NotasCreditoProveedorController(
        AppDbContext db,
        IFacturaProveedorRepository facturas,
        IProveedorRepository proveedores,
        IDevolucionProveedorRepository devoluciones,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria,
        ILogger<NotaCreditoProveedorService> logger)
    {
        ArgumentNullException.ThrowIfNull(db);
        _service = new NotaCreditoProveedorService(
            new NotaCreditoProveedorRepository(db),
            facturas,
            proveedores,
            devoluciones,
            currentUser,
            unitOfWork,
            auditoria,
            logger);
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] NotaCreditoProveedorFiltroDto filtro)
    {
        var pagina = await _service.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<NotaCreditoProveedorDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var nota = await _service.GetByIdAsync(id);
        return nota is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Nota de crédito de proveedor no encontrada",
                detail: "No existe una nota de crédito de proveedor con el identificador indicado.")
            : Ok(ApiResponse<NotaCreditoProveedorDto>.Ok(nota));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateNotaCreditoProveedorDto dto)
    {
        var creada = await _service.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = creada.Id },
            ApiResponse<NotaCreditoProveedorDto>.Ok(creada, "Nota de crédito de proveedor creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateNotaCreditoProveedorDto dto)
    {
        var actualizada = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<NotaCreditoProveedorDto>.Ok(
            actualizada,
            "Nota de crédito de proveedor actualizada correctamente."));
    }

    [HttpPost("{id:int}/registrar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Registrar(int id)
    {
        var registrada = await _service.RegistrarAsync(id);
        return Ok(ApiResponse<NotaCreditoProveedorDto>.Ok(
            registrada,
            "Nota de crédito de proveedor registrada correctamente."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Anular)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularNotaCreditoProveedorDto dto)
    {
        var anulada = await _service.AnularAsync(id, dto);
        return Ok(ApiResponse<NotaCreditoProveedorDto>.Ok(
            anulada,
            "Nota de crédito de proveedor anulada correctamente."));
    }
}
