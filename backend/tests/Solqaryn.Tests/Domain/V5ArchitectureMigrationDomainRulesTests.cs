using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class V5ArchitectureMigrationDomainRulesTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static V5ArchitectureMigrationDomainRules.TrustedBoundary Authority() =>
        new(Sha, "tenant-A", true, true,
            new HashSet<string>(StringComparer.Ordinal) { "Render", "Vercel", "Aiven", "Cloudinary" });

    private static V5ArchitectureMigrationDomainRules.Candidate Candidate() =>
        new(Sha, "tenant-A", "Render", "Render", "dev", true, true,
            false, false, true, true, true, true, false);

    private static string Check(
        V5ArchitectureMigrationDomainRules.TrustedBoundary? authority = null,
        V5ArchitectureMigrationDomainRules.Candidate? candidate = null) =>
        V5ArchitectureMigrationDomainRules.Evaluate(
            authority ?? Authority(), candidate ?? Candidate()).Code;

    [Fact]
    public void FullySpecifiedDevSnapshotOnlyRequestsRuntimeReview()
    {
        var result = V5ArchitectureMigrationDomainRules.Evaluate(Authority(), Candidate());
        Assert.True(result.RequiresIndependentRuntimeVerification);
        Assert.False(result.AuthorizesDeployment);
        Assert.False(result.AuthorizesDataMigration);
    }

    [Fact]
    public void NoMembershipTenantOrVerifiedDevScopeFailsClosed()
    {
        Assert.Equal("TRUSTED_DEV_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { CurrentMembershipVerified = false }));
        Assert.Equal("TRUSTED_DEV_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { ExplicitDevScopeVerified = false }));
        Assert.Equal("TRUSTED_DEV_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { AuthenticatedTenantId = null }));
        Assert.Equal("TRUSTED_DEV_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { ExpectedCommitSha = "short" }));
    }

    [Fact]
    public void MainQaProdAndStaleShaAreNeverAuthorized()
    {
        Assert.Equal("PRODUCTION_OR_QA_NOT_AUTHORIZED",
            Check(candidate: Candidate() with { Environment = "prod" }));
        Assert.Equal("PRODUCTION_OR_QA_NOT_AUTHORIZED",
            Check(candidate: Candidate() with { Environment = "qa" }));
        Assert.Equal("MANIFEST_EXACT_SHA_REQUIRED",
            Check(candidate: Candidate() with { ManifestSha = new string('a', 40) }));
    }

    [Fact]
    public void CrossTenantAndUnverifiedInventoryAreDenied()
    {
        Assert.Equal("TENANT_OWNERSHIP_OR_MAPPING_UNVERIFIED",
            Check(candidate: Candidate() with { TenantId = "tenant-B" }));
        Assert.Equal("TENANT_OWNERSHIP_OR_MAPPING_UNVERIFIED",
            Check(candidate: Candidate() with { SourceOwnershipFromTrustedInventory = false }));
        Assert.Equal("TENANT_OWNERSHIP_OR_MAPPING_UNVERIFIED",
            Check(candidate: Candidate() with { TargetOwnershipVerified = false }));
        Assert.Equal("TENANT_OWNERSHIP_OR_MAPPING_UNVERIFIED",
            Check(candidate: Candidate() with { TenantMappingVerified = false }));
    }

    [Fact]
    public void FutureHostOrStorageProviderNeedsRealApproval()
    {
        Assert.Equal("PROVIDER_NOT_IN_VERIFIED_DEV_CATALOG",
            Check(candidate: Candidate() with { TargetProvider = "Hostinger" }));
        Assert.Equal("PROVIDER_NOT_IN_VERIFIED_DEV_CATALOG",
            Check(candidate: Candidate() with { TargetProvider = "Cloudflare" }));
        Assert.Equal("PROVIDER_NOT_IN_VERIFIED_DEV_CATALOG",
            Check(candidate: Candidate() with { TargetProvider = "R2" }));
    }

    [Fact]
    public void NoRealDnsSecretsOrDestructiveDatabaseChanges()
    {
        Assert.Equal("RUNTIME_MUTATION_OUT_OF_SCOPE",
            Check(candidate: Candidate() with { RequiresLiveDnsOrSecretMutation = true }));
        Assert.Equal("RUNTIME_MUTATION_OUT_OF_SCOPE",
            Check(candidate: Candidate() with { DestructiveDatabaseOperation = true }));
    }

    [Fact]
    public void CriticalFindingAndMissingRestoreBlockReadiness()
    {
        Assert.Equal("UNMITIGATED_CRITICAL_FINDING",
            Check(candidate: Candidate() with { HasUnmitigatedCriticalFinding = true }));
        Assert.Equal("RECOVERY_EVIDENCE_REQUIRED",
            Check(candidate: Candidate() with { BackupChecksumVerified = false }));
        Assert.Equal("RECOVERY_EVIDENCE_REQUIRED",
            Check(candidate: Candidate() with { SandboxRestoreVerified = false }));
        Assert.Equal("RECOVERY_EVIDENCE_REQUIRED",
            Check(candidate: Candidate() with { ReversibleRollbackPlanVerified = false }));
    }

    [Fact]
    public void MissingAuthorityOrCandidateIsDenied()
    {
        Assert.Equal("TRUSTED_DEV_AUTHORITY_REQUIRED",
            V5ArchitectureMigrationDomainRules.Evaluate(null, Candidate()).Code);
        Assert.Equal("TRUSTED_DEV_AUTHORITY_REQUIRED",
            V5ArchitectureMigrationDomainRules.Evaluate(Authority(), null).Code);
    }
}
