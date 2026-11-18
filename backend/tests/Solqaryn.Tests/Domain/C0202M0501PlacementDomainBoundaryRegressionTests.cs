using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5SharedDedicatedPlacementDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class C0202M0501PlacementDomainBoundaryRegressionTests
{
    private static Rules.TrustedPlacementSnapshot Verified(
        string tenant = "tenant-A") => new()
    {
        VerifiedTenantId = tenant,
        AuthenticatedActorId = "actor-A",
        JwtValidatedByBackend = true,
        CurrentMembershipVerified = true,
        CurrentRoleCanReviewPlacement = true,
        CurrentRoleCanProposeMigration = true,
        TrustedConfigurationAvailable = true,
        AssignmentOwnerTenantId = tenant,
        CurrentAssignmentRevision = 9,
        CurrentBindingAlias = "storage_shared_A",
        BindingFromTrustedCatalog = true,
        UniqueActiveAssignmentVerified = true
    };

    private static Rules.PlacementCandidate Candidate(
        string tenant = "tenant-A", Rules.Intent intent = Rules.Intent.InspectExistingAssignment) =>
        new()
        {
            Intent = intent,
            RequestedTenantId = tenant,
            TargetMode = Rules.PlacementMode.Dedicated,
            TargetBindingAlias = "storage_dedicated_A",
            TargetBindingFromTrustedCatalog = true,
            TargetOwnedByVerifiedTenant = true,
            ExpectedAssignmentRevision = 9,
            UsesVerifiedMigrationIntent = true,
            RollbackTargetVerified = true,
            VerifiedSandboxBackupRestore = true,
            TenantDataConsistencyProofVerified = true,
            IdempotencyKey = "migration_09"
        };

    private static void NoAuthority(Rules.Verdict result)
    {
        Assert.False(result.AuthorizesConnection);
        Assert.False(result.AuthorizesMigration);
    }

    [Fact]
    public void TenantIdLengthBoundaryIsExactAndNeverGrantsConnection()
    {
        var tenant128 = new string('t', 128);
        var accepted = Rules.Evaluate(Verified(tenant128), Candidate(tenant128));
        Assert.Equal("TRUSTED_PLACEMENT_RUNTIME_VERIFICATION_REQUIRED", accepted.Code);
        Assert.True(accepted.RequiresServerEnforcement);
        NoAuthority(accepted);

        var tenant129 = new string('t', 129);
        var denied = Rules.Evaluate(Verified(tenant129), Candidate(tenant129));
        Assert.Equal("TRUSTED_AUTHENTICATED_TENANT_REQUIRED", denied.Code);
        NoAuthority(denied);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("actor\u200B7")]
    [InlineData("actor\n7")]
    [InlineData("actor/7")]
    public void MalformedVerifiedActorCannotPassPreflight(string actor)
    {
        var denied = Rules.Evaluate(Verified() with { AuthenticatedActorId = actor }, Candidate());
        Assert.Equal("TRUSTED_AUTHENTICATED_TENANT_REQUIRED", denied.Code);
        NoAuthority(denied);
    }

    [Fact]
    public void TenantNamesAreCaseSensitiveAndCannotCrossAssignment()
    {
        var mismatch = Rules.Evaluate(Verified(), Candidate("tenant-a"));
        Assert.Equal("CROSS_TENANT_PLACEMENT_DENIED", mismatch.Code);
        NoAuthority(mismatch);

        var wrongOwner = Rules.Evaluate(Verified() with { AssignmentOwnerTenantId = "tenant-a" }, Candidate());
        Assert.Equal("TRUSTED_PLACEMENT_ASSIGNMENT_REQUIRED", wrongOwner.Code);
        NoAuthority(wrongOwner);
    }

    [Fact]
    public void UnexpectedCurrentBindingAndNonUniqueAssignmentFailClosed()
    {
        var invalidBinding = Rules.Evaluate(Verified() with { CurrentBindingAlias = "mysql://server" }, Candidate());
        Assert.Equal("TRUSTED_PLACEMENT_ASSIGNMENT_REQUIRED", invalidBinding.Code);
        NoAuthority(invalidBinding);

        var ambiguous = Rules.Evaluate(Verified() with { UniqueActiveAssignmentVerified = false }, Candidate());
        Assert.Equal("TRUSTED_PLACEMENT_ASSIGNMENT_REQUIRED", ambiguous.Code);
        NoAuthority(ambiguous);
    }

    [Fact]
    public void MigrationIdempotencyKeyBoundaryIsStrictAndInert()
    {
        var request = Candidate(intent: Rules.Intent.ProposeMigration);
        var maximum = Rules.Evaluate(Verified(), request with { IdempotencyKey = new string('m', 128) });
        Assert.Equal("TRUSTED_PLACEMENT_RUNTIME_VERIFICATION_REQUIRED", maximum.Code);
        NoAuthority(maximum);

        foreach (var unsafeKey in new[] { new string('m', 129), "bad key", "bad/tenant", "", "\u200B" })
        {
            var denied = Rules.Evaluate(Verified(), request with { IdempotencyKey = unsafeKey });
            Assert.Equal("MIGRATION_INTENT_AUTHORITY_REQUIRED", denied.Code);
            NoAuthority(denied);
        }
    }

    [Fact]
    public void ProviderAndTenantTargetAreIndependentlyRequired()
    {
        var request = Candidate(intent: Rules.Intent.ProposeMigration);
        var notOwned = Rules.Evaluate(Verified(), request with { TargetOwnedByVerifiedTenant = false });
        Assert.Equal("TRUSTED_TARGET_BINDING_REQUIRED", notOwned.Code);
        NoAuthority(notOwned);

        var forged = Rules.Evaluate(Verified(), request with { TargetBindingAlias = "mysql://root@other" });
        Assert.Equal("TRUSTED_TARGET_BINDING_REQUIRED", forged.Code);
        NoAuthority(forged);
    }

    [Fact]
    public void RevokedOrUnverifiedActorCannotFallBackToOtherTenant()
    {
        var revoked = Rules.Evaluate(Verified() with { TenantSuspendedOrRevoked = true },
            Candidate() with { AllowsCrossTenantFallback = true });
        Assert.Equal("TRUSTED_AUTHENTICATED_TENANT_REQUIRED", revoked.Code);
        NoAuthority(revoked);

        var bypass = Rules.Evaluate(Verified(), Candidate() with { AllowsCrossTenantFallback = true });
        Assert.Equal("UNAUTHORIZED_STORAGE_MUTATION_DENIED", bypass.Code);
        NoAuthority(bypass);
    }

    [Fact]
    public void StaleStoredRevisionDeniesBeforeRecoveryProof()
    {
        var failed = Rules.Evaluate(Verified() with { CurrentAssignmentRevision = 0 }, Candidate());
        Assert.Equal("STALE_PLACEMENT_REVISION", failed.Code);
        NoAuthority(failed);

        var later = Rules.Evaluate(Verified() with { CurrentAssignmentRevision = 10 }, Candidate());
        Assert.Equal("STALE_PLACEMENT_REVISION", later.Code);
        NoAuthority(later);
    }

    [Fact]
    public void NullAndUnknownOperationsNeverAuthorizeStorage()
    {
        NoAuthority(Rules.Evaluate(null, Candidate()));
        NoAuthority(Rules.Evaluate(Verified(), null));
        var unknownMode = Rules.Evaluate(Verified(), Candidate() with { TargetMode = (Rules.PlacementMode)0 });
        Assert.Equal("UNKNOWN_PLACEMENT_OPERATION", unknownMode.Code);
        NoAuthority(unknownMode);
    }
}
