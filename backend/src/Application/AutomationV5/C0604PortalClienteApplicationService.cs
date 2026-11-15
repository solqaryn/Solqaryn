using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Verified backend adapter boundary for C06-04 private CUSTOMER portal preflight.
/// The interface does not implement authentication: a caller must validate the JWT,
/// session token hash, membership, permission, resource ownership and persisted version.
/// </summary>
public interface IC0604PortalVerifiedSnapshotSource
{
    ValueTask<C0604PortalClienteApplicationService.VerifiedSnapshot?> LoadVerifiedAsync(
        string resourceId, CancellationToken cancellationToken);
}

/// <summary>
/// C06-04.M03.02. Read-only Application-to-Domain preflight.
/// Never issues a JWT, exposes customer data, updates a profile, processes an order,
/// queues work or claims that trusted authentication actually occurred.
/// </summary>
public sealed class C0604PortalClienteApplicationService
{
    private readonly IC0604PortalVerifiedSnapshotSource _source;

    public C0604PortalClienteApplicationService(IC0604PortalVerifiedSnapshotSource source) =>
        _source = source ?? throw new ArgumentNullException(nameof(source));

    public sealed record Request(string? ResourceId, int? ClientTenantHint, long ExpectedVersion);

    public sealed record VerifiedSnapshot(
        int ServerVerifiedEmpresaId,
        int ServerVerifiedCustomerId,
        int ServerVerifiedAccountId,
        Cliente? PersistedCustomer,
        TiendaCuentaCliente? PersistedAccount,
        TiendaSesionCliente? PersistedSession,
        int PersistedResourceEmpresaId,
        int PersistedResourceCustomerId,
        long PersistedVersion,
        DateTime ServerNowUtc,
        bool JwtVerifiedOnServer,
        bool SessionTokenProofVerified,
        bool MembershipActive,
        bool PermissionRevoked,
        bool HasExplicitPrivateReadPermission);

    public readonly record struct Result(string Code)
    {
        public bool AllowsPrivateRead => false;
        public bool AllowsWrite => false;
        public bool IssuesJwt => false;
        public bool AuthorizesOrder => false;
        public bool CertifiesTenantIsolation => false;
    }

    public async Task<Result> PreflightAsync(Request? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || !SafeResourceId(request.ResourceId) ||
            request.ExpectedVersion <= 0)
            return Deny("REQUEST_INVALID");

        cancellationToken.ThrowIfCancellationRequested();
        var verified = await _source.LoadVerifiedAsync(request.ResourceId!, cancellationToken);
        if (verified is null ||
            verified.PersistedCustomer is null ||
            verified.PersistedAccount is null ||
            verified.PersistedSession is null ||
            verified.ServerVerifiedEmpresaId <= 0 ||
            verified.ServerVerifiedCustomerId <= 0 ||
            verified.ServerVerifiedAccountId <= 0 ||
            verified.ServerNowUtc.Kind != DateTimeKind.Utc)
            return Deny("TRUSTED_BACKEND_SNAPSHOT_REQUIRED");

        if (!verified.JwtVerifiedOnServer ||
            !verified.SessionTokenProofVerified ||
            !verified.MembershipActive ||
            verified.PermissionRevoked ||
            !verified.HasExplicitPrivateReadPermission)
            return Deny("AUTHENTICATION_OR_CUSTOMER_PERMISSION_REQUIRED");

        if (request.ClientTenantHint.HasValue &&
            request.ClientTenantHint.Value != verified.ServerVerifiedEmpresaId)
            return Deny("CLIENT_TENANT_OVERRIDE_DENIED");

        try
        {
            C0604PortalClienteDomainRules.ExigirSesionYPropiedad(
                verified.ServerVerifiedEmpresaId, verified.ServerVerifiedAccountId,
                verified.PersistedCustomer, verified.PersistedAccount,
                verified.PersistedSession, verified.ServerNowUtc);
            C0604PortalClienteDomainRules.ExigirRecursoPrivado(
                verified.ServerVerifiedEmpresaId, verified.ServerVerifiedCustomerId,
                verified.PersistedResourceEmpresaId, verified.PersistedResourceCustomerId);
            C0604PortalClienteDomainRules.ExigirVersionEsperada(
                request.ExpectedVersion, verified.PersistedVersion);
        }
        catch (InvalidOperationException)
        {
            return Deny("CUSTOMER_TENANT_SESSION_OR_VERSION_DENIED");
        }

        // The actual API adapter must authenticate the source and repeat permission checks.
        // No customer DTO or data is returned by this method.
        return new Result("AUTHENTICATED_CUSTOMER_ADAPTER_REQUIRED");
    }

    private static Result Deny(string code) => new(code);

    private static bool SafeResourceId(string? value) =>
        value is { Length: >= 1 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');
}
