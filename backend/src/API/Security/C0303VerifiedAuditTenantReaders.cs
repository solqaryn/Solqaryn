using System.Globalization;
using System.Security.Claims;
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
using Rules = Solqaryn.Domain.Security.C0303SecurityAuditDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0303SecurityAuditApplicationService;

namespace Solqaryn.API.Security;

/// <summary>
/// C03-03.M03.03: DEV-only, OFF-by-default, authenticated and relationally
/// revalidated tenant audit-read preflight. Only Empresa itself is the
/// tenant-owned resource proven here; arbitrary business resource ownership,
/// audit persistence and exceptional access are deliberately NOT certified.
/// No endpoint or authorization grant is installed by this adapter.
/// </summary>
public sealed class C0303VerifiedAuditTenantReaders(
    IHttpContextAccessor contexts,
    ICurrentUserService current,
    IUsuarioScopeService scopes,
    AppDbContext database,
    IConfiguration configuration)
    : IC0303VerifiedAuditEventReader, IC0303VerifiedAuditResourceReader
{
    public async ValueTask<Service.ServerEvent?> ReadCurrentAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!configuration.GetValue<bool>("SecurityAudit:EnableReadOnlyAuthorizationProbe"))
            return null;

        var http = contexts.HttpContext;
        var tenantId = VerifiedCommercialTenant.Resolve(http);
        if (http?.User.Identity?.IsAuthenticated != true ||
            !current.EstaAutenticado || current.UsuarioId is not > 0 ||
            string.IsNullOrWhiteSpace(current.NombreUsuario) || tenantId is not > 0)
            return null;

        // An optional route parameter may select a company but must never
        // contradict the independently established server-side tenant marker.
        if (http.Request.RouteValues.TryGetValue("empresaId", out var route) &&
            !string.Equals(Convert.ToString(route, CultureInfo.InvariantCulture),
                tenantId.Value.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
            return null;

        try
        {
            var bearer = await http.AuthenticateAsync(
                JwtBearerDefaults.AuthenticationScheme);
            cancellationToken.ThrowIfCancellationRequested();
            if (!bearer.Succeeded ||
                bearer.Principal?.Identity?.IsAuthenticated != true)
                return null;

            var ids = bearer.Principal.FindAll("id").ToArray();
            var names = bearer.Principal.FindAll("nombreUsuario").ToArray();
            var subjects = bearer.Principal.FindAll("sub")
                .Concat(bearer.Principal.FindAll(ClaimTypes.NameIdentifier))
                .ToArray();
            if (ids.Length != 1 || names.Length != 1 || subjects.Length != 1 ||
                !int.TryParse(ids[0].Value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var actorId) ||
                actorId <= 0 ||
                !string.Equals(ids[0].Value,
                    actorId.ToString(CultureInfo.InvariantCulture),
                    StringComparison.Ordinal) ||
                actorId != current.UsuarioId ||
                !string.Equals(names[0].Value, current.NombreUsuario,
                    StringComparison.Ordinal) ||
                !string.Equals(subjects[0].Value, names[0].Value,
                    StringComparison.Ordinal))
                return null;

            var membership = await scopes.ObtenerActualAsync(
                tenantId.Value, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (membership is null || membership.UsuarioId != actorId ||
                membership.EmpresaId != tenantId.Value || membership.RolId <= 0)
                return null;

            async Task<bool> CurrentAdminAuditGrantAsync(int roleId)
            {
                // Single tenant-fenced SQL predicate, including current user,
                // membership, enterprise, admin role and audit-read grant.
                return await (
                    from link in database.UsuarioEmpresas.IgnoreQueryFilters()
                        .AsNoTracking()
                    join user in database.Usuarios.IgnoreQueryFilters().AsNoTracking()
                        on link.UsuarioId equals user.Id
                    join company in database.Set<Empresa>().IgnoreQueryFilters()
                        .AsNoTracking() on link.EmpresaId equals company.Id
                    join role in database.Roles.IgnoreQueryFilters().AsNoTracking()
                        on link.RolId equals role.Id
                    join grant in database.RolPermisos.IgnoreQueryFilters()
                        .AsNoTracking() on role.Id equals grant.RolId
                    join permission in database.Permisos.IgnoreQueryFilters()
                        .AsNoTracking() on grant.PermisoId equals permission.Id
                    where link.UsuarioId == actorId &&
                          link.EmpresaId == tenantId.Value &&
                          link.RolId == roleId && link.Activa &&
                          user.Activo && !user.Eliminado && !user.Bloqueado &&
                          user.NombreUsuario == names[0].Value &&
                          company.Activa &&
                          role.Activo && !role.Eliminado &&
                          role.EsAdministrador &&
                          role.Ambito == AmbitoAutorizacion.Empresa &&
                          (role.EmpresaId == tenantId.Value ||
                           (role.EmpresaId == null && role.EsSistema)) &&
                          permission.Activo && !permission.Eliminado &&
                          permission.Ambito == AmbitoAutorizacion.Empresa &&
                          permission.Modulo == ModuloSistema.Auditoria &&
                          permission.Accion == AccionPermiso.Ver
                    select grant.Id).AnyAsync(cancellationToken);
            }

            if (!await CurrentAdminAuditGrantAsync(membership.RolId))
                return null;
            cancellationToken.ThrowIfCancellationRequested();

            // A grant or membership can disappear during I/O. Re-read before
            // providing any positive *non-authorizing* preparation evidence.
            var refreshed = await scopes.ObtenerActualAsync(
                tenantId.Value, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (refreshed is null ||
                refreshed.UsuarioId != actorId ||
                refreshed.EmpresaId != tenantId.Value ||
                refreshed.RolId != membership.RolId ||
                !await CurrentAdminAuditGrantAsync(refreshed.RolId))
                return null;
            cancellationToken.ThrowIfCancellationRequested();

            return new Service.ServerEvent(
                actorId, tenantId.Value, tenantId.Value,
                TrustedServerContext: true, SignedJwtVerified: true,
                MembershipActive: true, CurrentAuditRbacVerified: true,
                PermissionRevoked: false, TrustedAuditWriter: true,
                PlatformAuthorityVerified: false,
                ExplicitPlatformAuditGrant: false,
                TransactionalSinkVerified: false,
                Rules.Scope.Enterprise, Rules.Action.AuditRead,
                Rules.Outcome.Allowed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Never expose SQL, account identifiers, tokens or secrets.
            return null;
        }
    }

    public async ValueTask<Service.ServerResource?> ReadCurrentAsync(
        int actorId, int tenantId, Rules.Action action,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // This narrow source proves only ownership of the Empresa root
        // for read-only AuditRead. It MUST NOT attest invoice/order/etc.
        if (actorId <= 0 || tenantId <= 0 ||
            action != Rules.Action.AuditRead)
            return null;

        var verified = await ReadCurrentAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (verified is null || verified.ActorId != actorId ||
            verified.TenantId != tenantId)
            return null;
        try
        {
            var exists = await database.Set<Empresa>().IgnoreQueryFilters()
                .AsNoTracking().AnyAsync(c => c.Id == tenantId && c.Activa,
                    cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return exists
                ? new Service.ServerResource(tenantId,
                    TrustedStore: true, CurrentOwnershipVerified: true)
                : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return null;
        }
    }
}
