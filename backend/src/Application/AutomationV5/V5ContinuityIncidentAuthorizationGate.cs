using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C03-04.M03.03: a fail-closed Application authorization preflight.
/// Implementations must independently obtain this evidence from authenticated
/// server context and persisted incident ownership, not from HTTP DTOs.
/// No runtime adapter is registered here; success NEVER grants data or mutation.
/// </summary>
public interface IV5ContinuityIncidentVerifiedEvidenceReader
{
    ValueTask<V5ContinuityIncidentAuthorizationGate.VerifiedEvidence?> ReadAsync(
        string incidentKey, CancellationToken cancellationToken);
}

public sealed class V5ContinuityIncidentAuthorizationGate
{
    private readonly IV5ContinuityIncidentVerifiedEvidenceReader _reader;

    public V5ContinuityIncidentAuthorizationGate(
        IV5ContinuityIncidentVerifiedEvidenceReader reader) =>
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public sealed record Request(
        string? IncidentKey,
        string? ExpectedCommitSha,
        int? ClientTenantHint,
        bool ClientSuppliedRole,
        bool RequestsEvidenceExport,
        V5ContinuityIncidentDomainRules.Request? Command);

    /// <summary>
    /// Only a future trusted backend adapter can construct genuine evidence:
    /// this record, or a test double, does NOT authenticate JWTs or query MySQL.
    /// </summary>
    public sealed record VerifiedEvidence(
        string? ObservedCommitSha,
        int VerifiedActorId,
        int PersistedOwnerTenantId,
        bool JwtValidatedByBackend,
        bool MembershipCurrent,
        bool PermissionCurrent,
        bool RevocationChecked,
        bool PermissionRevoked,
        bool AuditSinkReady,
        string? SanitizedEvidenceReference,
        V5ContinuityIncidentDomainRules.ServerAuthority? Authority,
        V5ContinuityIncidentDomainRules.PersistedIncident? Incident);

    public readonly record struct Verdict(string Code)
    {
        public bool GrantsEvidenceRead => false;
        public bool GrantsMutation => false;
        public bool GrantsBackupRestore => false;
        public bool CertifiesJwtOrDatabase => false;
        public bool NeedsAuthenticatedRuntimeAdapter =>
            Code == "AUTHENTICATED_INCIDENT_RUNTIME_ADAPTER_REQUIRED";
    }

    public async ValueTask<Verdict> EvaluateAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || !SafeIncidentKey(request.IncidentKey) ||
            !FullSha(request.ExpectedCommitSha) || request.Command is null)
            return Deny("INCIDENT_AUTH_REQUEST_INVALID");

        if (request.ClientSuppliedRole || request.RequestsEvidenceExport ||
            request.Command.Action ==
                V5ContinuityIncidentDomainRules.IncidentAction.RequestSandboxRestore)
            return Deny("INCIDENT_CLIENT_AUTHORITY_OR_EXPORT_DENIED");

        VerifiedEvidence? evidence;
        try
        {
            evidence = await _reader.ReadAsync(request.IncidentKey!, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Deny("VERIFIED_INCIDENT_EVIDENCE_UNAVAILABLE");
        }

        if (evidence is null || evidence.Authority is null ||
            evidence.Incident is null ||
            !string.Equals(evidence.ObservedCommitSha,
                request.ExpectedCommitSha, StringComparison.Ordinal) ||
            !FullSha(evidence.ObservedCommitSha))
            return Deny("INCIDENT_TRUSTED_EVIDENCE_MISSING_OR_STALE");

        if (evidence.VerifiedActorId <= 0 ||
            evidence.PersistedOwnerTenantId <= 0 ||
            evidence.Authority.ActorId != evidence.VerifiedActorId ||
            evidence.Authority.TenantId != evidence.PersistedOwnerTenantId ||
            evidence.Incident.OwnerTenantId != evidence.PersistedOwnerTenantId ||
            !evidence.Incident.OwnershipReadFromPersistence)
            return Deny("INCIDENT_TRUSTED_ACTOR_OR_OWNER_MISMATCH");

        if (!evidence.JwtValidatedByBackend || !evidence.MembershipCurrent ||
            !evidence.PermissionCurrent || !evidence.RevocationChecked ||
            evidence.PermissionRevoked || !evidence.AuditSinkReady ||
            !evidence.Authority.JwtVerified ||
            !evidence.Authority.ActiveMembershipVerified ||
            !evidence.Authority.ExactPermissionVerified ||
            !evidence.Authority.ServerResolvedScope)
            return Deny("INCIDENT_CURRENT_BACKEND_PERMISSION_REQUIRED");

        // Client tenant is only an optional consistency hint, never authority.
        if (request.ClientTenantHint is not null &&
            request.ClientTenantHint != evidence.PersistedOwnerTenantId)
            return Deny("INCIDENT_CLIENT_TENANT_OVERRIDE_DENIED");

        var command = request.Command;
        if (command.ClientTenantOverride is not null &&
            command.ClientTenantOverride != evidence.PersistedOwnerTenantId)
            return Deny("INCIDENT_CLIENT_TENANT_OVERRIDE_DENIED");

        var preflight = new V5ContinuityIncidentApplicationService().Prepare(
            new V5ContinuityIncidentApplicationService.Request(
                request.ExpectedCommitSha, evidence.ObservedCommitSha,
                evidence.SanitizedEvidenceReference, evidence.Authority,
                evidence.Incident, command));

        if (!string.Equals(preflight.Code,
            "AUTHENTICATED_INCIDENT_ADAPTER_REQUIRED", StringComparison.Ordinal))
            return Deny(preflight.Code);

        // The evidence interface and preflight are not a security boundary:
        // live JWT verification, version-CAS, idempotency, audit and ownership
        // must all be independently enforced by a future runtime adapter.
        return new Verdict("AUTHENTICATED_INCIDENT_RUNTIME_ADAPTER_REQUIRED");
    }

    private static Verdict Deny(string code) => new(code);

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(c =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

    private static bool SafeIncidentKey(string? key) =>
        key is { Length: >= 1 and <= 128 } &&
        key.All(c => (c >= 'a' && c <= 'z') ||
                     (c >= 'A' && c <= 'Z') ||
                     (c >= '0' && c <= '9') || c is '-' or '_');
}
