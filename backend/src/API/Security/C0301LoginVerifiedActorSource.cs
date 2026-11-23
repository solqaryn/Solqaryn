using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API.Security;

/// <summary>Signed JWT and SQL current membership/tenant/branch proof. NEVER a credential or grant.</summary>
public sealed record C0301VerifiedLoginSelection(int ActorId, int TenantId, int BranchId, int RoleId,
    bool SignedJwtValidated, bool CurrentMembershipAndRbacFromDatabase)
{
    public bool IssuesJwt => false;
    public bool GrantsTenantAccess => false;
    public bool GrantsPlatformAccess => false;
}
public interface IC0301VerifiedLoginActorSource
{
    ValueTask<C0301VerifiedLoginSelection?> ReadCurrentAsync(CancellationToken cancellationToken);
}
/// <summary>C03-01.M03.03: DEV read-only JWT and current MySQL RBAC proof, no platform fallback.</summary>
public sealed class C0301LoginVerifiedActorSource(
    IHttpContextAccessor contexts,
    ICurrentUserService current,
    IUsuarioScopeService memberships,
    AppDbContext database,
    IConfiguration configuration) : IC0301VerifiedLoginActorSource
{
    public async ValueTask<C0301VerifiedLoginSelection?> ReadCurrentAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!configuration.GetValue<bool>("LoginSecurity:EnableReadOnlyProbe"))
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
                        r.Ambito == AmbitoAutorizacion.Empresa && r.EmpresaId == tenantId && !r.EsSistema, cancellationToken);
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

            // Read-only proof; not JWT issuance, tenant selection, or platform access.
            return new C0301VerifiedLoginSelection(actorId, tenantId, branchId,
                fresh.RolId, SignedJwtValidated: true,
                CurrentMembershipAndRbacFromDatabase: true);
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
