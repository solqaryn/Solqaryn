using Rules = Solqaryn.Domain.AutomationV5.V5GlobalControlPlaneDomainRules;
namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C02-04.M03.03. Fail-closed server-side evidence gate.
/// This preflight never grants JWT/RBAC, tenant, platform, audit, or data access.
/// </summary>
public interface IV5GlobalControlPlaneVerifiedEvidenceReader
{
    ValueTask<V5GlobalControlPlaneAuthorizationGate.Evidence?> ReadVerifiedAsync(CancellationToken ct);
}

public sealed class V5GlobalControlPlaneAuthorizationGate
{
    private readonly IV5GlobalControlPlaneVerifiedEvidenceReader _reader;
    private readonly V5GlobalControlPlaneApplicationPreflight _preflight = new();

    public V5GlobalControlPlaneAuthorizationGate(IV5GlobalControlPlaneVerifiedEvidenceReader reader)
        => _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public sealed record Request(Rules.Operation Operation, string? RequestedTenantId,
        string? ExpectedSourceSha, string? Justification, bool ClientAuthorityHint,
        bool WriteOrExport, bool SensitiveData);

    // Only a trusted backend adapter may construct Evidence, never HTTP input.
    public sealed record Evidence(string? ObservedSourceSha, string? VerifiedActorId,
        string? VerifiedTenantId, bool AuthenticatedByBackend,
        bool CurrentGrantsReadFromBackend, bool AuditReadFromBackend,
        bool PermissionRevoked, DateTimeOffset ServerUtc, Rules.TrustedSnapshot? Snapshot);

    public readonly record struct Result(string Code)
    {
        public bool NeedsAuthenticatedRuntimeAdapter =>
            Code == "AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED";
        public bool GrantsAccess => false;
        public bool GrantsTenantEnumeration => false;
        public bool GrantsCrossTenantRead => false;
        public bool GrantsInstitutionalWrite => false;
        public bool GrantsExceptionalLease => false;
    }

    public async ValueTask<Result> EvaluateAsync(Request? request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (request is null) return Deny("CONTROL_PLANE_REQUEST_REQUIRED");
        if (!Enum.IsDefined(request.Operation)) return Deny("CONTROL_PLANE_OPERATION_INVALID");
        if (request.ExpectedSourceSha is not { Length: 40 } ||
            !request.ExpectedSourceSha.All(Uri.IsHexDigit))
            return Deny("CONTROL_PLANE_EXACT_SOURCE_SHA_REQUIRED");

        // Reject untrusted authority and unsafe operations before any I/O.
        if (request.ClientAuthorityHint) return Deny("CONTROL_PLANE_CLIENT_AUTHORITY_DENIED");
        if (request.WriteOrExport || request.SensitiveData)
            return Deny("CONTROL_PLANE_UNSAFE_OPERATION_DENIED");
        if (!SafeId(request.RequestedTenantId))
            return Deny("CONTROL_PLANE_TARGET_TENANT_INVALID");

        var evidence = await _reader.ReadVerifiedAsync(ct);
        if (evidence is null || !string.Equals(evidence.ObservedSourceSha,
            request.ExpectedSourceSha, StringComparison.Ordinal))
            return Deny("CONTROL_PLANE_EVIDENCE_MISSING_OR_STALE");
        if (evidence.ServerUtc.Offset != TimeSpan.Zero)
            return Deny("CONTROL_PLANE_SERVER_UTC_REQUIRED");
        if (!evidence.AuthenticatedByBackend || !evidence.CurrentGrantsReadFromBackend ||
            !evidence.AuditReadFromBackend || evidence.PermissionRevoked)
            return Deny("CONTROL_PLANE_CURRENT_BACKEND_AUTHORITY_REQUIRED");
        if (!SafeId(evidence.VerifiedActorId) ||
            !string.Equals(evidence.VerifiedActorId, evidence.Snapshot?.ActorId, StringComparison.Ordinal) ||
            !string.Equals(evidence.VerifiedTenantId, evidence.Snapshot?.VerifiedTargetTenantId, StringComparison.Ordinal) ||
            !string.Equals(evidence.VerifiedTenantId, request.RequestedTenantId, StringComparison.Ordinal))
            return Deny("CONTROL_PLANE_ACTOR_TENANT_MISMATCH");

        var intent = new V5GlobalControlPlaneApplicationPreflight.ReadIntent(
            request.Operation, request.RequestedTenantId, request.Justification,
            request.ClientAuthorityHint, request.WriteOrExport, request.SensitiveData);
        var result = _preflight.ReviewOnly(evidence.Snapshot, intent, evidence.ServerUtc);
        if (!result.RequiresLiveSecurityChecks) return Deny(result.Code);

        // An authenticated adapter must independently enforce JWT, MFA, RBAC,
        // tenant ownership, persisted audit, and time-bounded exception grants.
        return new Result("AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED");
    }

    private static bool SafeId(string? id) => !string.IsNullOrWhiteSpace(id) &&
        id.Length <= 128 && !id.Any(char.IsControl);
    private static Result Deny(string code) => new(code);
}
