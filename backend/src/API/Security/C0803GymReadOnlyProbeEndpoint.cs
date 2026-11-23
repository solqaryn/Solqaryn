using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.API.Security;

/// <summary>
/// Actual opt-in DEV-only endpoint shared by Program and the GitHub-hosted
/// TestServer. It only demonstrates current signed JWT/relational read authority.
/// No gym membership, door, payment, key or hardware entitlement is granted.
/// </summary>
public static class C0803GymReadOnlyProbeEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/__dev/gym-auth-readonly/{tenantId:int}/{branchId:int}",
            async (IC0803VerifiedGymActorSource source,
                CancellationToken cancellationToken) =>
            {
                var context = await source.ReadCurrentAsync(cancellationToken);
                if (context?.SignedJwtValidated != true ||
                    context.CurrentMembershipAndRbacFromDatabase != true ||
                    context.Actor is null ||
                    context.Actor.SaaSMembershipActive ||
                    context.Actor.GymPermissionGrantedByServer)
                    return Results.StatusCode(StatusCodes.Status403Forbidden);

                return Results.Text("READONLY_NO_GYM_GRANT", "text/plain");
            })
            .RequireAuthorization()
            .ExcludeFromDescription();
    }
}
