using Gate = Solqaryn.Application.AutomationV5.C1402SecurityAcceptanceAuthorizationGate;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1402SecurityAcceptanceIsolatedSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Resource = "resource-A";
    private static V5SecurityAcceptanceDomainRules.VerifiedAuthority Authority() =>
        new(10, 100, 7, Sha, true, true, true, true, true, false, false);
    private static V5SecurityAcceptanceDomainRules.PersistedTarget Target() =>
        new(100, 7, 100, true, true, false, false);
    private static V5SecurityAcceptanceDomainRules.Attempt Attempt() =>
        new(V5SecurityAcceptanceDomainRules.Surface.BusinessRead, null, Sha, false, false, false);
    private static Gate.VerifiedEvidence Evidence() =>
        new(Resource, Sha, true, true, true, true, true,
            "git-proof:dev-tested", Authority(), Target(), Attempt());
    private static Gate.Request Request() =>
        new(Resource, Sha, V5SecurityAcceptanceDomainRules.Surface.BusinessRead);
    private static C1402SecurityAcceptanceIsolatedSnapshotAdapter Adapter(
        Gate.VerifiedEvidence? evidence = null) =>
        new(Resource, 100, 10, Sha, evidence ?? Evidence());
    private static async Task<Gate.Decision> Check(
        C1402SecurityAcceptanceIsolatedSnapshotAdapter adapter,
        Gate.Request? request = null,
        string? observedResource = Resource,
        int observedTenant = 100, int observedActor = 10,
        string? observedSha = Sha) =>
        await adapter.EvaluateReadOnlyAsync(
            request ?? Request(), observedResource, observedTenant, observedActor, observedSha);

    [Fact]
    public async Task NominalIsOnlyPreflightAndNeverGrantsResourceAccess()
    {
        var adapter = Adapter();
        var result = await Check(adapter);
        Assert.Equal("AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresSeparateAuthenticatedAdapter);
        Assert.False(result.GrantsTenantOrResourceAccess);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.AllowsRestoreOrDeployment);
        Assert.False(result.CertifiesSecurityAcceptance);
        Assert.False(adapter.IsLiveProvider);
        Assert.False(adapter.GrantsTenantOrResourceAccess);
        Assert.False(adapter.GrantsInstitutionalWrite);
        Assert.False(adapter.GrantsFileDownload);
        Assert.False(adapter.GrantsJobExecution);
        Assert.False(adapter.GrantsRestoreOrDeployment);
        Assert.False(adapter.AllowsTenantFilterBypass);
        Assert.False(adapter.CertifiesJwtHttpMySqlOrPentest);
    }

    [Fact]
    public void ConstructorRejectsInvalidAndDangerousInputs()
    {
        Assert.Throws<ArgumentException>(() => new C1402SecurityAcceptanceIsolatedSnapshotAdapter(
            "../bad", 100, 10, Sha, Evidence()));
        Assert.Throws<ArgumentException>(() => new C1402SecurityAcceptanceIsolatedSnapshotAdapter(
            Resource, 100, 10, "short", Evidence()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new C1402SecurityAcceptanceIsolatedSnapshotAdapter(
            Resource, 0, 10, Sha, Evidence()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new C1402SecurityAcceptanceIsolatedSnapshotAdapter(
            Resource, 100, 0, Sha, Evidence()));
        Assert.Throws<ArgumentNullException>(() => new C1402SecurityAcceptanceIsolatedSnapshotAdapter(
            Resource, 100, 10, Sha, null!));
    }

    [Fact]
    public async Task NullRequestAlwaysDenies()
    {
        var verdict = await Adapter().EvaluateReadOnlyAsync(null, Resource, 100, 10, Sha);
        Assert.Equal("ISOLATED_SECURITY_REQUEST_REQUIRED", verdict.Code);
    }

    [Fact]
    public async Task ObservedAndRequestedResourceMustMatchCapturedResource()
    {
        Assert.Equal("ISOLATED_SECURITY_RESOURCE_MISMATCH",
            (await Check(Adapter(), observedResource: "resource-B")).Code);
        Assert.Equal("ISOLATED_SECURITY_RESOURCE_MISMATCH",
            (await Check(Adapter(), request: Request() with { ResourceKey = "resource-B" })).Code);
    }

    [Fact]
    public async Task TenantBAndDifferentActorAreDeniedBeforeDomainEvaluation()
    {
        Assert.Equal("ISOLATED_SECURITY_TENANT_MISMATCH",
            (await Check(Adapter(), observedTenant: 200)).Code);
        Assert.Equal("ISOLATED_SECURITY_ACTOR_MISMATCH",
            (await Check(Adapter(), observedActor: 11)).Code);
    }

    [Fact]
    public async Task SourceShaMustMatchRequestAndObservedSnapshot()
    {
        Assert.Equal("ISOLATED_SECURITY_SHA_MISMATCH",
            (await Check(Adapter(), observedSha: new string('a', 40))).Code);
        Assert.Equal("ISOLATED_SECURITY_SHA_MISMATCH",
            (await Check(Adapter(), request: Request() with { ExpectedCommitSha = new string('f', 40) })).Code);
    }

    [Fact]
    public async Task ContradictoryCapturedResourceAndActorFailClosed()
    {
        Assert.Equal("ISOLATED_CAPTURED_SECURITY_EVIDENCE_MISMATCH",
            (await Check(Adapter(Evidence() with { PersistedResourceKey = "resource-B" }))).Code);
        Assert.Equal("ISOLATED_CAPTURED_SECURITY_EVIDENCE_MISMATCH",
            (await Check(Adapter(Evidence() with { Authority = Authority() with { ActorId = 11 } }))).Code);
        Assert.Equal("ISOLATED_CAPTURED_SECURITY_EVIDENCE_MISMATCH",
            (await Check(Adapter(Evidence() with { Target = Target() with { OwnerTenantId = 200 } }))).Code);
        Assert.Equal("ISOLATED_CAPTURED_SECURITY_EVIDENCE_MISMATCH",
            (await Check(Adapter(Evidence() with { ObservedSourceSha = new string('f', 40) }))).Code);
    }

    [Fact]
    public async Task BrowserAuthorityOverridesAndRestoreCannotPass()
    {
        Assert.Equal("CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED",
            (await Check(Adapter(), Request() with { ClientControlsTenantOrRole = true })).Code);
        Assert.Equal("CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED",
            (await Check(Adapter(), Request() with { RequestsBypassOrRestore = true })).Code);
        Assert.Equal("CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED",
            (await Check(Adapter(), Request() with
                { Surface = V5SecurityAcceptanceDomainRules.Surface.BackupRestore })).Code);
    }

    [Fact]
    public async Task RevokedMembershipAndPermissionCannotPass()
    {
        Assert.Equal("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED",
            (await Check(Adapter(Evidence() with { CurrentMembershipVerified = false }))).Code);
        Assert.Equal("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED",
            (await Check(Adapter(Evidence() with { CurrentJwtVerified = false }))).Code);
        Assert.Equal("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED",
            (await Check(Adapter(Evidence() with { CurrentExplicitRbacVerified = false }))).Code);
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            (await Check(Adapter(Evidence() with
                { Authority = Authority() with { PermissionSnapshotCurrent = false } }))).Code);
    }

    [Fact]
    public async Task AuditFenceAndProofAreMandatory()
    {
        Assert.Equal("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED",
            (await Check(Adapter(Evidence() with { CurrentAuditFenceVerified = false }))).Code);
        Assert.Equal("SECURITY_ACCEPTANCE_PROOF_REFERENCE_REQUIRED",
            (await Check(Adapter(Evidence() with { SanitizedProofReference = "https://secret" }))).Code);
    }

    [Fact]
    public async Task BranchAndHostCrossingCannotBeLaundered()
    {
        var branch = Evidence() with
        {
            Attempt = Attempt() with { Surface = V5SecurityAcceptanceDomainRules.Surface.BranchRead },
            Target = Target() with { OwnerBranchId = 8 }
        };
        Assert.Equal("BRANCH_OWNER_MISMATCH",
            (await Check(Adapter(branch), Request() with
            { Surface = V5SecurityAcceptanceDomainRules.Surface.BranchRead })).Code);
        var host = Evidence() with
        {
            Attempt = Attempt() with { Surface = V5SecurityAcceptanceDomainRules.Surface.HostResolve },
            Target = Target() with { BoundHostTenantId = 200 }
        };
        Assert.Equal("UNTRUSTED_OR_CROSS_TENANT_HOST",
            (await Check(Adapter(host), Request() with
            { Surface = V5SecurityAcceptanceDomainRules.Surface.HostResolve })).Code);
    }

    [Fact]
    public async Task InvalidSignedFileAndServiceJobEvidenceDenied()
    {
        var file = Evidence() with
        {
            Attempt = Attempt() with { Surface = V5SecurityAcceptanceDomainRules.Surface.FileDownload },
            Target = Target() with { SignedLinkExpired = true }
        };
        Assert.Equal("FILE_LINK_EXPIRED",
            (await Check(Adapter(file), Request() with
            { Surface = V5SecurityAcceptanceDomainRules.Surface.FileDownload })).Code);
        var job = Evidence() with
        {
            Attempt = Attempt() with { Surface = V5SecurityAcceptanceDomainRules.Surface.BackgroundJob }
        };
        Assert.Equal("JOB_AUTHORITY_OR_IDEMPOTENCY_REQUIRED",
            (await Check(Adapter(job), Request() with
            { Surface = V5SecurityAcceptanceDomainRules.Surface.BackgroundJob })).Code);
    }

    [Fact]
    public async Task CancelledRequestDoesNotReachAnyProvider()
    {
        var adapter = Adapter();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await adapter.EvaluateReadOnlyAsync(Request(), Resource, 100, 10, Sha, cts.Token));
        Assert.False(adapter.GrantsInstitutionalWrite);
    }
}
