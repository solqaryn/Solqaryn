using Solqaryn.Domain.AutomationV5;
using Xunit;
using Guard = Solqaryn.Domain.AutomationV5.V5PrivateMySqlCutoverDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class V5PrivateMySqlCutoverDomainRulesTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static Guard.TrustedSnapshot Trusted() => new()
    {
        VerifiedTenantId = "tenant-A",
        AuthenticatedActorId = "actor-01",
        JwtVerifiedByServer = true,
        MembershipCurrent = true,
        RoleCanReviewCutover = true,
        RecentMfaVerified = true,
        TenantMappingFromPersistence = true,
        VerifiedLeastPrivilegeGrants = true,
        NoGrantOptionOrAdminPrivileges = true,
        IndependentTechnicalApprovalVerified = true,
        ExactShaRequiredChecksSuccess = true,
        EphemeralMySql84IntegrationEvidenceVerified = true,
        BackupIntegrityVerified = true,
        EphemeralRestoreVerified = true,
        ReversibleRollbackEvidenceVerified = true,
        ExclusiveLeaseAndFencingVerified = true
    };

    private static Guard.Candidate Request() => new()
    {
        Intent = Guard.Intent.InspectReadiness,
        Environment = "dev",
        RequestedTenantId = "tenant-A",
        ExpectedCommitSha = Sha,
        ManifestCommitSha = Sha,
        SourceAndDestinationOwnedByTenant = true,
        IdempotencyKey = "dryrun-01"
    };

    private static string Check(Guard.TrustedSnapshot? authority = null,
        Guard.Candidate? candidate = null) =>
        Guard.Evaluate(authority ?? Trusted(), candidate ?? Request()).Code;

    [Fact]
    public void ValidDevSnapshotStillCannotAuthorizeSqlCutoverOrDeployment()
    {
        var result = Guard.Evaluate(Trusted(), Request());
        Assert.Equal("TRUSTED_DEV_SANDBOX_REVIEW_REQUIRED", result.Code);
        Assert.True(result.RequiresIndependentServerEnforcement);
        Assert.False(result.AuthorizesSqlOrMigration);
        Assert.False(result.AuthorizesCutover);
        Assert.False(result.AuthorizesDeployment);
    }

    [Fact]
    public void NullAndInvalidActorEvidenceFailClosed()
    {
        Assert.Equal("VERIFIED_SERVER_ACTOR_REQUIRED", Guard.Evaluate(null, Request()).Code);
        Assert.Equal("VERIFIED_SERVER_ACTOR_REQUIRED", Guard.Evaluate(Trusted(), null).Code);
        Assert.Equal("VERIFIED_SERVER_ACTOR_REQUIRED", Check(Trusted() with { JwtVerifiedByServer = false }));
        Assert.Equal("VERIFIED_SERVER_ACTOR_REQUIRED", Check(Trusted() with { MembershipRevoked = true }));
        Assert.Equal("VERIFIED_SERVER_ACTOR_REQUIRED", Check(Trusted() with { RecentMfaVerified = false }));
        Assert.Equal("VERIFIED_SERVER_ACTOR_REQUIRED", Check(Trusted() with { RoleCanReviewCutover = false }));
        Assert.Equal("VERIFIED_SERVER_ACTOR_REQUIRED", Check(Trusted() with { AuthenticatedActorId = "actor/unsafe" }));
    }

    [Theory]
    [InlineData("qa")]
    [InlineData("prod")]
    [InlineData("main")]
    [InlineData("DEV")]
    public void OnlyExactDevEnvironmentCanBeInspected(string environment)
    {
        Assert.Equal("NON_DEV_ENVIRONMENT_DENIED",
            Check(candidate: Request() with { Environment = environment }));
    }

    [Fact]
    public void UnknownIntentAndCrossTenantAccessAreDenied()
    {
        Assert.Equal("UNKNOWN_CUTOVER_INTENT",
            Check(candidate: Request() with { Intent = (Guard.Intent)99 }));
        Assert.Equal("TENANT_OWNERSHIP_UNVERIFIED",
            Check(candidate: Request() with { RequestedTenantId = "tenant-B" }));
        Assert.Equal("TENANT_OWNERSHIP_UNVERIFIED",
            Check(Trusted() with { TenantMappingFromPersistence = false }));
        Assert.Equal("TENANT_OWNERSHIP_UNVERIFIED",
            Check(candidate: Request() with { SourceAndDestinationOwnedByTenant = false }));
    }

    [Fact]
    public void MissingOrStaleShaIsNeverAccepted()
    {
        Assert.Equal("EXACT_MANIFEST_SHA_REQUIRED",
            Check(candidate: Request() with { ExpectedCommitSha = "short" }));
        Assert.Equal("EXACT_MANIFEST_SHA_REQUIRED",
            Check(candidate: Request() with { ManifestCommitSha = new string('a', 40) }));
    }

    [Fact]
    public void ForbiddenRuntimeMutationsAreAlwaysDenied()
    {
        var request = Request();
        Assert.Equal("UNAUTHORIZED_RUNTIME_MUTATION_DENIED", Check(candidate: request with { RequestsRealCutoverOrDualWrite = true }));
        Assert.Equal("UNAUTHORIZED_RUNTIME_MUTATION_DENIED", Check(candidate: request with { RequestsDestructiveSchemaMutation = true }));
        Assert.Equal("UNAUTHORIZED_RUNTIME_MUTATION_DENIED", Check(candidate: request with { ClientControlsConnectionOrTarget = true }));
        Assert.Equal("UNAUTHORIZED_RUNTIME_MUTATION_DENIED", Check(candidate: request with { RequestsGrantElevation = true }));
        Assert.Equal("UNAUTHORIZED_RUNTIME_MUTATION_DENIED", Check(candidate: request with { AllowsCrossTenantFallback = true }));
        Assert.Equal("UNAUTHORIZED_RUNTIME_MUTATION_DENIED", Check(candidate: request with { RequestsSecretOrPersonalDataEmission = true }));
    }

    [Fact]
    public void UnmitigatedCriticalFindingStopsReview()
    {
        Assert.Equal("CRITICAL_FINDING_NO_GO",
            Check(candidate: Request() with { HasUnmitigatedP0OrP1 = true }));
    }

    [Fact]
    public void AdministrativePrivilegesAndMissingIndependentApprovalFailClosed()
    {
        Assert.Equal("LEAST_PRIVILEGE_GRANTS_REQUIRED",
            Check(Trusted() with { NoGrantOptionOrAdminPrivileges = false }));
        Assert.Equal("LEAST_PRIVILEGE_GRANTS_REQUIRED",
            Check(Trusted() with { VerifiedLeastPrivilegeGrants = false }));
        Assert.Equal("INDEPENDENT_EXACT_SHA_GATES_REQUIRED",
            Check(Trusted() with { IndependentTechnicalApprovalVerified = false }));
        Assert.Equal("INDEPENDENT_EXACT_SHA_GATES_REQUIRED",
            Check(Trusted() with { ExactShaRequiredChecksSuccess = false }));
    }

    [Fact]
    public void MissingMysql84IntegrationRestoreOrRollbackEvidenceStopsReview()
    {
        Assert.Equal("EPHEMERAL_MYSQL84_RECOVERY_PROOF_REQUIRED",
            Check(Trusted() with { EphemeralMySql84IntegrationEvidenceVerified = false }));
        Assert.Equal("EPHEMERAL_MYSQL84_RECOVERY_PROOF_REQUIRED",
            Check(Trusted() with { BackupIntegrityVerified = false }));
        Assert.Equal("EPHEMERAL_MYSQL84_RECOVERY_PROOF_REQUIRED",
            Check(Trusted() with { EphemeralRestoreVerified = false }));
        Assert.Equal("EPHEMERAL_MYSQL84_RECOVERY_PROOF_REQUIRED",
            Check(Trusted() with { ReversibleRollbackEvidenceVerified = false }));
    }

    [Fact]
    public void MissingExclusiveFencingAndIdempotencyStopEvenDryRun()
    {
        Assert.Equal("SINGLE_WRITER_FENCING_REQUIRED",
            Check(Trusted() with { ExclusiveLeaseAndFencingVerified = false }));
        Assert.Equal("DRY_RUN_IDEMPOTENCY_REQUIRED",
            Check(candidate: Request() with { Intent = Guard.Intent.ProposeIsolatedDryRun, IdempotencyKey = "" }));
        Assert.Equal("DRY_RUN_IDEMPOTENCY_REQUIRED",
            Check(candidate: Request() with { Intent = Guard.Intent.ProposeIsolatedDryRun, IdempotencyKey = "unsafe/key" }));
        var result = Guard.Evaluate(Trusted(), Request() with { Intent = Guard.Intent.ProposeIsolatedDryRun });
        Assert.Equal("TRUSTED_DEV_SANDBOX_REVIEW_REQUIRED", result.Code);
        Assert.False(result.AuthorizesCutover);
    }
}
