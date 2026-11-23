using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Rules = Solqaryn.Domain.AutomationV5.C0803GymMembershipDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0803GymMembershipApplicationService;

namespace Solqaryn.API.Security;

/// <summary>
/// C08-03.M03.03: read-only DEV probe of a signed bearer, current tenant role,
/// relational branch grant and branch ownership. OFF by default. A branch
/// read permission is NEVER a gym membership or door-opening permission.
/// </summary>
public sealed class C0803GymVerifiedActorSource(
    IHttpContextAccessor contexts,
    ICurrentUserService current,
    IUsuarioScopeService memberships,
    AppDbContext database,
    IConfiguration configuration) : IC0803VerifiedGymActorSource
{
    public async ValueTask<Service.AuthenticatedContext?> ReadCurrentAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!configuration.GetValue<bool>("GymAuthorization:EnableReadOnlyProbe"))
            return null;

        var http = contexts.HttpContext;
        var rawTenant = Convert.ToString(http?.Request.RouteValues["tenantId"],
            CultureInfo.InvariantCulture);
        var rawBranch = Convert.ToString(http?.Request.RouteValues["branchId"],
            CultureInfo.InvariantCulture);
        if (http?.User.Identity?.IsAuthenticated != true ||
            !current.EstaAutenticado || current.UsuarioId is not > 0 ||
            !int.TryParse(rawTenant, NumberStyles.None, CultureInfo.InvariantCulture,
                out var tenantId) || tenantId <= 0 ||
            !string.Equals(rawTenant, tenantId.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal) ||
            !int.TryParse(rawBranch, NumberStyles.None, CultureInfo.InvariantCulture,
                out var branchId) || branchId <= 0 ||
            !string.Equals(rawBranch, branchId.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
            return null;

        var verifiedTenant = VerifiedCommercialTenant.Resolve(http);
        if (verifiedTenant.HasValue && verifiedTenant.Value != tenantId)
            return null;

        try
        {
            // A prepopulated ClaimsPrincipal is not proof: invoke the actual
            // registered JWT bearer validator (signature, issuer, audience, time).
            var bearer = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            cancellationToken.ThrowIfCancellationRequested();
            if (!bearer.Succeeded || bearer.Principal?.Identity?.IsAuthenticated != true)
                return null;

            var signedIds = bearer.Principal.FindAll("id").ToArray();
            if (signedIds.Length != 1 ||
                !int.TryParse(signedIds[0].Value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var actorId) ||
                actorId != current.UsuarioId.Value)
                return null;

            async Task<UsuarioTenantScopeActual?> CurrentScopeAsync() =>
                await memberships.ObtenerActualAsync(tenantId, cancellationToken);

            async Task<bool> CurrentBranchAndGrantAsync(int roleId)
            {
                var role = await database.Roles.IgnoreQueryFilters().AsNoTracking()
                    .AnyAsync(r => r.Id == roleId && r.Activo && !r.Eliminado &&
                        r.Ambito == AmbitoAutorizacion.Empresa &&
                        (r.EmpresaId == tenantId ||
                         (r.EmpresaId == null && r.EsSistema)), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!role)
                    return false;

                var branch = await database.Set<Sucursal>().IgnoreQueryFilters()
                    .AsNoTracking().AnyAsync(s => s.Id == branchId &&
                        s.EmpresaId == tenantId && s.Activa && !s.Eliminado,
                        cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!branch)
                    return false;

                // Only read access to this branch is checked here; this is NOT
                // a gym-specific permission, SaaS billing state or door grant.
                return await (from grant in database.RolPermisos.IgnoreQueryFilters()
                                   .AsNoTracking()
                              join permission in database.Permisos.IgnoreQueryFilters()
                                      .AsNoTracking()
                                  on grant.PermisoId equals permission.Id
                              where grant.RolId == roleId &&
                                    permission.Ambito == AmbitoAutorizacion.Empresa &&
                                    permission.Modulo == ModuloSistema.Sucursales &&
                                    permission.Accion == AccionPermiso.Ver &&
                                    permission.Activo && !permission.Eliminado
                              select grant.Id).AnyAsync(cancellationToken);
            }

            var scope = await CurrentScopeAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (scope is null || scope.UsuarioId != actorId ||
                scope.EmpresaId != tenantId || scope.RolId <= 0 ||
                !await CurrentBranchAndGrantAsync(scope.RolId))
                return null;
            cancellationToken.ThrowIfCancellationRequested();

            // Re-read current scope/grant/branch after async queries. Revocation
            // or tenant-role changes must fail closed before any snapshot exits.
            var fresh = await CurrentScopeAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (fresh is null || fresh.UsuarioId != actorId ||
                fresh.EmpresaId != tenantId || fresh.RolId != scope.RolId ||
                !await CurrentBranchAndGrantAsync(fresh.RolId))
                return null;
            cancellationToken.ThrowIfCancellationRequested();

            // No real gym entitlement/ERP persistence exists in this atomic
            // change. Explicit false flags stop Application and Domain from
            // producing even a conditional gym eligibility candidate.
            return new Service.AuthenticatedContext(
                new Rules.VerifiedActor(actorId, tenantId, branchId,
                    JwtVerified: true, SaaSMembershipActive: false,
                    GymPermissionGrantedByServer: false, PermissionRevoked: false),
                FromTrustedServer: true, SignedJwtValidated: true,
                CurrentMembershipAndRbacFromDatabase: true,
                CurrentTenantResolved: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // No PII, signed token, DB error, or exception detail escapes.
            return null;
        }
    }
}
