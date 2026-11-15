using System;
using System.Linq;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C02-02.M03.01: inert fail-closed placement preflight for shared/dedicated
/// tenancy. It neither selects a database connection nor authorizes cutover.
/// Trusted runtime services must verify all snapshots and enforce CAS, JWT,
/// RBAC, ownership, backup restoration and transactional fencing independently.
/// </summary>
public static class V5SharedDedicatedPlacementDomainRules
{
    public enum PlacementMode { Shared = 1, Dedicated = 2 }
    public enum Intent { InspectExistingAssignment = 1, ProposeMigration = 2 }

    public sealed record TrustedPlacementSnapshot
    {
        public string? VerifiedTenantId { get; init; }
        public string? AuthenticatedActorId { get; init; }
        public bool JwtValidatedByBackend { get; init; }
        public bool CurrentMembershipVerified { get; init; }
        public bool CurrentRoleCanReviewPlacement { get; init; }
        public bool CurrentRoleCanProposeMigration { get; init; }
        public bool TenantSuspendedOrRevoked { get; init; }
        public bool TrustedConfigurationAvailable { get; init; }
        public string? AssignmentOwnerTenantId { get; init; }
        public long CurrentAssignmentRevision { get; init; }
        public string? CurrentBindingAlias { get; init; }
        public bool BindingFromTrustedCatalog { get; init; }
        public bool UniqueActiveAssignmentVerified { get; init; }
    }

    public sealed record PlacementCandidate
    {
        public Intent Intent { get; init; }
        public string? RequestedTenantId { get; init; }
        public PlacementMode TargetMode { get; init; }
        public string? TargetBindingAlias { get; init; }
        public bool TargetBindingFromTrustedCatalog { get; init; }
        public bool TargetOwnedByVerifiedTenant { get; init; }
        public long ExpectedAssignmentRevision { get; init; }
        public bool ClientSuppliedConnectionOrDsn { get; init; }
        public bool ClientMaySelectInfrastructure { get; init; }
        public bool AllowsCrossTenantFallback { get; init; }
        public bool RequestsLiveCutoverOrDualWrite { get; init; }
        public bool AttemptsDestructiveSchemaChange { get; init; }
        public bool UsesVerifiedMigrationIntent { get; init; }
        public bool RollbackTargetVerified { get; init; }
        public bool VerifiedSandboxBackupRestore { get; init; }
        public bool TenantDataConsistencyProofVerified { get; init; }
        public string? IdempotencyKey { get; init; }
    }

    public readonly record struct Verdict(string Code)
    {
        public bool AuthorizesConnection => false;
        public bool AuthorizesMigration => false;
        public bool RequiresServerEnforcement =>
            Code == "TRUSTED_PLACEMENT_RUNTIME_VERIFICATION_REQUIRED";
    }

    private static bool SafeOpaqueId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 128 &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_' or '.');

    public static Verdict Evaluate(TrustedPlacementSnapshot? evidence, PlacementCandidate? request)
    {
        if (evidence is null || request is null ||
            !SafeOpaqueId(evidence.VerifiedTenantId) ||
            !SafeOpaqueId(evidence.AuthenticatedActorId) ||
            !evidence.JwtValidatedByBackend ||
            !evidence.CurrentMembershipVerified ||
            evidence.TenantSuspendedOrRevoked)
            return new("TRUSTED_AUTHENTICATED_TENANT_REQUIRED");

        if (!Enum.IsDefined(request.Intent) || !Enum.IsDefined(request.TargetMode))
            return new("UNKNOWN_PLACEMENT_OPERATION");

        if (!evidence.TrustedConfigurationAvailable ||
            !evidence.UniqueActiveAssignmentVerified ||
            !evidence.BindingFromTrustedCatalog ||
            !SafeOpaqueId(evidence.CurrentBindingAlias) ||
            !SafeOpaqueId(evidence.AssignmentOwnerTenantId) ||
            !string.Equals(evidence.AssignmentOwnerTenantId,
                evidence.VerifiedTenantId, StringComparison.Ordinal))
            return new("TRUSTED_PLACEMENT_ASSIGNMENT_REQUIRED");

        if (!SafeOpaqueId(request.RequestedTenantId) ||
            !string.Equals(request.RequestedTenantId,
                evidence.VerifiedTenantId, StringComparison.Ordinal))
            return new("CROSS_TENANT_PLACEMENT_DENIED");

        if (request.ClientSuppliedConnectionOrDsn ||
            request.ClientMaySelectInfrastructure ||
            request.AllowsCrossTenantFallback ||
            request.RequestsLiveCutoverOrDualWrite ||
            request.AttemptsDestructiveSchemaChange)
            return new("UNAUTHORIZED_STORAGE_MUTATION_DENIED");

        if (request.ExpectedAssignmentRevision <= 0 ||
            evidence.CurrentAssignmentRevision <= 0 ||
            request.ExpectedAssignmentRevision != evidence.CurrentAssignmentRevision)
            return new("STALE_PLACEMENT_REVISION");

        if (!evidence.CurrentRoleCanReviewPlacement)
            return new("PLACEMENT_REVIEW_ROLE_REQUIRED");

        if (request.Intent == Intent.ProposeMigration)
        {
            if (!evidence.CurrentRoleCanProposeMigration ||
                !request.UsesVerifiedMigrationIntent ||
                !SafeOpaqueId(request.IdempotencyKey))
                return new("MIGRATION_INTENT_AUTHORITY_REQUIRED");

            if (!request.TargetBindingFromTrustedCatalog ||
                !request.TargetOwnedByVerifiedTenant ||
                !SafeOpaqueId(request.TargetBindingAlias))
                return new("TRUSTED_TARGET_BINDING_REQUIRED");

            if (!request.RollbackTargetVerified ||
                !request.VerifiedSandboxBackupRestore ||
                !request.TenantDataConsistencyProofVerified)
                return new("RECOVERY_AND_CONSISTENCY_EVIDENCE_REQUIRED");
        }

        // A positive candidate still must be independently checked by the
        // server: no EF/SQL, storage router, live deployment or secret changes.
        return new("TRUSTED_PLACEMENT_RUNTIME_VERIFICATION_REQUIRED");
    }
}
