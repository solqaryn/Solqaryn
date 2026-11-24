using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;

namespace Solqaryn.API.Controllers;

/// <summary>Explicit context credential, not an authorization for legacy resource routes.</summary>
[ApiController]
[Route("tenant-context/sesion")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TenantSessionController(
    IUsuarioScopeService scopes, ITenantSessionTokenService tokens) : ControllerBase
{
    public sealed record Seleccion(int EmpresaId, int SucursalId);
    public sealed record Sesion(string Token, DateTime ExpiraEn, int EmpresaId, int SucursalId, int RolId);

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [EnableRateLimiting("AuthLogin")]
    public async Task<IActionResult> Emitir([FromBody] Seleccion seleccion,
        CancellationToken cancellationToken)
    {
        if (seleccion.EmpresaId <= 0 || seleccion.SucursalId <= 0)
            return BadRequest();
        if (!ClaimId(User, "id", out var actor) || !ClaimId(User, "rolId", out var baseRole))
            return Unauthorized();
        var exp = User.FindAll("exp").ToArray();
        if (exp.Length != 1 || !long.TryParse(exp[0].Value, NumberStyles.None,
            CultureInfo.InvariantCulture, out var seconds) || seconds <= 0 ||
            seconds > 253402300799L)
            return Unauthorized();
        var baseExpiry = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        if (!SelectorsMatch(seleccion.EmpresaId, seleccion.SucursalId))
            return Forbid(JwtBearerDefaults.AuthenticationScheme);

        var scope = await scopes.ObtenerSucursalActualAsync(actor, baseRole,
            seleccion.EmpresaId, seleccion.SucursalId, cancellationToken);
        if (scope is null || scope.UsuarioId != actor || scope.EmpresaId != seleccion.EmpresaId ||
            scope.RolId <= 0)
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        if (baseExpiry <= DateTime.UtcNow.AddSeconds(1))
            return Unauthorized();
        cancellationToken.ThrowIfCancellationRequested();
        var (token, expiraEn) = tokens.GenerarTokenContexto(new TenantSessionAuthority(actor,
            baseRole, scope.EmpresaId, seleccion.SucursalId, scope.RolId, baseExpiry));
        return Ok(ApiResponse<Sesion>.Ok(new Sesion(token, expiraEn, scope.EmpresaId,
            seleccion.SucursalId, scope.RolId), "Sesión de contexto verificada."));
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = TenantSessionToken.Scheme)]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        if (!ClaimId(User, "id", out var actor) || !ClaimId(User, "rolId", out var baseRole) ||
            !ClaimId(User, "tenantId", out var tenant) || !ClaimId(User, "branchId", out var branch) ||
            !ClaimId(User, "tenantRoleId", out var role))
            return Unauthorized();
        if (!SelectorsMatch(tenant, branch))
            return Forbid(TenantSessionToken.Scheme);
        var scope = await scopes.ObtenerSucursalActualAsync(actor, baseRole, tenant, branch,
            cancellationToken);
        if (scope is null || scope.UsuarioId != actor || scope.EmpresaId != tenant || scope.RolId != role)
            return Forbid(TenantSessionToken.Scheme);
        return Ok(ApiResponse<object>.Ok(new { EmpresaId = tenant, SucursalId = branch,
            RolId = role }, "Contexto vigente."));
    }

    private bool SelectorsMatch(int tenant, int branch) =>
        HeaderMatches("X-Empresa-Id", tenant) && HeaderMatches("X-Sucursal-Id", branch);
    private bool HeaderMatches(string name, int id) =>
        !Request.Headers.TryGetValue(name, out var values) ||
        (values.Count == 1 && string.Equals(values[0], id.ToString(CultureInfo.InvariantCulture),
            StringComparison.Ordinal));
    private static bool ClaimId(ClaimsPrincipal principal, string name, out int id)
    {
        id = 0;
        var claims = principal.FindAll(name).ToArray();
        return principal.Identity?.IsAuthenticated == true && claims.Length == 1 &&
            int.TryParse(claims[0].Value, NumberStyles.None, CultureInfo.InvariantCulture, out id) &&
            id > 0 && claims[0].Value == id.ToString(CultureInfo.InvariantCulture);
    }
}
