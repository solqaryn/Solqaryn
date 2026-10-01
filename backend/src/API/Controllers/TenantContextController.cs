using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

/// <summary>
/// Materializa la selección de empresa como una solicitud no autoritativa del
/// cliente. El contexto efectivo sólo se devuelve cuando el servidor verifica
/// una membresía UsuarioEmpresa activa para el usuario autenticado.
/// </summary>
[ApiController]
[Authorize]
[Route("tenant-context")]
public sealed class TenantContextController : ControllerBase
{
    private readonly IUsuarioScopeService _usuarioScopeService;

    public TenantContextController(IUsuarioScopeService usuarioScopeService)
    {
        _usuarioScopeService = usuarioScopeService;
    }

    [HttpGet("{empresaId:int}")]
    public async Task<IActionResult> Obtener(
        int empresaId,
        CancellationToken cancellationToken)
    {
        if (empresaId <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "El identificador de empresa no es válido."));
        }

        var contexto = await _usuarioScopeService.ObtenerActualAsync(
            empresaId,
            cancellationToken);

        if (contexto is null)
        {
            // No distinguir empresa inexistente, inactiva o membresía ausente:
            // el rechazo fail-closed evita filtrar existencia cross-tenant.
            return Forbid();
        }

        return Ok(ApiResponse<UsuarioTenantScopeActual>.Ok(
            contexto,
            "Contexto tenant verificado."));
    }
}
