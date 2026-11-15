using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ArchitectureMigrationDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class V5ArchitectureMigrationApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly V5ArchitectureMigrationApplicationService Service = new();

    private static Rules.TrustedBoundary Boundary() =>
        new(Sha, "tenant-A", true, true,
            new HashSet<string>(StringComparer.Ordinal) { "Render", "Vercel", "Aiven", "Cloudinary" });

    private static Rules.Candidate Candidate() =>
        new(Sha, "tenant-A", "Render", "Render", "dev", true, true,
            false, false, true, true, true, true, false);

    private static V5ArchitectureMigrationApplicationService.Request Request(
        params Rules.Candidate?[] values) =>
        new(Boundary(), values.Length == 0 ? new Rules.Candidate?[] { Candidate() } : values);

    [Fact]
    public void TrustedStructuralCandidatesStillRequireRuntimeEvidenceAndIndependentA5()
    {
        var result = Service.Prepare(Request(Candidate(), Candidate()));
        Assert.Equal("INDEPENDENT_RUNTIME_EVIDENCE_AND_A5_REQUIRED", result.Code);
        Assert.Equal(2, result.ItemsReviewed);
        Assert.False(result.AuthorizesDeployment);
        Assert.False(result.AuthorizesDataMigration);
        Assert.False(result.AuthorizesExternalWrite);
        Assert.False(result.CertifiesBackupOrRestore);
    }

    [Fact]
    public void UntrustedMembershipAndTenantBFailClosed()
    {
        Assert.Equal("TRUSTED_DEV_AUTHORITY_REQUIRED",
            Service.Prepare(Request(Candidate()) with
                { Boundary = Boundary() with { CurrentMembershipVerified = false } }).Code);
        Assert.Equal("TENANT_OWNERSHIP_OR_MAPPING_UNVERIFIED",
            Service.Prepare(Request(Candidate() with { TenantId = "tenant-B" })).Code);
        Assert.Equal("TENANT_OWNERSHIP_OR_MAPPING_UNVERIFIED",
            Service.Prepare(Request(Candidate() with { SourceOwnershipFromTrustedCatalog = false })).Code);
    }

    [Fact]
    public void QaProductionAndStaleShaAreNotAllowed()
    {
        Assert.Equal("PRODUCTION_OR_QA_NOT_AUTHORIZED",
            Service.Prepare(Request(Candidate() with { Environment = "prod" })).Code);
        Assert.Equal("PRODUCTION_OR_QA_NOT_AUTHORIZED",
            Service.Prepare(Request(Candidate() with { Environment = "qa" })).Code);
        Assert.Equal("MANIFEST_EXACT_SHA_REQUIRED",
            Service.Prepare(Request(Candidate() with { ManifestSha = new string('a', 40) })).Code);
    }

    [Fact]
    public void UnapprovedProvidersAndLiveMutationsAreBlocked()
    {
        Assert.Equal("PROVIDER_NOT_IN_VERIFIED_DEV_CATALOG",
            Service.Prepare(Request(Candidate() with { TargetProvider = "Cloudflare" })).Code);
        Assert.Equal("PROVIDER_NOT_IN_VERIFIED_DEV_CATALOG",
            Service.Prepare(Request(Candidate() with { TargetProvider = "Hostinger" })).Code);
        Assert.Equal("RUNTIME_MUTATION_OUT_OF_SCOPE",
            Service.Prepare(Request(Candidate() with { RequiresLiveDnsOrSecretMutation = true })).Code);
        Assert.Equal("RUNTIME_MUTATION_OUT_OF_SCOPE",
            Service.Prepare(Request(Candidate() with { DestructiveDatabaseOperation = true })).Code);
    }

    [Fact]
    public void MissingSandboxRestoreBackupOrRollbackCannotPass()
    {
        Assert.Equal("RECOVERY_EVIDENCE_REQUIRED",
            Service.Prepare(Request(Candidate() with { SandboxRestoreVerified = false })).Code);
        Assert.Equal("RECOVERY_EVIDENCE_REQUIRED",
            Service.Prepare(Request(Candidate() with { BackupChecksumVerified = false })).Code);
        Assert.Equal("RECOVERY_EVIDENCE_REQUIRED",
            Service.Prepare(Request(Candidate() with { ReversibleRollbackPlanVerified = false })).Code);
    }

    [Fact]
    public void UnmitigatedCriticalFindingAndUnknownAuthorityBlockReview()
    {
        Assert.Equal("UNMITIGATED_CRITICAL_FINDING",
            Service.Prepare(Request(Candidate() with { HasUnmitigatedCriticalFinding = true })).Code);
        Assert.Equal("TRUSTED_REQUEST_REQUIRED", Service.Prepare(null).Code);
        Assert.Equal("TRUSTED_REQUEST_REQUIRED",
            Service.Prepare(Request(Candidate()) with { Boundary = null }).Code);
    }

    [Fact]
    public void BoundedBatchRejectsEmptyOversizedOrNullEntries()
    {
        Assert.Equal("CANDIDATE_CAPACITY_DENIED",
            Service.Prepare(Request(Candidate()) with { MaxCandidates = 0 }).Code);
        Assert.Equal("CANDIDATE_CAPACITY_DENIED",
            Service.Prepare(Request(Candidate()) with { MaxCandidates = 65 }).Code);
        Assert.Equal("CANDIDATE_CAPACITY_DENIED",
            Service.Prepare(Request(Candidate()) with { Candidates = Array.Empty<Rules.Candidate?>() }).Code);
        Assert.Equal("NULL_CANDIDATE_DENIED",
            Service.Prepare(Request(Candidate(), null)).Code);
    }

    [Fact]
    public void FirstDeniedProposalStopsSubsequentProcessing()
    {
        var result = Service.Prepare(Request(
            Candidate(), Candidate() with { TenantId = "tenant-B" }, Candidate()));
        Assert.Equal("TENANT_OWNERSHIP_OR_MAPPING_UNVERIFIED", result.Code);
        Assert.Equal(2, result.ItemsReviewed);
        Assert.False(result.AuthorizesExternalWrite);
    }
}
