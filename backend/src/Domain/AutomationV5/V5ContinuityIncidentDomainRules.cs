namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C03-04.M03.01: pure fail-closed incident/continuity domain contract.
/// Inputs must come from trusted server authentication and persisted data.
/// A successful preflight never revokes identities, restores backups, performs
/// writes, exposes evidence, or authorizes any deployment.
/// </summary>
public static class V5ContinuityIncidentDomainRules
{
    public enum IncidentState { Open, Contained, Resolved }
    public enum IncidentAction { ReadEvidence, Contain, Resolve, RequestSandboxRestore }

    public sealed record ServerAuthority(
        int? ActorId, int? TenantId, int? BranchId,
        bool JwtVerified, bool ActiveMembershipVerified,
        bool ExactPermissionVerified, bool ServerResolvedScope,
        bool IndependentDecisionAuthorityVerified);

    public sealed record PersistedIncident(
        int? OwnerTenantId, int? OwnerBranchId, long Version,
        IncidentState State, bool OwnershipReadFromPersistence,
        bool IncidentRevoked, bool EvidenceSanitized);

    public sealed record Request(
        IncidentAction Action, int? ClientTenantOverride,
        long? ExpectedVersion, string? IdempotencyKey,
        string? Reason, bool MitigationEvidenceVerified,
        bool RecoveryApprovalFromTrustedSource);

    public readonly record struct Verdict(string Code)
    {
        public bool RequiresTrustedServiceEnforcement =>
            Code == "READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT";
        public bool AuthorizesIncidentMutation => false;
        public bool AuthorizesBackupRestore => false;
    }

    public static Verdict Evaluate(
        ServerAuthority? authority, PersistedIncident? incident, Request? request)
    {
        if (authority is null || incident is null || request is null ||
            authority.ActorId is not > 0 || authority.TenantId is not > 0 ||
            !authority.JwtVerified || !authority.ActiveMembershipVerified ||
            !authority.ExactPermissionVerified || !authority.ServerResolvedScope)
            return new Verdict("VERIFIED_SERVER_AUTHORITY_REQUIRED");

        if (!Enum.IsDefined(request.Action) || !Enum.IsDefined(incident.State))
            return new Verdict("UNKNOWN_INCIDENT_STATE_OR_ACTION");

        if (request.ClientTenantOverride is not null &&
            request.ClientTenantOverride != authority.TenantId)
            return new Verdict("CLIENT_TENANT_OVERRIDE_DENIED");

        if (!incident.OwnershipReadFromPersistence ||
            incident.OwnerTenantId is not > 0 ||
            incident.OwnerTenantId != authority.TenantId || incident.Version < 0)
            return new Verdict("CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED");

        if (incident.OwnerBranchId is not null &&
            (incident.OwnerBranchId is not > 0 ||
             authority.BranchId != incident.OwnerBranchId))
            return new Verdict("INCIDENT_BRANCH_MISMATCH");

        if (incident.IncidentRevoked)
            return new Verdict("INCIDENT_ACCESS_REVOKED");

        if (!incident.EvidenceSanitized)
            return new Verdict("SANITIZED_EVIDENCE_REQUIRED");

        if (request.Action == IncidentAction.RequestSandboxRestore)
            return new Verdict("RESTORE_REQUIRES_SEPARATE_AUTHORIZED_RUNNER");

        if (request.Action == IncidentAction.ReadEvidence)
            return new Verdict("READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT");

        if (request.ExpectedVersion != incident.Version ||
            request.ExpectedVersion is null)
            return new Verdict("INCIDENT_VERSION_CONFLICT");

        if (!SafeKey(request.IdempotencyKey) || !SafeReason(request.Reason))
            return new Verdict("AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED");

        if (!authority.IndependentDecisionAuthorityVerified)
            return new Verdict("INDEPENDENT_DECISION_AUTHORITY_REQUIRED");

        if (request.Action == IncidentAction.Contain &&
            incident.State != IncidentState.Open)
            return new Verdict("INVALID_CONTAINMENT_TRANSITION");

        if (request.Action == IncidentAction.Resolve &&
            (incident.State != IncidentState.Contained ||
             !request.MitigationEvidenceVerified))
            return new Verdict("MITIGATION_AND_CONTAINMENT_REQUIRED");

        // Trusted Application must revalidate authority, persist using an
        // atomic version compare, and verify idempotency receipts transactionally.
        return new Verdict("READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT");
    }

    private static bool SafeKey(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 128 &&
        value.All(c => c >= '!' && c <= '~');

    private static bool SafeReason(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 256 &&
        value.All(c => !char.IsControl(c));
}
