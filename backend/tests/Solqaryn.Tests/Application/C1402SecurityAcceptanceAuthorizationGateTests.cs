using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C1402SecurityAcceptanceAuthorizationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static V5SecurityAcceptanceDomainRules.VerifiedAuthority Authority() =>
        new(10, 100, 7, Sha, true, true, true, true, true, false, false);
    private static V5SecurityAcceptanceDomainRules.PersistedTarget Target() =>
        new(100, 7, 100, true, true, false, false);
    private static V5SecurityAcceptanceDomainRules.Attempt Attempt() =>
        new(V5SecurityAcceptanceDomainRules.Surface.BusinessRead, null, Sha, false, false, false);

    private static C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence Evidence() =>
        new("resource-A", Sha, true, true, true, true, true,
            "git-proof:dev-tested", Authority(), Target(), Attempt());

    private static C1402SecurityAcceptanceAuthorizationGate.Request Request() =>
        new("resource-A", Sha, V5SecurityAcceptanceDomainRules.Surface.BusinessRead);

    private sealed class Reader(C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence? evidence)
        : IC1402SecurityAcceptanceEvidenceReader
    {
        public int Calls { get; private set; }
        public ValueTask<C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence?> ReadAsync(
            string resourceKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(evidence);
        }
    }

    private static async Task<C1402SecurityAcceptanceAuthorizationGate.Decision> Check(
        C1402SecurityAcceptanceAuthorizationGate.Request? request = null,
        C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence? evidence = null)
    {
        var gate = new C1402SecurityAcceptanceAuthorizationGate(
            new Reader(evidence ?? Evidence()));
        return await gate.CheckAsync(request ?? Request());
    }

    [Fact]
    public async Task ValidPreflightNeverAuthorizesReadReleaseOrWrite()
    {
        var result = await Check();
        Assert.Equal("AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresSeparateAuthenticatedAdapter);
        Assert.False(result.GrantsTenantOrResourceAccess);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.AllowsRestoreOrDeployment);
        Assert.False(result.CertifiesSecurityAcceptance);
    }

    [Fact]
    public async Task InvalidShaAndResourceFailBeforeReading()
    {
        var reader = new Reader(Evidence());
        var gate = new C1402SecurityAcceptanceAuthorizationGate(reader);
        Assert.Equal("INVALID_REQUEST_OR_SHA", (await gate.CheckAsync(null)).Code);
        Assert.Equal("INVALID_REQUEST_OR_SHA",
            (await gate.CheckAsync(Request() with { ExpectedCommitSha = "short" })).Code);
        Assert.Equal("INVALID_REQUEST_OR_SHA",
            (await gate.CheckAsync(Request() with { ResourceKey = "resource/../B" })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ClientAuthorityAndRestoreOverrideNeverReachReader()
    {
        var reader = new Reader(Evidence());
        var gate = new C1402SecurityAcceptanceAuthorizationGate(reader);
        Assert.Equal("CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED",
            (await gate.CheckAsync(Request() with { ClientControlsTenantOrRole = true })).Code);
        Assert.Equal("CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED",
            (await gate.CheckAsync(Request() with { RequestsBypassOrRestore = true })).Code);
        Assert.Equal("CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED",
            (await gate.CheckAsync(Request() with
            { Surface = V5SecurityAcceptanceDomainRules.Surface.BackupRestore })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task UnknownSurfaceFailsClosed() =>
        Assert.Equal("INVALID_REQUEST_OR_SHA",
            (await Check(Request() with
            { Surface = (V5SecurityAcceptanceDomainRules.Surface)999 })).Code);

    [Fact]
    public async Task ForeignResourceAndOldShaRejected()
    {
        Assert.Equal("PERSISTED_RESOURCE_AND_SOURCE_REQUIRED",
            (await Check(evidence: Evidence() with { PersistedResourceKey = "resource-B" })).Code);
        Assert.Equal("PERSISTED_RESOURCE_AND_SOURCE_REQUIRED",
            (await Check(evidence: Evidence() with { ObservedSourceSha = new string('f', 40) })).Code);
    }

    [Fact]
    public async Task CurrentJwtMembershipAndRbacAreMandatory()
    {
        Assert.Equal("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED",
            (await Check(evidence: Evidence() with { CurrentJwtVerified = false })).Code);
        Assert.Equal("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED",
            (await Check(evidence: Evidence() with { CurrentMembershipVerified = false })).Code);
        Assert.Equal("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED",
            (await Check(evidence: Evidence() with { CurrentExplicitRbacVerified = false })).Code);
        Assert.Equal("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED",
            (await Check(evidence: Evidence() with { CurrentAuditFenceVerified = false })).Code);
    }

    [Fact]
    public async Task ForeignTenantAndRevokedTargetAreDenied()
    {
        Assert.Equal("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED",
            (await Check(evidence: Evidence() with { Target = Target() with { OwnerTenantId = 200 } })).Code);
        Assert.Equal("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED",
            (await Check(evidence: Evidence() with { Target = Target() with { AccessRevoked = true } })).Code);
    }

    [Fact]
    public async Task MismatchedOperationCannotUseEvidenceForOtherSurface() =>
        Assert.Equal("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED",
            (await Check(evidence: Evidence() with
            { Attempt = Attempt() with { Surface = V5SecurityAcceptanceDomainRules.Surface.FileDownload } })).Code);

    [Fact]
    public async Task DomainStillRejectsCrossTenantAndSpoofedClaims()
    {
        Assert.Equal("CLIENT_TENANT_SPOOFING_DENIED",
            (await Check(evidence: Evidence() with
            { Attempt = Attempt() with { ClientTenantOverride = 200 } })).Code);
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            (await Check(evidence: Evidence() with
            { Authority = Authority() with { PermissionSnapshotCurrent = false } })).Code);
    }

    [Fact]
    public async Task UnsafeEvidenceIsRejectedByApplication()
    {
        Assert.Equal("SECURITY_ACCEPTANCE_PROOF_REFERENCE_REQUIRED",
            (await Check(evidence: Evidence() with { SanitizedProofReference = "https://secret" })).Code);
    }

    [Fact]
    public async Task NullReaderSnapshotFailsClosed()
    {
        var gate = new C1402SecurityAcceptanceAuthorizationGate(new Reader(null));
        Assert.Equal("PERSISTED_RESOURCE_AND_SOURCE_REQUIRED",
            (await gate.CheckAsync(Request())).Code);
    }

    [Fact]
    public async Task CancellationIsPropagatedWithoutUsingEvidenceReader()
    {
        var reader = new Reader(Evidence());
        var gate = new C1402SecurityAcceptanceAuthorizationGate(reader);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await gate.CheckAsync(Request(), cts.Token));
        Assert.Equal(0, reader.Calls);
    }
}
