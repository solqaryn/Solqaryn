using System.Security.Claims;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("conciliaciones-bancarias")]
public class ConciliacionBancariaController : ControllerBase
{
    private readonly IConciliacionBancariaService _service;

    public ConciliacionBancariaController(IConciliacionBancariaService service)
    {
        _service = service;
    }

    private bool TryGetUsuarioId(out int usuarioId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId) && usuarioId > 0;

    [HttpPost("importaciones-estado-cuenta")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Importar)]
    public async Task<IActionResult> ImportarEstadoCuenta([FromBody] ImportarEstadoCuentaRequestDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        var result = await _service.ImportarEstadoCuentaAsync(dto, usuarioId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("matches")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> ConciliarMovimientos([FromBody] ConciliarMovimientosRequestDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        var result = await _service.ConciliarMovimientosAsync(dto, usuarioId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("ajustes")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> SolicitarAjuste([FromBody] SolicitarAjusteRequestDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        var result = await _service.SolicitarAjusteAsync(dto, usuarioId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("cierre-periodo")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Cerrar)]
    public async Task<IActionResult> CerrarPeriodo([FromBody] CerrarPeriodoConciliacionRequestDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        var result = await _service.CerrarPeriodoAsync(dto, usuarioId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reapertura-periodo")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Reabrir)]
    public async Task<IActionResult> ReabrirPeriodo([FromBody] ReabrirPeriodoConciliacionRequestDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        var result = await _service.ReabrirPeriodoAsync(dto, usuarioId, cancellationToken);
        return Ok(result);
    }
}
