using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Solqaryn.API.Security;

/// <summary>
/// C03-01.M03.03 opt-in DEV-only endpoint shared by Program and the GitHub-hosted
/// TestServer. It only demonstrates current signed JWT/relational read authority.
/// No login session, tenant, platform or branch grant is granted.
/// </summary>
public static class C0301LoginReadOnlyProbeEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/__dev/login-tenant-readonly/{tenantId:int}/{branchId:int}",
            async (IC0301VerifiedLoginActorSource source,
                CancellationToken cancellationToken) =>
            {
                var context = await source.ReadCurrentAsync(cancellationToken);
                if (context?.SignedJwtValidated != true ||
                    context.CurrentMembershipAndRbacFromDatabase != true ||
                    context.GrantsTenantAccess || context.GrantsPlatformAccess || context.IssuesJwt)
                    return Results.StatusCode(StatusCodes.Status403Forbidden);

                return Results.Text("LOGIN_SELECTION_ONLY_NO_TOKEN_OR_GRANT", "text/plain");
            })
            .RequireAuthorization()
            .ExcludeFromDescription();
    }
}
