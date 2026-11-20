using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Rules = Solqaryn.Domain.AutomationV5.V5TenantLifecycleDomainRules;
using Gate = Solqaryn.Application.AutomationV5.C0203TenantLifecycleAuthorizationGate;

namespace Solqaryn.API.Security;

/// <summary>
/// C02-03.M03.03: reads actual, current platform RBAC, a signed JWT and a
/// tenant-fenced Empresa row. No tenant mutation or privilege assignment.
/// DEV-only, disabled by default, and limited to a read-only Suspend preflight.
/// The caller must separately verify lifecycle revision, audit and CAS before
/// any future operation; this reader never grants write permission.
/// </summary>
public sealed class C0203VerifiedPlatformOperatorEvidenceReader(
    IHttpContextAccessor contexts,
    ICurrentUserService current,
    IPlataformaScopeService platform,
    AppDbContext database,
    IConfiguration configuration) : IC0203VerifiedOperatorEvidenceReader
{
    public async ValueTask<Gate.TrustedOperatorSnapshot?> ReadAsync(
        string requestedTenantId,
        Rules.Command requestedCommand,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // All opt-ins are backend-only and OFF if absent. Do not enable other
        // lifecycle commands by implication, especially reactivation/deletion.
        if (!configuration.GetValue<bool>("TenantLifecycle:EnableReadOnlyAuthorizationProbe") ||
            !configuration.GetValue<bool>("TenantLifecycle:AllowSuspendReadOnly") ||
            configuration.GetValue<int>("TenantLifecycle:ReadOnlyPolicyRevision") <= 0 ||
            requestedCommand != Rules.Command.Suspend ||
            !int.TryParse(requestedTenantId, NumberStyles.None,
                CultureInfo.InvariantCulture, out var tenantId) || tenantId <= 0 ||
            !string.Equals(requestedTenantId,
                tenantId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            return null;

        var http = contexts.HttpContext;
        if (http?.User.Identity?.IsAuthenticated != true ||
            !current.EstaAutenticado ||
            current.UsuarioId is not > 0 ||
            string.IsNullOrWhiteSpace(current.NombreUsuario) ||
            !int.TryParse(Convert.ToString(http.Request.RouteValues["tenantId"],
                    CultureInfo.InvariantCulture), NumberStyles.None,
                CultureInfo.InvariantCulture, out var routeTenant) ||
            routeTenant != tenantId ||
            !string.Equals(Convert.ToString(http.Request.RouteValues["tenantId"],
                CultureInfo.InvariantCulture), requestedTenantId, StringComparison.Ordinal))
            return null;

        try
        {
            // Authenticate the actual registered JWT bearer handler. A synthetic
            // ClaimsPrincipal, custom X-Empresa-Id, or unsigned JWT is insufficient.
            var bearer = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            cancellationToken.ThrowIfCancellationRequested();
            if (!bearer.Succeeded || bearer.Principal?.Identity?.IsAuthenticated != true)
                return null;

            var ids = bearer.Principal.FindAll("id").ToArray();
            var names = bearer.Principal.FindAll("nombreUsuario").ToArray();
            var subjects = bearer.Principal.FindAll("sub")
                .Concat(bearer.Principal.FindAll(ClaimTypes.NameIdentifier)).ToArray();
            if (ids.Length != 1 || names.Length != 1 || subjects.Length != 1 ||
                !int.TryParse(ids[0].Value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var signedActor) ||
                signedActor != current.UsuarioId ||
                !string.Equals(names[0].Value, current.NombreUsuario, StringComparison.Ordinal) ||
                !string.Equals(subjects[0].Value, names[0].Value, StringComparison.Ordinal))
                return null;

            // This resolves a live UsuarioRolPlataforma + active Usuario/Rol,
            // independently of Usuario.RolId and enterprise membership.
            var authority = await platform.ObtenerActualAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (authority is null || authority.UsuarioId != signedActor ||
                authority.RolId <= 0 ||
                !string.Equals(authority.NombreUsuario, names[0].Value,
                    StringComparison.Ordinal))
                return null;

            // An explicit global scope alone is not a grant. The active,
            // platform-scoped RolPermiso / Permiso row must exist as well.
            var hasGrant = await (
                from relation in database.RolPermisos.IgnoreQueryFilters().AsNoTracking()
                join permission in database.Permisos.IgnoreQueryFilters().AsNoTracking()
                    on relation.PermisoId equals permission.Id
                where relation.RolId == authority.RolId &&
                      permission.Ambito == AmbitoAutorizacion.Plataforma &&
                      permission.Modulo == ModuloSistema.Configuracion &&
                      permission.Accion == AccionPermiso.CambiarEstado &&
                      permission.Activo && !permission.Eliminado
                select relation.Id
            ).AnyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!hasGrant)
                return null;

            // Do not trust route/header claims as proof of target ownership.
            // The platform-admin scope is the only global authority and the
            // target must be an actual active Empresa row in the current DB.
            var targetExists = await database.Set<Solqaryn.Domain.Entities.Empresa>().IgnoreQueryFilters()
                .AsNoTracking().AnyAsync(e => e.Id == tenantId && e.Activa,
                    cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!targetExists)
                return null;

            // A positive result is only an informational READONLY preflight;
            // the operation still requires versioned policy, transaction,
            // audit and revocation gates, none of which are implemented here.
            return new Gate.TrustedOperatorSnapshot
            {
                VerifiedActorId = signedActor.ToString(CultureInfo.InvariantCulture),
                VerifiedTenantId = requestedTenantId,
                VerifiedCommand = requestedCommand,
                ServerControlledEvidence = true,
                BackendJwtSignatureVerified = true,
                CurrentPlatformGrantVerified = true,
                CurrentOperatorRoleVerified = true,
                MembershipAndSessionFresh = true,
                TargetOwnershipVerifiedServerSide = true,
                PolicyRevisionFresh = true,
                LifecycleFeatureEnabled = true,
                CommandEnabledByTrustedConfig = true,
                TenantFencedEvidenceRead = true
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // No provider details, grant data, JWT or tenant identity leave
            // this boundary. A database/authentication outage denies access.
            return null;
        }
    }
}
