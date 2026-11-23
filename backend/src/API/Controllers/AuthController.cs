using System.Globalization;
using System.Linq;
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
                "Intento fallido de inicio de sesion.",
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
        // A signed product token contains exactly one canonical positive actor id.
        // Never use FindFirst: duplicated/ambiguous claims could renew a stale or
        // conflicting identity before tenant-aware checks run.
        var actorClaims = User.FindAll("id").ToArray();
        if (User.Identity?.IsAuthenticated != true || actorClaims.Length != 1)
            return Unauthorized(ApiResponse<object>.Fail("No fue posible identificar la sesión."));

        var actorText = actorClaims[0].Value;
        if (!int.TryParse(actorText, NumberStyles.None, CultureInfo.InvariantCulture,
                out var usuarioId) || usuarioId <= 0 ||
            !string.Equals(actorText, usuarioId.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
            return Unauthorized(ApiResponse<object>.Fail("No fue posible identificar la sesión."));

        var resultado = await _authService.RenovarAsync(usuarioId);
        if (resultado is null)
            return Unauthorized(ApiResponse<object>.Fail("La sesión ya no es válida."));

        return Ok(ApiResponse<LoginResponseDto>.Ok(resultado, "Sesión renovada correctamente."));
    }
}
