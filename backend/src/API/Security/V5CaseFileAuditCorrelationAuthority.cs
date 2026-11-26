using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;

namespace Solqaryn.API.Security;

// API12 session authority only. Never attests case ownership, source SHA or CI.
// Consumers must independently resolve persisted resource/version and revalidate
// this authority before and after I/O and within the persistence boundary.
public sealed class V5CaseFileAuditCorrelationAuthority(
    IHttpContextAccessor accessor, IPermisoService permissions)
{
    public sealed record Session(int ActorId, int BaseRoleId, int TenantId,
        int BranchId, int TenantRoleId, long ExpiresUnix);

    public async Task<Session?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var http = accessor.HttpContext;
        if (http is null) return null;

        var auth = await http.AuthenticateAsync(TenantSessionToken.Scheme);
        cancellationToken.ThrowIfCancellationRequested();
        if (!auth.Succeeded || auth.Ticket?.AuthenticationScheme != TenantSessionToken.Scheme ||
            auth.Principal is null) return null;
        var session = Read(auth.Principal);
        if (session is null || !Matches(http, session)) return null;

        // No administrator, generic Auditoria:Ver or platform fallback.
        var grant = $"{ModuloSistema.Auditoria}:{AccionPermiso.ReadAuditCorrelation}";
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var current = await permissions.GetMisPermisosContextoAsync(
                session.ActorId, session.BaseRoleId, session.TenantId,
                session.BranchId, session.TenantRoleId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (current?.Permisos is null ||
                !current.Permisos.Contains(grant, StringComparer.Ordinal) ||
                !Matches(http, session)) return null;
        }
        return session;
    }

    public async Task<bool> RevalidateAsync(Session expected,
        CancellationToken cancellationToken = default) =>
        expected is not null && await ResolveAsync(cancellationToken) == expected;

    private bool Matches(HttpContext http, Session session) =>
        ReferenceEquals(accessor.HttpContext, http) &&
        Read(http.User) == session &&
        session.ExpiresUnix > DateTimeOffset.UtcNow.ToUnixTimeSeconds() &&
        HeaderMatches(http, "X-Empresa-Id", session.TenantId) &&
        HeaderMatches(http, "X-Sucursal-Id", session.BranchId);

    private static Session? Read(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true ||
            !Positive(principal, "id", out var actor) ||
            !Positive(principal, "rolId", out var baseRole) ||
            !Positive(principal, "tenantId", out var tenant) ||
            !Positive(principal, "branchId", out var branch) ||
            !Positive(principal, "tenantRoleId", out var role) ||
            !PositiveLong(principal, "exp", out var expiry)) return null;
        return new Session(actor, baseRole, tenant, branch, role, expiry);
    }

    private static bool Positive(ClaimsPrincipal principal, string name, out int value)
    {
        value = 0;
        var claims = principal.FindAll(name).ToArray();
        return claims.Length == 1 &&
            int.TryParse(claims[0].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out value) && value > 0 &&
            claims[0].Value == value.ToString(CultureInfo.InvariantCulture);
    }

    private static bool PositiveLong(ClaimsPrincipal principal, string name, out long value)
    {
        value = 0;
        var claims = principal.FindAll(name).ToArray();
        return claims.Length == 1 &&
            long.TryParse(claims[0].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out value) && value > 0 &&
            claims[0].Value == value.ToString(CultureInfo.InvariantCulture);
    }

    private static bool HeaderMatches(HttpContext http, string name, int id) =>
        !http.Request.Headers.TryGetValue(name, out var values) ||
        (values.Count == 1 && string.Equals(values[0],
            id.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
}
