using System;
using System.Linq;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C12-04.M03.01: pure fail-closed preflight for a FUTURE private MySQL cutover.
/// Input booleans are untrusted claims until a server verifies the underlying
/// grants, CI logs, backup, tenant membership and lease. No DB, network, DNS,
/// secrets, provider provisioning, migrations or writes occur here.
/// A positive result authorizes ONLY independent server-side verification.
/// </summary>
public static class V5PrivateMySqlCutoverDomainRules
{
    public enum Intent { InspectReadiness = 1, ProposeIsolatedDryRun = 2 }

    public sealed record TrustedSnapshot
    {
        public string? VerifiedTenantId { get; init; }
        public string? AuthenticatedActorId { get; init; }
        public bool JwtVerifiedByServer { get; init; }
        public bool MembershipCurrent { get; init; }
        public bool RoleCanReviewCutover { get; init; }
        public bool RecentMfaVerified { get; init; }
        public bool MembershipRevoked { get; init; }
        public bool TenantMappingFromPersistence { get; init; }
        public bool VerifiedLeastPrivilegeGrants { get; init; }
        public bool NoGrantOptionOrAdminPrivileges { get; init; }
        public bool IndependentTechnicalApprovalVerified { get; init; }
        public bool ExactShaRequiredChecksSuccess { get; init; }
        public bool EphemeralMySql84IntegrationEvidenceVerified { get; init; }
        public bool BackupIntegrityVerified { get; init; }
        public bool EphemeralRestoreVerified { get; init; }
        public bool ReversibleRollbackEvidenceVerified { get; init; }
        public bool ExclusiveLeaseAndFencingVerified { get; init; }
    }

    public sealed record Candidate
    {
        public Intent Intent { get; init; }
        public string? Environment { get; init; }
        public string? RequestedTenantId { get; init; }
        public string? ExpectedCommitSha { get; init; }
        public string? ManifestCommitSha { get; init; }
        public string? IdempotencyKey { get; init; }
        public bool SourceAndDestinationOwnedByTenant { get; init; }
        public bool ClientControlsConnectionOrTarget { get; init; }
        public bool RequestsRealCutoverOrDualWrite { get; init; }
        public bool RequestsDestructiveSchemaMutation { get; init; }
        public bool RequestsGrantElevation { get; init; }
        public bool RequestsSecretOrPersonalDataEmission { get; init; }
        public bool AllowsCrossTenantFallback { get; init; }
        public bool HasUnmitigatedP0OrP1 { get; init; }
    }

    public readonly record struct Verdict(string Code)
    {
        public bool AuthorizesSqlOrMigration => false;
        public bool AuthorizesCutover => false;
        public bool AuthorizesDeployment => false;
        public bool RequiresIndependentServerEnforcement =>
            Code == "TRUSTED_DEV_SANDBOX_REVIEW_REQUIRED";
    }

    private static bool SafeOpaqueId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 128 &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_' or '.');

    private static bool FullCommitSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);

    public static Verdict Evaluate(TrustedSnapshot? evidence, Candidate? request)
    {
        if (evidence is null || request is null ||
            !SafeOpaqueId(evidence.VerifiedTenantId) ||
            !SafeOpaqueId(evidence.AuthenticatedActorId) ||
            !evidence.JwtVerifiedByServer ||
            !evidence.MembershipCurrent || evidence.MembershipRevoked ||
            !evidence.RoleCanReviewCutover || !evidence.RecentMfaVerified)
            return new("VERIFIED_SERVER_ACTOR_REQUIRED");

        if (!Enum.IsDefined(request.Intent))
            return new("UNKNOWN_CUTOVER_INTENT");

        if (!string.Equals(request.Environment, "dev", StringComparison.Ordinal))
            return new("NON_DEV_ENVIRONMENT_DENIED");

        if (!SafeOpaqueId(request.RequestedTenantId) ||
            !string.Equals(request.RequestedTenantId, evidence.VerifiedTenantId, StringComparison.Ordinal) ||
            !evidence.TenantMappingFromPersistence ||
            !request.SourceAndDestinationOwnedByTenant)
            return new("TENANT_OWNERSHIP_UNVERIFIED");

        if (!FullCommitSha(request.ExpectedCommitSha) ||
            !string.Equals(request.ManifestCommitSha, request.ExpectedCommitSha, StringComparison.Ordinal))
            return new("EXACT_MANIFEST_SHA_REQUIRED");

        if (request.ClientControlsConnectionOrTarget ||
            request.RequestsRealCutoverOrDualWrite ||
            request.RequestsDestructiveSchemaMutation ||
            request.RequestsGrantElevation ||
            request.RequestsSecretOrPersonalDataEmission ||
            request.AllowsCrossTenantFallback)
            return new("UNAUTHORIZED_RUNTIME_MUTATION_DENIED");

        if (request.HasUnmitigatedP0OrP1)
            return new("CRITICAL_FINDING_NO_GO");

        if (!evidence.VerifiedLeastPrivilegeGrants ||
            !evidence.NoGrantOptionOrAdminPrivileges)
            return new("LEAST_PRIVILEGE_GRANTS_REQUIRED");

        if (!evidence.IndependentTechnicalApprovalVerified ||
            !evidence.ExactShaRequiredChecksSuccess)
            return new("INDEPENDENT_EXACT_SHA_GATES_REQUIRED");

        // Real Category=Integration tests on ephemeral MySQL 8.4 and verified
        // restore are REQUIRED before any later cutover authorization.
        // This pure domain rule never runs or attests those tests itself.
        if (!evidence.EphemeralMySql84IntegrationEvidenceVerified ||
            !evidence.BackupIntegrityVerified ||
            !evidence.EphemeralRestoreVerified ||
            !evidence.ReversibleRollbackEvidenceVerified)
            return new("EPHEMERAL_MYSQL84_RECOVERY_PROOF_REQUIRED");

        if (!evidence.ExclusiveLeaseAndFencingVerified)
            return new("SINGLE_WRITER_FENCING_REQUIRED");

        if (request.Intent == Intent.ProposeIsolatedDryRun &&
            !SafeOpaqueId(request.IdempotencyKey))
            return new("DRY_RUN_IDEMPOTENCY_REQUIRED");

        // All evidence booleans still require server-authenticated provenance.
        // No positive verdict grants SQL, DNS, provider or production authority.
        return new("TRUSTED_DEV_SANDBOX_REVIEW_REQUIRED");
    }
}
