using Solqaryn.Domain.AutomationV5;
using Xunit;
using Guard = Solqaryn.Domain.AutomationV5.V5SharedDedicatedPlacementDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class V5SharedDedicatedPlacementDomainRulesTests
{
    private static Guard.TrustedPlacementSnapshot Snapshot() => new()
    {
        VerifiedTenantId = "tenant-A",
        AuthenticatedActorId = "actor-A",
        JwtValidatedByBackend = true,
        CurrentMembershipVerified = true,
        CurrentRoleCanReviewPlacement = true,
        CurrentRoleCanProposeMigration = true,
        TrustedConfigurationAvailable = true,
        AssignmentOwnerTenantId = "tenant-A",
        CurrentAssignmentRevision = 7,
        CurrentBindingAlias = "storage-shared-A",
        BindingFromTrustedCatalog = true,
        UniqueActiveAssignmentVerified = true
    };

    private static Guard.PlacementCandidate Request(
        Guard.Intent intent = Guard.Intent.InspectExistingAssignment) => new()
    {
        Intent = intent,
        RequestedTenantId = "tenant-A",
        TargetMode = Guard.PlacementMode.Shared,
        TargetBindingAlias = "storage-dedicated-A",
        TargetBindingFromTrustedCatalog = true,
        TargetOwnedByVerifiedTenant = true,
        ExpectedAssignmentRevision = 7,
        UsesVerifiedMigrationIntent = true,
        RollbackTargetVerified = true,
        VerifiedSandboxBackupRestore = true,
        TenantDataConsistencyProofVerified = true,
        IdempotencyKey = "intent-v7-A"
    };

    private static string Code(Guard.TrustedPlacementSnapshot? e = null,
        Guard.PlacementCandidate? c = null) =>
        Guard.Evaluate(e ?? Snapshot(), c ?? Request()).Code;

    [Fact]
    public void ExistingAssignmentOnlyRequestsIndependentRuntimeEnforcement()
    {
        var result = Guard.Evaluate(Snapshot(), Request());
        Assert.True(result.RequiresServerEnforcement);
        Assert.False(result.AuthorizesConnection);
        Assert.False(result.AuthorizesMigration);
    }

    [Fact]
    public void VerifiedDedicatedMigrationIntentIsStillNotACutoverAuthorization()
    {
        var candidate = Request(Guard.Intent.ProposeMigration) with
        {
            TargetMode = Guard.PlacementMode.Dedicated
        };
        var result = Guard.Evaluate(Snapshot(), candidate);
        Assert.Equal("TRUSTED_PLACEMENT_RUNTIME_VERIFICATION_REQUIRED", result.Code);
        Assert.False(result.AuthorizesMigration);
    }

    [Fact]
    public void AnonymousSuspendedOrRevokedTenantsAreDenied()
    {
        Assert.Equal("TRUSTED_AUTHENTICATED_TENANT_REQUIRED",
            Guard.Evaluate(null, Request()).Code);
        Assert.Equal("TRUSTED_AUTHENTICATED_TENANT_REQUIRED",
            Guard.Evaluate(Snapshot(), null).Code);
        Assert.Equal("TRUSTED_AUTHENTICATED_TENANT_REQUIRED",
            Code(Snapshot() with { JwtValidatedByBackend = false }));
        Assert.Equal("TRUSTED_AUTHENTICATED_TENANT_REQUIRED",
            Code(Snapshot() with { TenantSuspendedOrRevoked = true }));
        Assert.Equal("TRUSTED_AUTHENTICATED_TENANT_REQUIRED",
            Code(Snapshot() with { CurrentMembershipVerified = false }));
    }

    [Fact]
    public void TenantBAndClientTenantOverrideCannotAccessTenantA()
    {
        Assert.Equal("CROSS_TENANT_PLACEMENT_DENIED",
            Code(c: Request() with { RequestedTenantId = "tenant-B" }));
        Assert.Equal("TRUSTED_PLACEMENT_ASSIGNMENT_REQUIRED",
            Code(Snapshot() with { AssignmentOwnerTenantId = "tenant-B" }));
        Assert.Equal("TRUSTED_PLACEMENT_ASSIGNMENT_REQUIRED",
            Code(Snapshot() with { BindingFromTrustedCatalog = false }));
    }

    [Fact]
    public void SuppliedDsnAndUnsafeFallbackNeverPass()
    {
        Assert.Equal("UNAUTHORIZED_STORAGE_MUTATION_DENIED",
            Code(c: Request() with { ClientSuppliedConnectionOrDsn = true }));
        Assert.Equal("UNAUTHORIZED_STORAGE_MUTATION_DENIED",
            Code(c: Request() with { ClientMaySelectInfrastructure = true }));
        Assert.Equal("UNAUTHORIZED_STORAGE_MUTATION_DENIED",
            Code(c: Request() with { AllowsCrossTenantFallback = true }));
    }

    [Fact]
    public void CutoverDualWriteAndDestructiveSchemaCannotBeRequested()
    {
        Assert.Equal("UNAUTHORIZED_STORAGE_MUTATION_DENIED",
            Code(c: Request() with { RequestsLiveCutoverOrDualWrite = true }));
        Assert.Equal("UNAUTHORIZED_STORAGE_MUTATION_DENIED",
            Code(c: Request() with { AttemptsDestructiveSchemaChange = true }));
    }

    [Fact]
    public void StaleZeroAndFutureRevisionFailClosed()
    {
        Assert.Equal("STALE_PLACEMENT_REVISION",
            Code(c: Request() with { ExpectedAssignmentRevision = 6 }));
        Assert.Equal("STALE_PLACEMENT_REVISION",
            Code(c: Request() with { ExpectedAssignmentRevision = 0 }));
        Assert.Equal("STALE_PLACEMENT_REVISION",
            Code(c: Request() with { ExpectedAssignmentRevision = 8 }));
    }

    [Fact]
    public void RevokedReviewerAndMigrationRoleFailClosed()
    {
        Assert.Equal("PLACEMENT_REVIEW_ROLE_REQUIRED",
            Code(Snapshot() with { CurrentRoleCanReviewPlacement = false }));
        Assert.Equal("MIGRATION_INTENT_AUTHORITY_REQUIRED",
            Code(Snapshot() with { CurrentRoleCanProposeMigration = false },
                Request(Guard.Intent.ProposeMigration)));
    }

    [Fact]
    public void MissingMigrationIntentIdempotencyAndTargetAreDenied()
    {
        var candidate = Request(Guard.Intent.ProposeMigration);
        Assert.Equal("MIGRATION_INTENT_AUTHORITY_REQUIRED",
            Code(c: candidate with { UsesVerifiedMigrationIntent = false }));
        Assert.Equal("MIGRATION_INTENT_AUTHORITY_REQUIRED",
            Code(c: candidate with { IdempotencyKey = null }));
        Assert.Equal("TRUSTED_TARGET_BINDING_REQUIRED",
            Code(c: candidate with { TargetOwnedByVerifiedTenant = false }));
        Assert.Equal("TRUSTED_TARGET_BINDING_REQUIRED",
            Code(c: candidate with { TargetBindingAlias = "mysql://secret@server" }));
    }

    [Fact]
    public void FailedRestoreChecksumOrRollbackEvidenceBlocksProposal()
    {
        var candidate = Request(Guard.Intent.ProposeMigration);
        Assert.Equal("RECOVERY_AND_CONSISTENCY_EVIDENCE_REQUIRED",
            Code(c: candidate with { RollbackTargetVerified = false }));
        Assert.Equal("RECOVERY_AND_CONSISTENCY_EVIDENCE_REQUIRED",
            Code(c: candidate with { VerifiedSandboxBackupRestore = false }));
        Assert.Equal("RECOVERY_AND_CONSISTENCY_EVIDENCE_REQUIRED",
            Code(c: candidate with { TenantDataConsistencyProofVerified = false }));
    }

    [Fact]
    public void UnknownModesAndUntrustedAssignmentNeverPass()
    {
        Assert.Equal("UNKNOWN_PLACEMENT_OPERATION",
            Code(c: Request() with { TargetMode = (Guard.PlacementMode)99 }));
        Assert.Equal("UNKNOWN_PLACEMENT_OPERATION",
            Code(c: Request() with { Intent = (Guard.Intent)99 }));
        Assert.Equal("TRUSTED_PLACEMENT_ASSIGNMENT_REQUIRED",
            Code(Snapshot() with { TrustedConfigurationAvailable = false }));
        Assert.Equal("TRUSTED_PLACEMENT_ASSIGNMENT_REQUIRED",
            Code(Snapshot() with { UniqueActiveAssignmentVerified = false }));
    }
}
