using Solqaryn.Domain.Security;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C06-04.M03.03. Customer portal authorization evidence gate.
/// This preflight never authenticates a JWT, issues a session, reads a customer
/// resource or authorizes a private response. A real API/backend adapter must
/// independently verify identity, persisted ownership and token proof.
/// </summary>
public interface IC0604PortalAuthorizationEvidenceReader
{
    ValueTask<C0604PortalClienteAuthorizationGate.Evidence?> ReadCurrentAsync(
        string resourceId, CancellationToken cancellationToken);
}

public sealed class C0604PortalClienteAuthorizationGate
{
    private readonly IC0604PortalAuthorizationEvidenceReader _reader;

    public C0604PortalClienteAuthorizationGate(
        IC0604PortalAuthorizationEvidenceReader reader) =>
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public sealed record Request(
        string? ResourceId, string? ExpectedSourceSha, long ExpectedVersion,
        int? ClientTenantHint, bool ClientSuppliedRole,
        bool AttemptsWriteOrExport, bool IncludesSensitiveTelemetry);

    // Values are trusted only if obtained from an independently authenticated
    // backend source. This interface alone does not prove live authentication.
    public sealed record Evidence(
        string? ObservedSourceSha, string? VerifiedActorId,
        int VerifiedEmpresaId, int VerifiedCustomerId, int VerifiedAccountId,
        bool JwtVerifiedOnServer, bool TokenHashProofVerified,
        bool MembershipCurrent, bool PrivateReadPermissionCurrent,
        bool PermissionRevoked, bool AuditEvidenceAvailable,
        C0604PortalClienteApplicationService.VerifiedSnapshot? Snapshot);

    public readonly record struct Result(string Code)
    {
        public bool GrantsPrivateRead => false;
        public bool GrantsCrossTenantRead => false;
        public bool GrantsWrite => false;
        public bool IssuesJwt => false;
        public bool AuthorizesOrder => false;
        public bool CertifiesLiveAuthentication => false;
    }

    public async ValueTask<Result> EvaluateAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || !SafeResourceId(request.ResourceId) ||
            request.ExpectedVersion <= 0)
            return Deny("PORTAL_AUTH_REQUEST_INVALID");
        if (!ValidSha(request.ExpectedSourceSha))
            return Deny("PORTAL_AUTH_SOURCE_SHA_INVALID");
        if (request.ClientSuppliedRole || request.AttemptsWriteOrExport ||
            request.IncludesSensitiveTelemetry)
            return Deny("PORTAL_AUTH_CLIENT_AUTHORITY_DENIED");

        var evidence = await _reader.ReadCurrentAsync(
            request.ResourceId!, cancellationToken);
        if (evidence is null || evidence.Snapshot is null ||
            !string.Equals(evidence.ObservedSourceSha,
                request.ExpectedSourceSha, StringComparison.Ordinal))
            return Deny("PORTAL_AUTH_TRUSTED_EVIDENCE_MISSING_OR_STALE");

        if (!SafeActorId(evidence.VerifiedActorId) ||
            evidence.VerifiedEmpresaId <= 0 ||
            evidence.VerifiedCustomerId <= 0 ||
            evidence.VerifiedAccountId <= 0)
            return Deny("PORTAL_AUTH_ACTOR_OR_TENANT_UNVERIFIED");

        if (!evidence.JwtVerifiedOnServer || !evidence.TokenHashProofVerified ||
            !evidence.MembershipCurrent || !evidence.PrivateReadPermissionCurrent ||
            evidence.PermissionRevoked || !evidence.AuditEvidenceAvailable)
            return Deny("PORTAL_AUTH_CURRENT_BACKEND_PERMISSION_REQUIRED");

        if (request.ClientTenantHint is not null &&
            request.ClientTenantHint != evidence.VerifiedEmpresaId)
            return Deny("PORTAL_AUTH_CLIENT_TENANT_OVERRIDE_DENIED");

        var snapshot = evidence.Snapshot;
        if (snapshot.ServerVerifiedEmpresaId != evidence.VerifiedEmpresaId ||
            snapshot.ServerVerifiedCustomerId != evidence.VerifiedCustomerId ||
            snapshot.ServerVerifiedAccountId != evidence.VerifiedAccountId ||
            !snapshot.JwtVerifiedOnServer || !snapshot.SessionTokenProofVerified ||
            !snapshot.MembershipActive || snapshot.PermissionRevoked ||
            !snapshot.HasExplicitPrivateReadPermission ||
            snapshot.ServerNowUtc.Kind != DateTimeKind.Utc)
            return Deny("PORTAL_AUTH_BACKEND_SNAPSHOT_INCONSISTENT");

        var source = new CapturedSource(request.ResourceId!, snapshot);
        var inner = await new C0604PortalClienteApplicationService(source).PreflightAsync(
            new C0604PortalClienteApplicationService.Request(
                request.ResourceId, request.ClientTenantHint, request.ExpectedVersion),
            cancellationToken);
        if (!string.Equals(inner.Code, "AUTHENTICATED_CUSTOMER_ADAPTER_REQUIRED",
            StringComparison.Ordinal))
            return Deny(inner.Code);

        // No actual signed JWT, authenticated HTTP route or persisted data
        // is certified by this preflight. Do not turn this into an access grant.
        return new Result("AUTHENTICATED_CUSTOMER_RUNTIME_ADAPTER_REQUIRED");
    }

    private sealed class CapturedSource : IC0604PortalVerifiedSnapshotSource
    {
        private readonly string _resourceId;
        private readonly C0604PortalClienteApplicationService.VerifiedSnapshot _snapshot;
        public CapturedSource(
            string resourceId, C0604PortalClienteApplicationService.VerifiedSnapshot snapshot)
        {
            _resourceId = resourceId;
            _snapshot = snapshot;
        }
        public ValueTask<C0604PortalClienteApplicationService.VerifiedSnapshot?> LoadVerifiedAsync(
            string resourceId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            C0604PortalClienteApplicationService.VerifiedSnapshot? evidence =
                string.Equals(resourceId, _resourceId, StringComparison.Ordinal) ? _snapshot : null;
            return ValueTask.FromResult(evidence);
        }
    }

    private static Result Deny(string code) => new(code);
    private static bool SafeResourceId(string? id) =>
        id is { Length: > 0 and <= 128 } &&
        id.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') || c is '-' or '_');
    private static bool SafeActorId(string? id) =>
        id is { Length: > 0 and <= 128 } && !id.Any(char.IsControl);
    private static bool ValidSha(string? sha) =>
        sha is { Length: 40 } &&
        sha.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
}
