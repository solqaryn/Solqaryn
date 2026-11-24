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
        // The JWT identifies the actor and original base role; neither a client
        // header nor an old token may acquire a different current SQL role.
        var principal = ControllerContext?.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true ||
            !TryCanonicalClaim(principal, "id", out var usuarioId) ||
            !TryCanonicalClaim(principal, "rolId", out var rolId))
            return Unauthorized(ApiResponse<object>.Fail("No fue posible identificar la sesión."));

        var resultado = await _authService.RenovarAsync(usuarioId, rolId);
        if (resultado is null)
            return Unauthorized(ApiResponse<object>.Fail("La sesión ya no es válida."));

        return Ok(ApiResponse<LoginResponseDto>.Ok(resultado, "Sesión renovada correctamente."));
    }

    private static bool TryCanonicalClaim(ClaimsPrincipal principal, string name, out int id)
    {
        id = 0;
        var claims = principal.FindAll(name).ToArray();
        if (claims.Length != 1) return false;
        var value = claims[0].Value;
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id) &&
            id > 0 &&
            string.Equals(value, id.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }
}
