using Solqaryn.Domain.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.V5ContinuityIncidentDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C03-04.M03.04: isolated, read-only projection of incident metadata.
/// This is NOT a database repository, authenticated identity, SQL adapter,
/// backup runner or proof of persisted ownership.
/// </summary>
public static class C0304IsolatedIncidentPersistenceProjectionAdapter
{
    public sealed record StoredMetadata(
        int? OwnerTenantId, int? OwnerBranchId, long Version,
        string? State, bool OwnershipReadFromPersistence,
        bool Revoked, bool EvidenceSanitized, string? CommitSha);

    public sealed record Projection(string Code, Rules.PersistedIncident? Incident)
    {
        public bool AuthorizesEvidenceDisclosure => false;
        public bool AuthorizesIncidentMutation => false;
        public bool AuthorizesBackupRestore => false;
        public bool CertifiesPersistedOwnership => false;
    }

    public static Projection Project(StoredMetadata? source,
        int trustedTenantId, int? trustedBranchId, string? expectedCommitSha)
    {
        if (source is null || trustedTenantId <= 0)
            return Denied("VERIFIED_INCIDENT_SOURCE_REQUIRED");
        if (!source.OwnershipReadFromPersistence)
            return Denied("PERSISTENCE_PROVENANCE_UNVERIFIED");
        if (source.OwnerTenantId is not > 0 ||
            source.OwnerTenantId != trustedTenantId)
            return Denied("INCIDENT_CROSS_TENANT_DENIED");
        if (source.OwnerBranchId is not null &&
            (source.OwnerBranchId <= 0 || trustedBranchId != source.OwnerBranchId))
            return Denied("INCIDENT_BRANCH_SCOPE_DENIED");
        if (!CanonicalSha(expectedCommitSha) ||
            !string.Equals(source.CommitSha, expectedCommitSha, StringComparison.Ordinal))
            return Denied("EXACT_SOURCE_SHA_REQUIRED");
        if (source.Version < 0)
            return Denied("INVALID_INCIDENT_VERSION");
        if (source.Revoked)
            return Denied("INCIDENT_ACCESS_REVOKED");
        if (!source.EvidenceSanitized)
            return Denied("SANITIZED_EVIDENCE_REQUIRED");

        Rules.IncidentState? state = source.State switch
        {
            "Open" => Rules.IncidentState.Open,
            "Contained" => Rules.IncidentState.Contained,
            "Resolved" => Rules.IncidentState.Resolved,
            _ => null
        };
        if (state is null)
            return Denied("UNKNOWN_INCIDENT_STATE");

        // This metadata alone never establishes JWT, permission, source
        // authenticity, read grants or an approved backup/restore operation.
        var projection = new Rules.PersistedIncident(
            source.OwnerTenantId, source.OwnerBranchId, source.Version,
            state.Value, source.OwnershipReadFromPersistence,
            source.Revoked, source.EvidenceSanitized);
        return new Projection("AUTHENTICATED_INCIDENT_GATE_REQUIRED", projection);
    }

    private static Projection Denied(string code) => new(code, null);
    private static bool CanonicalSha(string? sha) =>
        sha is { Length: 40 } &&
        sha.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
}
