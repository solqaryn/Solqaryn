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
[Route("finanzas")]
public class FinanzasController : ControllerBase
{
    private readonly IFinanzasService _finanzasService;
    private readonly ICurrentUserService _currentUser;

    public FinanzasController(IFinanzasService finanzasService, ICurrentUserService currentUser)
    {
        _finanzasService = finanzasService;
        _currentUser = currentUser;
    }

    [HttpGet("resumen")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetResumen()
    {
        var resumen = await _finanzasService.GetResumenAsync();
        return Ok(ApiResponse<FinanzasResumenDto>.Ok(resumen));
    }

    [HttpGet("movimientos")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetMovimientos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var movimientos = await _finanzasService.GetMovimientosAsync(desde, hasta);
        return Ok(ApiResponse<List<MovimientoFinancieroDto>>.Ok(movimientos));
    }

    [HttpPost("movimientos/manual")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> RegistrarManual([FromBody] CreateMovimientoManualDto dto)
    {
        var creado = await _finanzasService.RegistrarMovimientoManualAsync(dto);
        return Ok(ApiResponse<MovimientoFinancieroDto>.Ok(creado, "Movimiento registrado correctamente."));
    }

    [HttpPost("movimientos/{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Anular)]
    public async Task<IActionResult> AnularMovimiento(int id, [FromBody] AnularDocumentoDto dto)
    {
        var anulado = await _finanzasService.AnularMovimientoAsync(id, dto.MotivoAnulacion);
        if (anulado is null) return NotFound(ApiResponse<object>.Fail("Movimiento no encontrado."));
        return Ok(ApiResponse<MovimientoFinancieroDto>.Ok(anulado, "Movimiento anulado correctamente."));
    }

    [HttpGet("revisiones")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetRevisiones()
    {
        if (!_currentUser.EsAdministrador)
            return Forbid();

        var revisiones = await _finanzasService.GetRevisionesAsync();
        return Ok(ApiResponse<List<RevisionFinancieraDto>>.Ok(revisiones));
    }

    [HttpPost("revisiones")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> RegistrarRevision([FromBody] CreateRevisionFinancieraDto dto)
    {
        if (!_currentUser.EsAdministrador)
            return Forbid();

        var creada = await _finanzasService.RegistrarRevisionAsync(dto);
        return Ok(ApiResponse<RevisionFinancieraDto>.Ok(creada, "Revisión financiera registrada correctamente."));
    }
}
