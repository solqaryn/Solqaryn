using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;
using Guard = Solqaryn.Domain.AutomationV5.V5PrivateMySqlCutoverDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class C1204PrivateMySqlReadinessApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static C1204VerifiedActor Actor() =>
        new("actor-01", "tenant-A", true, true, true, true);

    private static Guard.Candidate Request() => new()
    {
        Intent = Guard.Intent.InspectReadiness,
        Environment = "dev",
        RequestedTenantId = "tenant-A",
        ExpectedCommitSha = Sha,
        ManifestCommitSha = Sha,
        SourceAndDestinationOwnedByTenant = true
    };

    private static Guard.TrustedSnapshot Snapshot() => new()
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

    private sealed class Reader : IC1204CutoverReadinessEvidenceReader
    {
        public int Calls { get; private set; }
        public C1204VerifiedEvidence? Evidence { get; set; } =
            new("actor-01", "tenant-A", Sha, Snapshot());
        public bool ThrowProviderError { get; set; }

        public Task<C1204VerifiedEvidence?> ReadVerifiedAsync(
            string actorId, string tenantId, string sha, CancellationToken token)
        {
            Calls++;
            if (ThrowProviderError)
                throw new InvalidOperationException("Evidence provider unavailable");
            return Task.FromResult(Evidence);
        }
    }

    [Fact]
    public async Task VerifiedSandboxReadinessNeverAuthorizesSqlCutoverOrDeployment()
    {
        var reader = new Reader();
        var result = await new C1204PrivateMySqlReadinessApplicationService(reader)
            .ReviewAsync(Actor(), Request());
        Assert.Equal("TRUSTED_DEV_SANDBOX_REVIEW_REQUIRED", result.Code);
        Assert.True(result.RequiresIndependentServerEnforcement);
        Assert.False(result.AuthorizesSqlOrMigration);
        Assert.False(result.AuthorizesCutover);
        Assert.False(result.AuthorizesDeployment);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task UnauthenticatedRevokedOrUnprivilegedCallerNeverReadsEvidence()
    {
        var reader = new Reader();
        var service = new C1204PrivateMySqlReadinessApplicationService(reader);
        Assert.Equal("VERIFIED_SERVER_ACTOR_REQUIRED",
            (await service.ReviewAsync(null, Request())).Code);
        Assert.Equal("VERIFIED_SERVER_ACTOR_REQUIRED",
            (await service.ReviewAsync(Actor() with { JwtAuthenticatedByServer = false }, Request())).Code);
        Assert.Equal("VERIFIED_SERVER_ACTOR_REQUIRED",
            (await service.ReviewAsync(Actor() with { MembershipCurrent = false }, Request())).Code);
        Assert.Equal("VERIFIED_SERVER_ACTOR_REQUIRED",
            (await service.ReviewAsync(Actor() with { CanReviewCutover = false }, Request())).Code);
        Assert.Equal("VERIFIED_SERVER_ACTOR_REQUIRED",
            (await service.ReviewAsync(Actor() with { RecentMfaVerified = false }, Request())).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task TenantBAndNonDevEnvironmentAreDeniedBeforeEvidenceRead()
    {
        var reader = new Reader();
        var service = new C1204PrivateMySqlReadinessApplicationService(reader);
        Assert.Equal("TENANT_OWNERSHIP_UNVERIFIED",
            (await service.ReviewAsync(Actor(), Request() with { RequestedTenantId = "tenant-B" })).Code);
        Assert.Equal("NON_DEV_ENVIRONMENT_DENIED",
            (await service.ReviewAsync(Actor(), Request() with { Environment = "prod" })).Code);
        Assert.Equal("NON_DEV_ENVIRONMENT_DENIED",
            (await service.ReviewAsync(Actor(), Request() with { Environment = "qa" })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task RuntimeMutationCriticalFindingAndMismatchedShaAreDeniedBeforeRead()
    {
        var reader = new Reader();
        var service = new C1204PrivateMySqlReadinessApplicationService(reader);
        Assert.Equal("UNAUTHORIZED_RUNTIME_MUTATION_DENIED",
            (await service.ReviewAsync(Actor(), Request() with { RequestsRealCutoverOrDualWrite = true })).Code);
        Assert.Equal("UNAUTHORIZED_RUNTIME_MUTATION_DENIED",
            (await service.ReviewAsync(Actor(), Request() with { RequestsGrantElevation = true })).Code);
        Assert.Equal("UNAUTHORIZED_RUNTIME_MUTATION_DENIED",
            (await service.ReviewAsync(Actor(), Request() with { RequestsDestructiveSchemaMutation = true })).Code);
        Assert.Equal("UNAUTHORIZED_RUNTIME_MUTATION_DENIED",
            (await service.ReviewAsync(Actor(), Request() with { RequestsSecretOrPersonalDataEmission = true })).Code);
        Assert.Equal("CRITICAL_FINDING_NO_GO",
            (await service.ReviewAsync(Actor(), Request() with { HasUnmitigatedP0OrP1 = true })).Code);
        Assert.Equal("EXACT_MANIFEST_SHA_REQUIRED",
            (await service.ReviewAsync(Actor(), Request() with { ManifestCommitSha = new string('f', 40) })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task MissingEvidenceIsNotMeasuredAndNeverSuccess()
    {
        var reader = new Reader { Evidence = null };
        var result = await new C1204PrivateMySqlReadinessApplicationService(reader)
            .ReviewAsync(Actor(), Request());
        Assert.Equal("EVIDENCE_NOT_VERIFIED", result.Code);
        Assert.False(result.RequiresIndependentServerEnforcement);
    }

    [Fact]
    public async Task WrongShaTenantActorOrSnapshotProvenanceCannotBeReused()
    {
        var reader = new Reader();
        var service = new C1204PrivateMySqlReadinessApplicationService(reader);
        reader.Evidence = new("actor-01", "tenant-A", new string('0', 40), Snapshot());
        Assert.Equal("EVIDENCE_PROVENANCE_MISMATCH", (await service.ReviewAsync(Actor(), Request())).Code);
        reader.Evidence = new("actor-01", "tenant-B", Sha, Snapshot());
        Assert.Equal("EVIDENCE_PROVENANCE_MISMATCH", (await service.ReviewAsync(Actor(), Request())).Code);
        reader.Evidence = new("actor-01", "tenant-A", Sha,
            Snapshot() with { AuthenticatedActorId = "actor-B" });
        Assert.Equal("EVIDENCE_PROVENANCE_MISMATCH", (await service.ReviewAsync(Actor(), Request())).Code);
    }

    [Fact]
    public async Task DomainStillRejectsUnverifiedGrantsRestoreAndFencing()
    {
        var reader = new Reader();
        var service = new C1204PrivateMySqlReadinessApplicationService(reader);
        reader.Evidence = new("actor-01", "tenant-A", Sha,
            Snapshot() with { NoGrantOptionOrAdminPrivileges = false });
        Assert.Equal("LEAST_PRIVILEGE_GRANTS_REQUIRED",
            (await service.ReviewAsync(Actor(), Request())).Code);
        reader.Evidence = new("actor-01", "tenant-A", Sha,
            Snapshot() with { EphemeralMySql84IntegrationEvidenceVerified = false });
        Assert.Equal("EPHEMERAL_MYSQL84_RECOVERY_PROOF_REQUIRED",
            (await service.ReviewAsync(Actor(), Request())).Code);
        reader.Evidence = new("actor-01", "tenant-A", Sha,
            Snapshot() with { ExclusiveLeaseAndFencingVerified = false });
        Assert.Equal("SINGLE_WRITER_FENCING_REQUIRED",
            (await service.ReviewAsync(Actor(), Request())).Code);
    }

    [Fact]
    public async Task CancellationAndProviderFailureNeverTurnIntoApproval()
    {
        var reader = new Reader { ThrowProviderError = true };
        var service = new C1204PrivateMySqlReadinessApplicationService(reader);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewAsync(Actor(), Request()));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ReviewAsync(Actor(), Request(), cancellation.Token));
    }
}
