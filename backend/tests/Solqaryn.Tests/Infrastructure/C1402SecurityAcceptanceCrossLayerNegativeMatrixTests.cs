using Gate = Solqaryn.Application.AutomationV5.C1402SecurityAcceptanceAuthorizationGate;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

/// <summary>
/// C14-02.M03.05: executable negative acceptance matrix across actual
/// Domain, Application and isolated Infrastructure classes.
/// In-memory evidence cannot certify JWT HTTP, EF/MySQL or provider security.
/// </summary>
public sealed class C1402SecurityAcceptanceCrossLayerNegativeMatrixTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string ResourceKey = "security-acceptance-101";
    private static V5SecurityAcceptanceDomainRules.VerifiedAuthority Authority() =>
        new(10, 100, 7, Sha, true, true, true, true, true, false, false);
    private static V5SecurityAcceptanceDomainRules.PersistedTarget Target() =>
        new(100, 7, 100, true, true, false, false);
    private static V5SecurityAcceptanceDomainRules.Attempt Attempt(
        V5SecurityAcceptanceDomainRules.Surface surface = V5SecurityAcceptanceDomainRules.Surface.BusinessRead) =>
        new(surface, null, Sha, false, false, false);
    private static Gate.VerifiedEvidence Evidence(
        V5SecurityAcceptanceDomainRules.Surface surface = V5SecurityAcceptanceDomainRules.Surface.BusinessRead) =>
        new(ResourceKey, Sha, true, true, true, true, true,
            "git-proof:security-test", Authority(), Target(), Attempt(surface));
    private static Gate.Request Request(
        V5SecurityAcceptanceDomainRules.Surface surface = V5SecurityAcceptanceDomainRules.Surface.BusinessRead) =>
        new(ResourceKey, Sha, surface);
    private static C1402SecurityAcceptanceIsolatedSnapshotAdapter Adapter(
        Gate.VerifiedEvidence? evidence = null) =>
        new(ResourceKey, 100, 10, Sha, evidence ?? Evidence());
    private static Task<Gate.Decision> Check(
        Gate.VerifiedEvidence evidence, Gate.Request? request = null) =>
        Adapter(evidence).EvaluateReadOnlyAsync(
            request ?? Request(), ResourceKey, 100, 10, Sha).AsTask();

    private static void NoGrant(Gate.Decision decision)
    {
        Assert.False(decision.GrantsTenantOrResourceAccess);
        Assert.False(decision.AllowsInstitutionalWrite);
        Assert.False(decision.AllowsRestoreOrDeployment);
        Assert.False(decision.CertifiesSecurityAcceptance);
    }
    private static void Denied(Gate.Decision decision)
    {
        Assert.False(decision.RequiresSeparateAuthenticatedAdapter);
        NoGrant(decision);
    }

    [Fact]
    public async Task PositiveDomainApplicationAndInfrastructurePreflightNeverAuthorizes()
    {
        var domain = V5SecurityAcceptanceDomainRules.Evaluate(
            Authority(), Target(), Attempt());
        Assert.True(domain.RequiresTrustedIntegration);
        Assert.False(domain.AuthorizesResourceAccess);
        Assert.False(domain.AuthorizesRelease);
        var app = new V5SecurityAcceptanceApplicationService().Prepare(
            new V5SecurityAcceptanceApplicationService.Request(
                Sha, Sha, "git-proof:security-test", Authority(), Target(), Attempt()));
        Assert.True(app.NeedsTrustedAdapter);
        Assert.False(app.AllowsResourceAccess);
        Assert.False(app.AllowsInstitutionalWrite);
        var decision = await Check(Evidence());
        Assert.Equal("AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED", decision.Code);
        Assert.True(decision.RequiresSeparateAuthenticatedAdapter);
        NoGrant(decision);
        Assert.False(Adapter().IsLiveProvider);
        Assert.False(Adapter().CertifiesJwtHttpMySqlOrPentest);
    }

    [Fact]
    public async Task BolaIdorTenantAndCapturedOwnerCannotCrossTenantBoundary()
    {
        Assert.Equal("BOLA_RESOURCE_TENANT_MISMATCH",
            V5SecurityAcceptanceDomainRules.Evaluate(
                Authority(), Target() with { OwnerTenantId = 200 }, Attempt()).Code);
        Denied(await Check(Evidence() with
        { Target = Target() with { OwnerTenantId = 200 } }));
        Denied(await Check(Evidence() with
        { Authority = Authority() with { ServerTenantId = 200 } }));
        Denied(await Adapter().EvaluateReadOnlyAsync(
            Request(), ResourceKey, 200, 10, Sha));
    }

    [Fact]
    public async Task ActorAndResourceSpoofingAreDeniedBeforeDomainReview()
    {
        Denied(await Adapter().EvaluateReadOnlyAsync(
            Request(), ResourceKey, 100, 11, Sha));
        Denied(await Adapter().EvaluateReadOnlyAsync(
            Request(), "security-acceptance-102", 100, 10, Sha));
        Denied(await Check(Evidence() with { PersistedResourceKey = "other-resource" }));
        Denied(await Check(Evidence() with
        { Authority = Authority() with { ActorId = 11 } }));
    }

    [Fact]
    public async Task ClientTenantOverrideAndFilterBypassNeverProducePermission()
    {
        foreach (var attempt in new[]
        {
            Attempt() with { ClientTenantOverride = 200 },
            Attempt() with { RequestsTenantFilterBypass = true },
            Attempt() with { RequestsCrossTenantEnumeration = true }
        })
        {
            var result = await Check(Evidence() with { Attempt = attempt });
            Denied(result);
        }
        Assert.Equal("TENANT_FILTER_OR_ENUMERATION_BYPASS_DENIED",
            V5SecurityAcceptanceDomainRules.Evaluate(
                Authority(), Target(), Attempt() with { RequestsTenantFilterBypass = true }).Code);
    }

    [Fact]
    public async Task HostSpoofingAndUntrustedHostBindingAreRejected()
    {
        var host = V5SecurityAcceptanceDomainRules.Surface.HostResolve;
        foreach (var target in new[]
        {
            Target() with { BoundHostTenantId = 200 },
            Target() with { HostBindingVerified = false }
        })
        {
            var result = await Check(Evidence(host) with { Target = target }, Request(host));
            Assert.Equal("UNTRUSTED_OR_CROSS_TENANT_HOST", result.Code);
            NoGrant(result);
        }
    }

    [Fact]
    public async Task BranchReadAndBusinessWriteRespectCurrentBranchOwnership()
    {
        foreach (var surface in new[]
        {
            V5SecurityAcceptanceDomainRules.Surface.BranchRead,
            V5SecurityAcceptanceDomainRules.Surface.BusinessWrite
        })
        {
            var result = await Check(Evidence(surface) with
            { Target = Target() with { OwnerBranchId = 8 } }, Request(surface));
            Assert.Equal("BRANCH_OWNER_MISMATCH", result.Code);
            Denied(result);
        }
    }

    [Fact]
    public async Task FileDownloadIsNeverGrantedWhenLinkExpiredOrOwnershipRevoked()
    {
        var file = V5SecurityAcceptanceDomainRules.Surface.FileDownload;
        Assert.Equal("FILE_LINK_EXPIRED",
            (await Check(Evidence(file) with
            { Target = Target() with { SignedLinkExpired = true } }, Request(file))).Code);
        Assert.Equal("RESOURCE_OR_PERMISSION_REVOKED",
            (await Check(Evidence(file) with
            { Target = Target() with { AccessRevoked = true } }, Request(file))).Code);
    }

    [Fact]
    public async Task JobsRequireServiceActorGrantsAndIdempotencyEvidence()
    {
        var job = V5SecurityAcceptanceDomainRules.Surface.BackgroundJob;
        Denied(await Check(Evidence(job), Request(job)));
        var privileged = Evidence(job) with
        {
            Authority = Authority() with
            { ServiceActorVerified = true, ServiceGrantVerified = true }
        };
        Denied(await Check(privileged, Request(job)));
        var ready = await Check(privileged with
        { Attempt = Attempt(job) with { IdempotencyEvidenceVerified = true } }, Request(job));
        Assert.True(ready.RequiresSeparateAuthenticatedAdapter);
        NoGrant(ready);
    }

    [Fact]
    public async Task CacheReadWithRevocationOrMissingOwnershipDenies()
    {
        var cache = V5SecurityAcceptanceDomainRules.Surface.CacheRead;
        Denied(await Check(Evidence(cache) with
        { Target = Target() with { AccessRevoked = true } }, Request(cache)));
        Denied(await Check(Evidence(cache) with
        { Target = Target() with { OwnershipVerifiedFromStore = false } }, Request(cache)));
    }

    [Fact]
    public async Task BackupRestoreCannotBeAuthorizedByPreflightOrClientOverride()
    {
        var restore = V5SecurityAcceptanceDomainRules.Surface.BackupRestore;
        Assert.Equal("SEPARATE_SANDBOX_RESTORE_AUTHORIZATION_REQUIRED",
            V5SecurityAcceptanceDomainRules.Evaluate(Authority(), Target(), Attempt(restore)).Code);
        var result = await Check(Evidence(restore), Request(restore));
        Assert.Equal("CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED", result.Code);
        Denied(result);
        Denied(await Check(Evidence(), Request() with { RequestsBypassOrRestore = true }));
    }

    [Fact]
    public async Task RevokedSessionRbacAndExpiredAuthorityFailClosed()
    {
        foreach (var evidence in new[]
        {
            Evidence() with { CurrentJwtVerified = false },
            Evidence() with { CurrentMembershipVerified = false },
            Evidence() with { CurrentExplicitRbacVerified = false },
            Evidence() with { CurrentAuditFenceVerified = false },
            Evidence() with { Authority = Authority() with { PermissionSnapshotCurrent = false } },
            Evidence() with { Authority = Authority() with { ActiveMembershipVerified = false } }
        })
            Denied(await Check(evidence));
    }

    [Fact]
    public async Task UnsafeProofAndStaleSourceShaNeverCertifySecurityAcceptance()
    {
        Denied(await Check(Evidence() with
        { SanitizedProofReference = "https://secrets.example/private" }));
        Denied(await Check(Evidence() with { ObservedSourceSha = new string('b', 40) }));
        Denied(await Adapter().EvaluateReadOnlyAsync(
            Request(), ResourceKey, 100, 10, new string('a', 40)));
    }

    [Fact]
    public async Task UnknownSurfaceAndClientPrivilegeClaimsAreDenied()
    {
        var unknown = (V5SecurityAcceptanceDomainRules.Surface)999;
        Denied(await Check(Evidence(), Request(unknown)));
        Denied(await Check(Evidence(), Request() with { ClientControlsTenantOrRole = true }));
        Assert.False(V5SecurityAcceptanceDomainRules.Evaluate(
            Authority(), Target(), Attempt(unknown)).RequiresTrustedIntegration);
    }

    [Fact]
    public async Task CancellationAndNullRequestCannotGrantAccess()
    {
        Denied(await Adapter().EvaluateReadOnlyAsync(
            null, ResourceKey, 100, 10, Sha));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Adapter().EvaluateReadOnlyAsync(
                Request(), ResourceKey, 100, 10, Sha, cts.Token));
        Assert.False(Adapter().GrantsInstitutionalWrite);
    }
}
