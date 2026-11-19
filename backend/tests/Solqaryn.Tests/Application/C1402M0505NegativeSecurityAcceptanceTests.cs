using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C14-02.M05.05: negative xUnit acceptance of real Application/Domain preflight.
/// Fixture evidence is NOT signed JWT HTTP, persisted MySQL or production certification.
/// </summary>
public sealed class C1402M0505NegativeSecurityAcceptanceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Key = "security-acceptance-resource";
    private static V5SecurityAcceptanceDomainRules.VerifiedAuthority Authority() =>
        new(101, 11, 2, Sha, true, true, true, true, true, false, false);
    private static V5SecurityAcceptanceDomainRules.PersistedTarget Target() =>
        new(11, 2, 11, true, true, false, false);
    private static V5SecurityAcceptanceDomainRules.Attempt Attempt(V5SecurityAcceptanceDomainRules.Surface s) =>
        new(s, null, Sha, false, false, false);
    private static C1402SecurityAcceptanceAuthorizationGate.Request Request(
        V5SecurityAcceptanceDomainRules.Surface s = V5SecurityAcceptanceDomainRules.Surface.BusinessRead) =>
        new(Key, Sha, s);
    private static C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence Evidence(
        V5SecurityAcceptanceDomainRules.Surface s = V5SecurityAcceptanceDomainRules.Surface.BusinessRead) =>
        new(Key, Sha, true, true, true, true, true, "git-proof:negative-acceptance",
            Authority(), Target(), Attempt(s));

    private sealed class Reader(C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence evidence,
        bool fails = false) : IC1402SecurityAcceptanceEvidenceReader
    {
        public int Calls { get; private set; }
        public ValueTask<C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence?> ReadAsync(
            string resourceKey, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Calls++;
            if (fails) throw new InvalidOperationException("private-provider-value");
            return ValueTask.FromResult<C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence?>(evidence);
        }
    }

    private static void Denied(C1402SecurityAcceptanceAuthorizationGate.Decision v, string code)
    {
        Assert.Equal(code, v.Code);
        Assert.False(v.RequiresSeparateAuthenticatedAdapter);
        Assert.False(v.GrantsTenantOrResourceAccess);
        Assert.False(v.AllowsInstitutionalWrite);
        Assert.False(v.AllowsRestoreOrDeployment);
        Assert.False(v.CertifiesSecurityAcceptance);
    }

    private static async Task<C1402SecurityAcceptanceAuthorizationGate.Decision> Check(
        C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence evidence,
        V5SecurityAcceptanceDomainRules.Surface surface)
    {
        var reader = new Reader(evidence);
        var decision = await new C1402SecurityAcceptanceAuthorizationGate(reader)
            .CheckAsync(Request(surface));
        Assert.Equal(1, reader.Calls);
        return decision;
    }

    [Theory]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.BusinessRead)]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.BusinessWrite)]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.BranchRead)]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.HostResolve)]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.FileDownload)]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.BackgroundJob)]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.CacheRead)]
    public async Task ForeignTenantBolaIdorDeniedAcrossSevenSurfaces(
        V5SecurityAcceptanceDomainRules.Surface surface) =>
        Denied(await Check(Evidence(surface) with { Target = Target() with { OwnerTenantId = 22 } },
            surface), "CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED");

    [Fact]
    public async Task TenantHeaderSpoofingAndEnumerationNeverYieldAuthority()
    {
        var attempt = Attempt(V5SecurityAcceptanceDomainRules.Surface.BusinessRead);
        foreach (var abuse in new[]
        {
            attempt with { ClientTenantOverride = 22 },
            attempt with { RequestsTenantFilterBypass = true },
            attempt with { RequestsCrossTenantEnumeration = true }
        })
        {
            var v = await Check(Evidence() with { Attempt = abuse },
                V5SecurityAcceptanceDomainRules.Surface.BusinessRead);
            Assert.False(v.RequiresSeparateAuthenticatedAdapter);
            Assert.False(v.GrantsTenantOrResourceAccess);
            Assert.False(v.CertifiesSecurityAcceptance);
        }
    }

    [Theory]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.BranchRead)]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.BusinessWrite)]
    public async Task TenantMatchDoesNotBypassForeignBranch(
        V5SecurityAcceptanceDomainRules.Surface surface) =>
        Denied(await Check(Evidence(surface) with { Target = Target() with { OwnerBranchId = 99 } },
            surface), "BRANCH_OWNER_MISMATCH");

    [Fact]
    public async Task CrossTenantHostCannotResolve() =>
        Denied(await Check(Evidence(V5SecurityAcceptanceDomainRules.Surface.HostResolve) with
        { Target = Target() with { BoundHostTenantId = 22 } },
        V5SecurityAcceptanceDomainRules.Surface.HostResolve), "UNTRUSTED_OR_CROSS_TENANT_HOST");

    [Fact]
    public async Task ExpiredFileLinkCannotAuthorizeDownload() =>
        Denied(await Check(Evidence(V5SecurityAcceptanceDomainRules.Surface.FileDownload) with
        { Target = Target() with { SignedLinkExpired = true } },
        V5SecurityAcceptanceDomainRules.Surface.FileDownload), "FILE_LINK_EXPIRED");

    [Fact]
    public async Task JobsRequireServerGrantAndAntiReplayIdempotency()
    {
        const V5SecurityAcceptanceDomainRules.Surface s = V5SecurityAcceptanceDomainRules.Surface.BackgroundJob;
        Denied(await Check(Evidence(s), s), "JOB_AUTHORITY_OR_IDEMPOTENCY_REQUIRED");
        Denied(await Check(Evidence(s) with
        { Authority = Authority() with { ServiceActorVerified = true, ServiceGrantVerified = true } },
            s), "JOB_AUTHORITY_OR_IDEMPOTENCY_REQUIRED");
    }

    [Fact]
    public async Task RevokedSessionMembershipRbacCacheAndAuditFailClosed()
    {
        foreach (var evidence in new[]
        {
            Evidence() with { CurrentJwtVerified = false },
            Evidence() with { CurrentMembershipVerified = false },
            Evidence() with { CurrentExplicitRbacVerified = false },
            Evidence() with { CurrentAuditFenceVerified = false },
            Evidence() with { Target = Target() with { AccessRevoked = true } }
        })
            Denied(await Check(evidence, V5SecurityAcceptanceDomainRules.Surface.BusinessRead),
                "CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED");
    }

    [Fact]
    public async Task ClientOverrideAndBackupRestoreAreDeniedBeforeProviderRead()
    {
        var reader = new Reader(Evidence());
        var gate = new C1402SecurityAcceptanceAuthorizationGate(reader);
        Denied(await gate.CheckAsync(Request() with { ClientControlsTenantOrRole = true }),
            "CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED");
        Denied(await gate.CheckAsync(Request() with { RequestsBypassOrRestore = true }),
            "CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED");
        Denied(await gate.CheckAsync(Request(V5SecurityAcceptanceDomainRules.Surface.BackupRestore)),
            "CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED");
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task TraversalAndMalformedShaAreDeniedBeforeProviderRead()
    {
        var reader = new Reader(Evidence());
        var gate = new C1402SecurityAcceptanceAuthorizationGate(reader);
        Denied(await gate.CheckAsync(Request() with { ResourceKey = "../tenant-B/resource" }),
            "INVALID_REQUEST_OR_SHA");
        Denied(await gate.CheckAsync(Request() with { ExpectedCommitSha = "abc" }),
            "INVALID_REQUEST_OR_SHA");
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task StaleShaAndForgedResourceCannotLaunderEvidence()
    {
        Denied(await Check(Evidence() with { ObservedSourceSha = new string('a', 40) },
            V5SecurityAcceptanceDomainRules.Surface.BusinessRead),
            "PERSISTED_RESOURCE_AND_SOURCE_REQUIRED");
        Denied(await Check(Evidence() with { PersistedResourceKey = "other-tenant" },
            V5SecurityAcceptanceDomainRules.Surface.BusinessRead),
            "PERSISTED_RESOURCE_AND_SOURCE_REQUIRED");
    }

    [Fact]
    public async Task ProviderFailureReturnsFixedErrorWithoutLeakingDetail()
    {
        var reader = new Reader(Evidence(), fails: true);
        var decision = await new C1402SecurityAcceptanceAuthorizationGate(reader).CheckAsync(Request());
        Denied(decision, "TRUSTED_EVIDENCE_SOURCE_UNAVAILABLE");
        Assert.DoesNotContain("private-provider", decision.Code);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task CancelledRequestDoesNotAcquireAuthorization()
    {
        var reader = new Reader(Evidence());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new C1402SecurityAcceptanceAuthorizationGate(reader)
                .CheckAsync(Request(), cts.Token));
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task EvenNominalEvidenceRequiresIndependentAuthenticatedAdapter()
    {
        var decision = await Check(Evidence(), V5SecurityAcceptanceDomainRules.Surface.BusinessRead);
        Assert.Equal("AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED", decision.Code);
        Assert.True(decision.RequiresSeparateAuthenticatedAdapter);
        Assert.False(decision.GrantsTenantOrResourceAccess);
        Assert.False(decision.CertifiesSecurityAcceptance);
    }
}
