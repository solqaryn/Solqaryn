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
[Route("cuentas-por-pagar")]
public sealed class CuentasPorPagarController : ControllerBase
{
    private readonly ICuentaPorPagarService _service;

    public CuentasPorPagarController(
        AppDbContext db,
        IFacturaProveedorRepository facturas,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        ArgumentNullException.ThrowIfNull(db);
        _service = new CuentaPorPagarService(
            new CuentaPorPagarRepository(db),
            facturas,
            currentUser,
            unitOfWork,
            auditoria);
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] CuentaPorPagarFiltroDto filtro)
    {
        var pagina = await _service.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<CuentaPorPagarDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var cuenta = await _service.GetByIdAsync(id);
        return cuenta is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Cuenta por pagar no encontrada",
                detail: "No existe una cuenta por pagar con el identificador indicado.")
            : Ok(ApiResponse<CuentaPorPagarDto>.Ok(cuenta));
    }

    [HttpPost("generar")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> Generar([FromBody] GenerarCuentaPorPagarDto dto)
    {
        var cuenta = await _service.GenerarAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = cuenta.Id },
            ApiResponse<CuentaPorPagarDto>.Ok(cuenta, "Cuenta por pagar generada correctamente."));
    }

    [HttpPost("{id:int}/aplicaciones")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Editar)]
    public async Task<IActionResult> Aplicar(int id, [FromBody] AplicarCuentaPorPagarDto dto)
    {
        var cuenta = await _service.AplicarAsync(id, dto);
        return Ok(ApiResponse<CuentaPorPagarDto>.Ok(cuenta, "Aplicación registrada correctamente."));
    }

    [HttpPost("{id:int}/aplicaciones/revertir")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Editar)]
    public async Task<IActionResult> RevertirAplicacion(int id, [FromBody] RevertirAplicacionCuentaPorPagarDto dto)
    {
        var cuenta = await _service.RevertirAplicacionAsync(id, dto);
        return Ok(ApiResponse<CuentaPorPagarDto>.Ok(cuenta, "Aplicación revertida correctamente."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Anular)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularCuentaPorPagarDto dto)
    {
        var cuenta = await _service.AnularAsync(id, dto);
        return Ok(ApiResponse<CuentaPorPagarDto>.Ok(cuenta, "Cuenta por pagar anulada correctamente."));
    }
}
