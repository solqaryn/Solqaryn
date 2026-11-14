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
[Route("operaciones-bancarias")]
public class OperacionBancariaController : ControllerBase
{
    private readonly IOperacionBancariaService _service;

    public OperacionBancariaController(IOperacionBancariaService service)
    {
        _service = service;
    }

    private bool TryGetUsuarioId(out int usuarioId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId) && usuarioId > 0;

    [HttpPost("deposito")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> RegistrarDeposito([FromBody] DepositoBancarioDto dto)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        await _service.RegistrarDepositoAsync(dto, usuarioId);
        return Ok();
    }

    [HttpPost("retiro")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> RegistrarRetiro([FromBody] RetiroBancarioDto dto)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        await _service.RegistrarRetiroAsync(dto, usuarioId);
        return Ok();
    }

    [HttpPost("transferencia")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> RegistrarTransferencia([FromBody] TransferenciaBancariaDto dto)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        await _service.RegistrarTransferenciaAsync(dto, usuarioId);
        return Ok();
    }

    [HttpPost("comision")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> RegistrarComision([FromBody] ComisionBancariaDto dto)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        await _service.RegistrarComisionAsync(dto, usuarioId);
        return Ok();
    }

    [HttpPost("interes")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> RegistrarInteres([FromBody] InteresBancarioDto dto)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        await _service.RegistrarInteresAsync(dto, usuarioId);
        return Ok();
    }

    [HttpPost("conciliacion")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> RegistrarConciliacion([FromBody] ConciliacionBancariaDto dto)
    {
        if (!TryGetUsuarioId(out var usuarioId)) return Unauthorized();
        await _service.RegistrarConciliacionAsync(dto, usuarioId);
        return Ok();
    }
}
