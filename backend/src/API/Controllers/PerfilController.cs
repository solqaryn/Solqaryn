using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

/// Autogestión exclusiva del usuario autenticado. No depende de permisos de
/// administración porque nunca recibe un UsuarioId externo: todas las acciones
/// se resuelven desde la identidad del JWT actual.
[ApiController]
[Authorize]
[Route("perfil")]
public class PerfilController : ControllerBase
{
    private readonly IPerfilService _service;

    public PerfilController(IPerfilService service)
    {
        _service = service;
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Get()
    {
        var perfil = await _service.GetPerfilAsync();
        return Ok(ApiResponse<PerfilDto>.Ok(perfil));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ActualizarPerfilDto dto)
    {
        var actualizado = await _service.ActualizarPerfilAsync(dto);
        return Ok(ApiResponse<PerfilDto>.Ok(
            actualizado,
            "Perfil actualizado correctamente. El nuevo usuario se utilizará en el próximo inicio de sesión."));
    }

    [HttpPut("foto")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> ActualizarFoto([FromForm] ActualizarFotoPerfilDto dto)
    {
        var actualizado = await _service.ActualizarFotoAsync(dto);
        return Ok(ApiResponse<PerfilDto>.Ok(actualizado, "Fotografía de perfil actualizada correctamente."));
    }

    [HttpDelete("foto")]
    public async Task<IActionResult> EliminarFoto()
    {
        var actualizado = await _service.EliminarFotoAsync();
        return Ok(ApiResponse<PerfilDto>.Ok(actualizado, "Fotografía de perfil eliminada correctamente."));
    }

    [HttpPut("password")]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordPropiaDto dto)
    {
        await _service.CambiarPasswordAsync(dto);
        return Ok(ApiResponse<object>.Ok(new { }, "Contraseña actualizada correctamente."));
    }
}
