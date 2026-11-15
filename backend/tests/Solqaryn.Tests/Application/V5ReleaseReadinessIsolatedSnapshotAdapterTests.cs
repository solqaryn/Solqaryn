using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ReleaseReadinessDomainRules;
using Adapter = Solqaryn.Application.AutomationV5.V5ReleaseReadinessIsolatedSnapshotAdapter;

namespace Solqaryn.Tests.Application;

public sealed class V5ReleaseReadinessIsolatedSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static V5ReadinessVerifiedCaller Caller() =>
        new("reviewer", "tenant-A", "grant-dev", "dev", true, true, true);

    private static Rules.VerificationContext Authority() =>
        new("trusted-dev", Sha, "tenant-A",
            new HashSet<string> { "author", "reviewer" },
            new HashSet<string> { "scope-lock", "backend-tests" },
            new HashSet<string> { "grant-dev" });

    private static Rules.Candidate Candidate() =>
        new("release-1", Sha, Sha, "sha256:" + new string('a', 64),
            "dev", "grant-dev", "tenant-A", "author", "reviewer",
            "rollback:revert", "stop:on-failure",
            new[] {
                new Rules.CheckResult("scope-lock", Sha, "completed", "success"),
                new Rules.CheckResult("backend-tests", Sha, "completed", "success")
            },
            new[] { new Rules.ObservedCase("AC-1", "dev:synthetic", true) });

    private static Adapter.Snapshot Snapshot() =>
        new("release-1", Sha, "tenant-A",
            "sha256:" + new string('a', 64), "rollback:revert",
            4, true, true, true, true);

    private sealed class Sources : IV5ReadinessAuthenticatedCallerSource,
        IV5ReleaseReadinessVerifiedEvidenceSource, IV5ReleaseReadinessSnapshotReader
    {
        public V5ReadinessVerifiedCaller? Principal { get; set; } = Caller();
        public Rules.VerificationContext? AuthorityValue { get; set; } = Authority();
        public Rules.Candidate? CandidateValue { get; set; } = Candidate();
        public Adapter.Snapshot? SnapshotValue { get; set; } = Snapshot();
        public int SnapshotReads { get; private set; }
        public string? ReadTenant { get; private set; }

        public ValueTask<V5ReadinessVerifiedCaller?> GetAuthenticatedCallerAsync(
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(Principal);

        public ValueTask<Rules.VerificationContext?> GetAuthorityAsync(
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(AuthorityValue);

        public ValueTask<Rules.Candidate?> GetCandidateAsync(
            string releaseId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(CandidateValue);

        public ValueTask<Adapter.Snapshot?> ReadForTenantAsync(
            string verifiedTenantScope, string releaseId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadTenant = verifiedTenantScope;
            SnapshotReads++;
            return ValueTask.FromResult(SnapshotValue);
        }
    }

    private static Adapter Service(Sources source) =>
        new(source, new V5ReleaseReadinessAuthorizationGate(source, source), source);

    private static Adapter.Request Request() => new("release-1", Sha);

    [Fact]
    public async Task VerifiedSnapshotIsStillOnlyExternalReviewNotAuthorization()
    {
        var source = new Sources();
        var result = await Service(source).InspectAsync(Request());
        Assert.Equal("READ_ONLY_SNAPSHOT_EXTERNAL_REVIEW_REQUIRED", result.Code);
        Assert.True(result.NeedsExternalAuthenticatedReview);
        Assert.False(result.AllowsRelease);
        Assert.False(result.AllowsDeployment);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.GrantsTenantAccess);
        Assert.False(result.CertifiesDatabaseRestore);
        Assert.False(result.ExposesSnapshotData);
        Assert.Equal("tenant-A", source.ReadTenant);
        Assert.Equal(1, source.SnapshotReads);
    }

    [Theory]
    [InlineData("qa")]
    [InlineData("PROD")]
    [InlineData("")]
    public async Task NonDevPrincipalCannotReadSnapshot(string environment)
    {
        var source = new Sources { Principal = Caller() with { Environment = environment } };
        Assert.Equal("AUTHENTICATED_DEV_REVIEWER_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        Assert.Equal(0, source.SnapshotReads);
    }

    [Fact]
    public async Task RevokedMembershipOrUnverifiedCallerIsDeniedBeforeRead()
    {
        var source = new Sources { Principal = Caller() with { IsMembershipActive = false } };
        Assert.Equal("AUTHENTICATED_DEV_REVIEWER_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        source.Principal = Caller() with { IsAuthenticated = false };
        Assert.Equal("AUTHENTICATED_DEV_REVIEWER_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        source.Principal = Caller() with { HasReadinessReviewGrant = false };
        Assert.Equal("AUTHENTICATED_DEV_REVIEWER_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        Assert.Equal(0, source.SnapshotReads);
    }

    [Fact]
    public async Task CrossTenantOrUnauthorizedReviewerFailsBeforeSnapshotRead()
    {
        var source = new Sources { Principal = Caller() with { TenantScope = "tenant-B" } };
        Assert.Equal("TRUSTED_RELEASE_GATE_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        source.Principal = Caller() with { TaskId = "other" };
        Assert.Equal("TRUSTED_RELEASE_GATE_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        Assert.Equal(0, source.SnapshotReads);
    }

    [Fact]
    public async Task ForgedOrMissingSnapshotTenantAndShaNeverPass()
    {
        var source = new Sources { SnapshotValue = Snapshot() with { TenantScope = "tenant-B" } };
        Assert.Equal("SNAPSHOT_TENANT_OR_SOURCE_MISMATCH",
            (await Service(source).InspectAsync(Request())).Code);
        source.SnapshotValue = Snapshot() with { CommitSha = OtherSha };
        Assert.Equal("SNAPSHOT_TENANT_OR_SOURCE_MISMATCH",
            (await Service(source).InspectAsync(Request())).Code);
        source.SnapshotValue = Snapshot() with { ReleaseId = "another-release" };
        Assert.Equal("SNAPSHOT_TENANT_OR_SOURCE_MISMATCH",
            (await Service(source).InspectAsync(Request())).Code);
        source.SnapshotValue = null;
        Assert.Equal("SNAPSHOT_TENANT_OR_SOURCE_MISMATCH",
            (await Service(source).InspectAsync(Request())).Code);
    }

    [Fact]
    public async Task MutableOrUnauditedSnapshotCannotBeAccepted()
    {
        var source = new Sources { SnapshotValue = Snapshot() with { IsImmutable = false } };
        Assert.Equal("IMMUTABLE_AUDITED_ROLLBACK_EVIDENCE_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        source.SnapshotValue = Snapshot() with { TenantOwnershipVerified = false };
        Assert.Equal("IMMUTABLE_AUDITED_ROLLBACK_EVIDENCE_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        source.SnapshotValue = Snapshot() with { AuditChainVerified = false };
        Assert.Equal("IMMUTABLE_AUDITED_ROLLBACK_EVIDENCE_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        source.SnapshotValue = Snapshot() with { ReadOnlyRollbackPlanVerified = false };
        Assert.Equal("IMMUTABLE_AUDITED_ROLLBACK_EVIDENCE_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
    }

    [Fact]
    public async Task InvalidDigestVersionOrRollbackFailsClosed()
    {
        var source = new Sources { SnapshotValue = Snapshot() with { EvidenceVersion = 0 } };
        Assert.Equal("IMMUTABLE_AUDITED_ROLLBACK_EVIDENCE_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        source.SnapshotValue = Snapshot() with { ArtifactDigest = "sha256:bad" };
        Assert.Equal("IMMUTABLE_AUDITED_ROLLBACK_EVIDENCE_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        source.SnapshotValue = Snapshot() with { RollbackReference = "\r\nsecret" };
        Assert.Equal("IMMUTABLE_AUDITED_ROLLBACK_EVIDENCE_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
    }

    [Fact]
    public async Task UnsafeReleaseAndShaAreDeniedBeforeAnySnapshotRead()
    {
        var source = new Sources();
        Assert.Equal("INVALID_RELEASE_OR_SHA",
            (await Service(source).InspectAsync(null)).Code);
        Assert.Equal("INVALID_RELEASE_OR_SHA",
            (await Service(source).InspectAsync(Request() with { ReleaseId = "../escape" })).Code);
        Assert.Equal("INVALID_RELEASE_OR_SHA",
            (await Service(source).InspectAsync(Request() with { ExpectedCommitSha = OtherSha.ToUpperInvariant() })).Code);
        Assert.Equal(0, source.SnapshotReads);
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("skipped")]
    [InlineData("failure")]
    public async Task NonPassCIAndOwnA5NeverPermitSnapshotReview(string result)
    {
        var source = new Sources { CandidateValue = Candidate() with {
            Checks = new[] {
                new Rules.CheckResult("scope-lock", Sha, "completed", result),
                new Rules.CheckResult("backend-tests", Sha, "completed", "success")
            }
        } };
        Assert.Equal("TRUSTED_RELEASE_GATE_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        source.CandidateValue = Candidate() with { AuthorTaskId = "reviewer" };
        Assert.Equal("TRUSTED_RELEASE_GATE_REQUIRED",
            (await Service(source).InspectAsync(Request())).Code);
        Assert.Equal(0, source.SnapshotReads);
    }

    [Fact]
    public async Task CancellationStopsBeforeExternalReads()
    {
        var source = new Sources();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await Service(source).InspectAsync(Request(), cts.Token));
        Assert.Equal(0, source.SnapshotReads);
    }

    [Fact]
    public void MissingTrustedDependenciesAreRejected()
    {
        var source = new Sources();
        var gate = new V5ReleaseReadinessAuthorizationGate(source, source);
        Assert.Throws<ArgumentNullException>(() => new Adapter(null!, gate, source));
        Assert.Throws<ArgumentNullException>(() => new Adapter(source, null!, source));
        Assert.Throws<ArgumentNullException>(() => new Adapter(source, gate, null!));
    }
}
