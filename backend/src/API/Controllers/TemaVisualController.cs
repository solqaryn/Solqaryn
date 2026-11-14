using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Route("tema-visual")]
public class TemaVisualController : ControllerBase
{
    private readonly ITemaVisualService _service;

    public TemaVisualController(ITemaVisualService service)
    {
        _service = service;
    }

    /// Público deliberado (sin [Authorize]): la sección 16 del prompt exige
    /// que el tema se aplique también en "pantallas de autenticación", que
    /// por definición se ven ANTES de tener un JWT. No es información
    /// sensible (son solo códigos de color).
    [HttpGet]
    [AllowAnonymous]
    [PublicHttpCache(PublicHttpCacheProfile.Identity)]
    public async Task<IActionResult> Get()
    {
        var tema = await _service.GetAsync();
        return Ok(ApiResponse<TemaVisualDto>.Ok(tema));
    }

    [HttpPut]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> Update([FromBody] ActualizarTemaVisualDto dto)
    {
        var actualizado = await _service.UpdateAsync(dto);
        return Ok(ApiResponse<TemaVisualDto>.Ok(actualizado, "Tema visual actualizado correctamente."));
    }

    [HttpPost("restaurar")]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> Restaurar()
    {
        var restaurado = await _service.RestaurarPredeterminadoAsync();
        return Ok(ApiResponse<TemaVisualDto>.Ok(restaurado, "Tema visual restaurado a los valores predeterminados."));
    }
}
