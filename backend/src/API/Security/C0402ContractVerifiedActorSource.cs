using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Rules = Solqaryn.Domain.AutomationV5.C0402ContractDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0402ContractApplicationService;

namespace Solqaryn.API.Security;

/// <summary>
/// C04-02.M03.03: DEV-only, opt-in, read-only verified actor source.
/// A successful actor snapshot is NOT authorization to mutate contracts or money.
/// No endpoint, contract storage adapter, payment or production opt-in is created.
/// </summary>
public sealed class C0402ContractVerifiedActorSource(
    IHttpContextAccessor contexts,
    ICurrentUserService current,
    IUsuarioScopeService memberships,
    AppDbContext database,
    IConfiguration configuration) : IC0402VerifiedContractActorSource
{
    public async ValueTask<Service.VerifiedServerActor?> ReadCurrentAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!configuration.GetValue<bool>("ContractAuthorization:EnableReadOnlyActorProbe"))
            return null;

        var http = contexts.HttpContext;
        var rawTenant = Convert.ToString(
            http?.Request.RouteValues["empresaId"], CultureInfo.InvariantCulture);
        if (http?.User.Identity?.IsAuthenticated != true ||
            !current.EstaAutenticado || current.UsuarioId is not > 0 ||
            !int.TryParse(rawTenant, NumberStyles.None,
                CultureInfo.InvariantCulture, out var tenantId) || tenantId <= 0 ||
            !string.Equals(rawTenant, tenantId.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
            return null;

        // VerifiedCommercialTenant comes from backend authorization, not a client header.
        var authorizedTenant = VerifiedCommercialTenant.Resolve(http);
        if (authorizedTenant.HasValue && authorizedTenant.Value != tenantId)
            return null;

        try
        {
            var jwt = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            cancellationToken.ThrowIfCancellationRequested();
            if (!jwt.Succeeded || jwt.Principal?.Identity?.IsAuthenticated != true)
                return null;

            var ids = jwt.Principal.FindAll("id").ToArray();
            if (ids.Length != 1 ||
                !int.TryParse(ids[0].Value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var actorId) ||
                actorId <= 0 || actorId != current.UsuarioId.Value)
                return null;

            var scope = await memberships.ObtenerActualAsync(tenantId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (scope is null || scope.UsuarioId != actorId ||
                scope.EmpresaId != tenantId || scope.RolId <= 0)
                return null;

            async Task<bool> HasPreflightGrantsAsync(int roleId)
            {
                // Two current enterprise grants are mandatory for this billing
                // read-only preflight. They are NOT contract write authorization.
                var grants = from link in database.RolPermisos.IgnoreQueryFilters().AsNoTracking()
                             join permission in database.Permisos.IgnoreQueryFilters().AsNoTracking()
                                 on link.PermisoId equals permission.Id
                             where link.RolId == roleId &&
                                   permission.Ambito == AmbitoAutorizacion.Empresa &&
                                   permission.Activo && !permission.Eliminado
                             select new { permission.Modulo, permission.Accion };
                var billing = await grants.AnyAsync(x =>
                    x.Modulo == ModuloSistema.Facturacion &&
                    x.Accion == AccionPermiso.Ver, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var config = await grants.AnyAsync(x =>
                    x.Modulo == ModuloSistema.Configuracion &&
                    x.Accion == AccionPermiso.Ver, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return billing && config;
            }

            async Task<bool> RoleValidAsync(int roleId) =>
                await database.Roles.IgnoreQueryFilters().AsNoTracking()
                    .AnyAsync(r => r.Id == roleId &&
                        r.Ambito == AmbitoAutorizacion.Empresa &&
                        (r.EmpresaId == tenantId || (r.EmpresaId == null && r.EsSistema)) &&
                        r.Activo && !r.Eliminado, cancellationToken);

            if (!await RoleValidAsync(scope.RolId) ||
                !await HasPreflightGrantsAsync(scope.RolId))
                return null;
            cancellationToken.ThrowIfCancellationRequested();

            // A membership or grant revoked while database IO was in progress
            // must not yield a stale positive actor snapshot.
            var refreshed = await memberships.ObtenerActualAsync(
                tenantId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (refreshed is null || refreshed.EmpresaId != tenantId ||
                refreshed.UsuarioId != actorId ||
                refreshed.RolId != scope.RolId ||
                !await RoleValidAsync(refreshed.RolId) ||
                !await HasPreflightGrantsAsync(refreshed.RolId))
                return null;
            cancellationToken.ThrowIfCancellationRequested();

            return new Service.VerifiedServerActor(
                new Rules.VerifiedActor(tenantId, actorId,
                    JwtVerified: true, MembershipActive: true,
                    ExplicitContractRbac: true, PermissionRevoked: false),
                TrustedServerSource: true, SignedJwtEstablished: true,
                RelationalRbacFresh: true, MembershipFresh: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Never reflect authentication, SQL, role or tenant details.
            return null;
        }
    }
}
