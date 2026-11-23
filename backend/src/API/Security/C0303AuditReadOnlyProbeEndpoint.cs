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
/// C03-03.M03.03: real Development-only HTTP read-only authorization
/// preflight. It cannot write an audit record, authorize a business mutation,
/// or claim that a durable audit sink exists. Feature is OFF by default.
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
                if (empresaId <= 0 ||
                    http.User.Identity?.IsAuthenticated != true)
                    return Results.StatusCode(StatusCodes.Status403Forbidden);

                // HTTP selectors can only narrow a server-verified scope.
                // Never treat a header or URL parameter as permission.
                var selector = http.Request.Headers["X-Empresa-Id"];
                if (selector.Count > 0 &&
                    (selector.Count != 1 ||
                     !int.TryParse(selector[0], out var selected) ||
                     selected != empresaId))
                    return Results.StatusCode(StatusCodes.Status403Forbidden);

                var bearer = await http.AuthenticateAsync(
                    JwtBearerDefaults.AuthenticationScheme);
                if (!bearer.Succeeded ||
                    bearer.Principal?.Identity?.IsAuthenticated != true)
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                var ids = bearer.Principal.FindAll("id").ToArray();
                if (ids.Length != 1 ||
                    !int.TryParse(ids[0].Value, out var actor) || actor <= 0)
                    return Results.StatusCode(StatusCodes.Status403Forbidden);

                // The membership lookup is current and tenant-scoped. The
                // reader independently revalidates JWT, admin role and SQL
                // audit-read grant and then rejects a concurrent revocation.
                var membership = await scopes.ObtenerActualAsync(
                    empresaId, cancellationToken);
                if (membership is null || membership.UsuarioId != actor ||
                    membership.EmpresaId != empresaId)
                    return Results.StatusCode(StatusCodes.Status403Forbidden);

                var previousTenant = VerifiedCommercialTenant.Resolve(http);
                if (previousTenant is not null &&
                    previousTenant != empresaId)
                    return Results.StatusCode(StatusCodes.Status403Forbidden);

                http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] =
                    empresaId;
                var candidate = await audit.PrepareAsync(
                    new C0303SecurityAuditApplicationService.ClientIntent(
                        Guid.NewGuid().ToString("N")), cancellationToken);
                http.Response.Headers.CacheControl = "no-store";
                return candidate.RequiresDurableAuthenticatedAdapter
                    ? Results.NoContent()
                    : Results.StatusCode(StatusCodes.Status403Forbidden);
            })
            .RequireAuthorization()
            .ExcludeFromDescription();
    }
}
