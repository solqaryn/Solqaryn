using System.Globalization;
using Solqaryn.Application.Common;
using Solqaryn.Application.AutomationV5;
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

    /// <summary>
    /// Selector seguro: solo las empresas con membresía vigente del actor JWT.
    /// No utiliza el catálogo administrativo global ni el ID suministrado por UI.
    /// </summary>
    [HttpGet("empresas")]
    public async Task<IActionResult> ListarEmpresas(CancellationToken cancellationToken)
    {
        // The productive selector must not trust ambiguous actor/role claims.
        // Authorize verifies the signature; this verifies their canonical shape
        // before the SQL membership reader enumerates any tenant.
        if (!TryCanonicalJwtAuthority(out _, out _))
            return Unauthorized(ApiResponse<object>.Fail(
                "La sesión no tiene identidad o rol verificables."));

        var empresas = await _usuarioScopeService.ListarEmpresasSeleccionablesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<EmpresaSeleccionable>>.Ok(
            empresas, "Empresas activas autorizadas."));
    }

    /// <summary>
    /// DEV-only authenticated tenant-resolution evidence from current MySQL
    /// membership and role. Never persists preference, switches session, issues
    /// tokens or authorizes resources. Outside Development always returns 404.
    /// </summary>
    [HttpGet("resolver-login")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ResolverLoginSoloDev(
        [FromServices] IServiceProvider services,
        [FromServices] Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(environment.EnvironmentName, "Development",
                StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var reader = services.GetService<ITrustedTenantLoginAuthorityReader>();
        if (reader is null)
            return StatusCode(503);

        var decision = await new V17IntelligentTenantLoginAuthorization(reader)
            .ResolveAsync(new(), cancellationToken);

        return Ok(new
        {
            decision.Code,
            decision.ProposedTenantId,
            decision.Choices,
            decision.RequiresAuthenticatedBackendReissue,
            decision.IssuesJwt,
            decision.AuthorizesResourceAccess,
            decision.InstallsTenantContext
        });
    }

    // C03-01.M03.03: verify the canonical, signed JWT actor/role claims;
    // authorize the selected tenant using live SQL membership and its effective role.
    private bool TryCanonicalJwtAuthority(out int actorId, out int roleId)
    {
        actorId = 0;
        roleId = 0;
        var principal = ControllerContext?.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) return false;
        var actorClaims = principal.FindAll("id").ToArray();
        var roleClaims = principal.FindAll("rolId").ToArray();
        return actorClaims.Length == 1 && roleClaims.Length == 1 &&
            CanonicalPositiveId(actorClaims[0].Value, out actorId) &&
            CanonicalPositiveId(roleClaims[0].Value, out roleId);
    }

    private static bool CanonicalPositiveId(string raw, out int id) =>
        int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out id) &&
        id > 0 &&
        string.Equals(raw, id.ToString(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);

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

        if (!TryCanonicalJwtAuthority(out var jwtActor, out _))
            return Unauthorized(ApiResponse<object>.Fail("La sesión no tiene identidad o rol verificables."));

        var contexto = await _usuarioScopeService.ObtenerActualAsync(
            empresaId,
            cancellationToken);

        // A JWT role can name tenant A's role while the live tenant B
        // membership legitimately has another role. SQL is authoritative;
        // never grant B using a JWT role, a header or a cached tenant choice.
        if (contexto is null || contexto.UsuarioId != jwtActor ||
            contexto.EmpresaId != empresaId)
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
