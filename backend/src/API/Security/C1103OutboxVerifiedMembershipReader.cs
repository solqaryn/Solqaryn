using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Security;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;

namespace Solqaryn.API.Security;

/// <summary>
/// C11-03.M03.03: actual DEV-only read-only bridge to authenticated JWT,
/// current UsuarioEmpresa scope and persistent RolPermiso grants.
/// Feature OFF by default. Never authorizes outbox INSERT, dispatch or ACK.
/// </summary>
public sealed class C1103OutboxVerifiedMembershipReader(
    IHttpContextAccessor contexts,
    ICurrentUserService current,
    IUsuarioScopeService scopes,
    IPermisoService permissions,
    IConfiguration configuration) : IC1103OutboxCurrentMembershipReader
{
    public async ValueTask<C1103OutboxAuthorizationGate.CurrentAuthority?> ReadCurrentAsync(
        int authenticatedUserId, int requestedTenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!configuration.GetValue<bool>("OutboxPlanning:EnableReadOnlyAuthorizationProbe"))
            return null;

        var http = contexts.HttpContext;
        if (http?.User.Identity?.IsAuthenticated != true ||
            !current.EstaAutenticado || current.UsuarioId != authenticatedUserId ||
            authenticatedUserId <= 0 || requestedTenantId <= 0 ||
            !int.TryParse(Convert.ToString(http.Request.RouteValues["tenantId"],
                    CultureInfo.InvariantCulture), NumberStyles.None,
                CultureInfo.InvariantCulture, out var routeTenant) ||
            routeTenant != requestedTenantId)
            return null;

        var verifiedTenant = VerifiedCommercialTenant.Resolve(http);
        if (verifiedTenant.HasValue && verifiedTenant.Value != requestedTenantId)
            return null;

        try
        {
            // Never trust an arbitrary ClaimsPrincipal or X-Empresa-Id header.
            // The actual registered Bearer handler validates signature, issuer,
            // audience, expiry and signing key before any database access.
            var bearer = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            cancellationToken.ThrowIfCancellationRequested();
            if (!bearer.Succeeded || bearer.Principal?.Identity?.IsAuthenticated != true)
                return null;

            var signedIds = bearer.Principal.FindAll("id").ToArray();
            if (signedIds.Length != 1 ||
                !int.TryParse(signedIds[0].Value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var signedActor) ||
                signedActor != authenticatedUserId)
                return null;
            // JwtService emits the numeric identity as "id", but "sub"
            // contains the signed user name. ASP.NET may map sub to
            // NameIdentifier; comparing that name to the numeric id would
            // reject every legitimate token. Never infer a user from sub:
            // require all three signed identity claims to agree with the
            // authenticated request while the numeric id remains authoritative.
            var signedNames = bearer.Principal.FindAll("nombreUsuario").ToArray();
            var signedSubjects = bearer.Principal.FindAll("sub")
                .Concat(bearer.Principal.FindAll(ClaimTypes.NameIdentifier))
                .ToArray();
            if (signedNames.Length != 1 || signedSubjects.Length != 1 ||
                string.IsNullOrWhiteSpace(signedNames[0].Value) ||
                !string.Equals(signedNames[0].Value, current.NombreUsuario,
                    StringComparison.Ordinal) ||
                !string.Equals(signedSubjects[0].Value, signedNames[0].Value,
                    StringComparison.Ordinal))
                return null;

            // These services use the live relational UsuarioEmpresa/RolPermiso
            // mapping; neither an administrator claim nor UI permission mocks
            // can manufacture membership in an unrelated tenant.
            var scope = await scopes.ObtenerActualAsync(requestedTenantId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (scope is null ||
                scope.UsuarioId != signedActor ||
                scope.EmpresaId != requestedTenantId ||
                scope.RolId <= 0)
                return null;

            if (!await permissions.TienePermisoAsync(
                    requestedTenantId, ModuloSistema.Configuracion, AccionPermiso.Crear))
                return null;
            cancellationToken.ThrowIfCancellationRequested();

            return new C1103OutboxAuthorizationGate.CurrentAuthority(
                signedActor,
                requestedTenantId,
                ServerTrusted: true,
                MembershipActive: true,
                RoleGrantActive: true,
                PermissionRevoked: false,
                TenantOwnedByUser: true,
                VerifiedNowUtc: DateTime.UtcNow,
                AllowedEventTypes: new HashSet<string>(StringComparer.Ordinal)
                {
                    "ventas.factura.emitida"
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // No token, PII, authorization detail or database error leaves the API.
            return null;
        }
    }
}
