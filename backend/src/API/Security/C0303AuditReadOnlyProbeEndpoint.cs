using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;

namespace Solqaryn.API.Security;

/// <summary>
/// C03-03.M03.03: Development-only, OFF-by-default authenticated tenant audit
/// preflight. GET is strictly read-only; POST explicitly persists only a
/// verified audit-read access event through the existing strict MySQL ledger.
/// Neither route authorizes a business mutation or runs outside Development.
/// </summary>
public static class C0303AuditReadOnlyProbeEndpoint
{
    public static bool ShouldMap(
        IHostEnvironment environment, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);
        return environment.IsDevelopment() &&
            configuration.GetValue<bool>(
                "SecurityAudit:EnableReadOnlyAuthorizationProbe");
    }

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/__dev/audit-authorization/{empresaId:int}",
            async (HttpContext http, int empresaId, IUsuarioScopeService scopes,
                C0303SecurityAuditApplicationService audit,
                CancellationToken cancellationToken) =>
            {
                if (!await IsVerifiedAuditReaderAsync(http, empresaId,
                    scopes, audit, cancellationToken))
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                http.Response.Headers.CacheControl = "no-store";
                return Results.NoContent();
            })
            .RequireAuthorization()
            .ExcludeFromDescription();

        // Deliberate POST: a durable audit entry is a write, even though the
        // verified business operation being recorded is an audit-read probe.
        // No business data are mutated or returned. Reuse the very same
        // JWT, tenant, membership and SQL RBAC checks as GET.
        endpoints.MapPost("/__dev/audit-authorization/{empresaId:int}/record",
            async (HttpContext http, int empresaId, IUsuarioScopeService scopes,
                C0303SecurityAuditApplicationService audit,
                C0303TransactionalAuditPersistenceAdapter writer,
                CancellationToken cancellationToken) =>
            {
                if (!await IsVerifiedAuditReaderAsync(http, empresaId,
                    scopes, audit, cancellationToken))
                    return Results.StatusCode(StatusCodes.Status403Forbidden);

                try
                {
                    // C03-03.M03.04: the adapter rechecks current relational
                    // grant/tenant in a real MySQL transaction and writes
                    // through the existing strict audit ledger. Never trust
                    // route/header identifiers as authorization.
                    if (!await writer.RecordVerifiedAuditReadAsync(
                        empresaId, cancellationToken))
                        return Results.StatusCode(StatusCodes.Status403Forbidden);
                    http.Response.Headers.CacheControl = "no-store";
                    return Results.NoContent();
                }
                catch (OperationCanceledException) when (
                    cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    // Do not disclose SQL, JWT, tenancy or exception content.
                    return Results.StatusCode(
                        StatusCodes.Status503ServiceUnavailable);
                }
            })
            .RequireAuthorization()
            .ExcludeFromDescription();
    }

    private static async Task<bool> IsVerifiedAuditReaderAsync(
        HttpContext http, int empresaId, IUsuarioScopeService scopes,
        C0303SecurityAuditApplicationService audit,
        CancellationToken cancellationToken)
    {
        if (empresaId <= 0 ||
            http.User.Identity?.IsAuthenticated != true)
            return false;

        // HTTP selectors can only narrow a server-verified scope.
        // Never treat a header or URL parameter as permission.
        var selector = http.Request.Headers["X-Empresa-Id"];
        if (selector.Count > 0 &&
            (selector.Count != 1 ||
             !int.TryParse(selector[0], out var selected) ||
             selected != empresaId))
            return false;

        var bearer = await http.AuthenticateAsync(
            JwtBearerDefaults.AuthenticationScheme);
        if (!bearer.Succeeded ||
            bearer.Principal?.Identity?.IsAuthenticated != true)
            return false;
        var ids = bearer.Principal.FindAll("id").ToArray();
        if (ids.Length != 1 ||
            !int.TryParse(ids[0].Value, out var actor) || actor <= 0)
            return false;

        var membership = await scopes.ObtenerActualAsync(
            empresaId, cancellationToken);
        if (membership is null || membership.UsuarioId != actor ||
            membership.EmpresaId != empresaId)
            return false;

        var previousTenant = VerifiedCommercialTenant.Resolve(http);
        if (previousTenant is not null && previousTenant != empresaId)
            return false;

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] =
            empresaId;
        var candidate = await audit.PrepareAsync(
            new C0303SecurityAuditApplicationService.ClientIntent(
                Guid.NewGuid().ToString("N")), cancellationToken);
        return candidate.RequiresDurableAuthenticatedAdapter;
    }
}
