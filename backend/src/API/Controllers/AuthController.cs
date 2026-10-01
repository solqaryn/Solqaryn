using System.Security.Claims;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Solqaryn.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditoriaService _auditoria;

    public AuthController(IAuthService authService, IAuditoriaService auditoria)
    {
        _authService = authService;
        _auditoria = auditoria;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthLogin")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var resultado = await _authService.LoginAsync(dto);

        if (resultado is null)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Usuarios,
                AccionPermiso.ConsultarHistorial,
                $"Intento fallido de inicio de sesion para '{dto.NombreUsuario}'.",
                entidad: "Sesion",
                resultado: "Rechazado",
                error: "Credenciales invalidas");

            return Unauthorized(ApiResponse<object>.Fail("Usuario o contrasena incorrectos."));
        }

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.ConsultarHistorial,
            $"Inicio de sesion exitoso para '{resultado.NombreUsuario}'.",
            entidad: "Sesion",
            resultado: "Exito");

        return Ok(ApiResponse<LoginResponseDto>.Ok(resultado, "Login exitoso."));
    }

    [Authorize]
    [HttpPost("renovar")]
    public async Task<IActionResult> Renovar()
    {
        var idClaim = User.FindFirstValue("id");
        if (!int.TryParse(idClaim, out var usuarioId))
            return Unauthorized(ApiResponse<object>.Fail("No fue posible identificar la sesión."));

        var resultado = await _authService.RenovarAsync(usuarioId);
        if (resultado is null)
            return Unauthorized(ApiResponse<object>.Fail("La sesión ya no es válida."));

        return Ok(ApiResponse<LoginResponseDto>.Ok(resultado, "Sesión renovada correctamente."));
    }
}
