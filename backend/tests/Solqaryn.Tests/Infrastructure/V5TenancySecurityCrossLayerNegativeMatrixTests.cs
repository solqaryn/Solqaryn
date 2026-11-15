using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Gate = Solqaryn.Application.AutomationV5.V5TenancySecurityAuthorizationGate;
using Rules = Solqaryn.Domain.AutomationV5.V5TenancySecurityDomainRules;

namespace Solqaryn.Tests.Infrastructure;

/// <summary>
/// C15-03.M03.05: executable negative cross-layer tenancy/security regressions.
/// These are isolated fixtures, not HTTP/JWT/EF/MySQL integration evidence.
/// </summary>
public sealed class V5TenancySecurityCrossLayerNegativeMatrixTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static Rules.TrustedAuthority Authority() =>
        new(7, 11, 3, true, true, false, false);
    private static Rules.PersistedResource Resource() =>
        new(7, 7, 3, true, false, false);
    private static Gate.VerifiedEvidence Evidence() =>
        new(Sha, 101, 7, 11, true, true, true, true, false, Authority(), Resource());
    private static Gate.Request Request(Rules.Surface surface = Rules.Surface.BusinessRead) =>
        new(101, surface, Sha, false, false, false, null);
    private static V5TenancySecurityIsolatedSnapshotAdapter Adapter(Gate.VerifiedEvidence? evidence = null) =>
        new(101, 7, 11, Sha, evidence ?? Evidence());
    private static Task<Gate.Result> Check(Gate.VerifiedEvidence evidence, Gate.Request? request = null) =>
        Adapter(evidence).EvaluateReadOnlyAsync(request ?? Request(), 101, 7, 11, Sha).AsTask();

    private static void NoGrant(Gate.Result result)
    {
        Assert.False(result.GrantsResourceRead);
        Assert.False(result.GrantsFileDownload);
        Assert.False(result.GrantsBackgroundJob);
        Assert.False(result.GrantsControlPlane);
        Assert.False(result.GrantsInstitutionalWrite);
        Assert.False(result.AllowsTenantFilterBypass);
    }
    private static void Denied(Gate.Result result)
    {
        Assert.False(result.NeedsAuthenticatedExecutionAdapter);
        NoGrant(result);
    }

    private sealed class AuthorityReader : IV5TenancySecurityAuthoritySource
    {
        public Rules.TrustedAuthority? Current { get; set; } = Authority();
        public int Calls { get; private set; }
        public ValueTask<Rules.TrustedAuthority?> ReadVerifiedAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(Current);
        }
    }
    private sealed class ResourceReader : IV5TenancySecurityResourceSource
    {
        public Rules.PersistedResource? Current { get; set; } = Resource();
        public int Calls { get; private set; }
        public int? SeenTenant { get; private set; }
        public ValueTask<Rules.PersistedResource?> ReadScopedAsync(int id, int verifiedTenantId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Calls++;
            SeenTenant = verifiedTenantId;
            return ValueTask.FromResult(Current);
        }
    }
    private static V5TenancySecurityApplicationService.Request AppRequest(
        Rules.Surface surface = Rules.Surface.BusinessRead) =>
        new(101, surface, Sha, Sha, "regression:sha-verified", false, false, null);

    [Fact]
    public async Task NominalCrossLayerPreflightNeverGrantsResourceOrTenantBypass()
    {
        var domain = Rules.Evaluate(Authority(), Resource(),
            new Rules.Request(Rules.Surface.BusinessRead, false, null, false));
        Assert.True(domain.RequiresServerEnforcement);
        var source = new AuthorityReader();
        var resources = new ResourceReader();
        var app = await new V5TenancySecurityApplicationService(source, resources)
            .PrepareAsync(AppRequest());
        Assert.True(app.NeedsAuthenticatedAdapter);
        Assert.Equal(7, resources.SeenTenant);
        Assert.False(app.GrantsResourceAccess);
        Assert.False(app.AllowsTenantFilterBypass);
        var isolated = await Check(Evidence());
        Assert.Equal("AUTHENTICATED_EXECUTION_ADAPTER_REQUIRED", isolated.Code);
        Assert.True(isolated.NeedsAuthenticatedExecutionAdapter);
        NoGrant(isolated);
        Assert.False(Adapter().IsLiveEvidenceProvider);
        Assert.False(Adapter().AllowsPersistence);
    }

    [Fact]
    public async Task CrossTenantAndForeignParentAreRejected()
    {
        foreach (var evidence in new[]
        {
            Evidence() with { VerifiedTenantId = 8 },
            Evidence() with { Authority = Authority() with { AuthenticatedTenantId = 8 } },
            Evidence() with { Resource = Resource() with { OwnerTenantId = 8 } },
            Evidence() with { Resource = Resource() with { ParentTenantId = 8 } }
        })
            Denied(await Check(evidence));
        var a = new AuthorityReader();
        var r = new ResourceReader { Current = Resource() with { ParentTenantId = 8 } };
        var app = await new V5TenancySecurityApplicationService(a, r)
            .PrepareAsync(AppRequest());
        Assert.False(app.NeedsAuthenticatedAdapter);
        Assert.Equal(7, r.SeenTenant);
    }

    [Fact]
    public async Task CapturedActorResourceAndObservedIdentifiersCannotBeForged()
    {
        foreach (var evidence in new[]
        {
            Evidence() with { VerifiedActorId = 12 },
            Evidence() with { VerifiedResourceId = 102 },
            Evidence() with { Authority = Authority() with { AuthenticatedActorId = 12 } }
        })
            Denied(await Check(evidence));
        Denied(await Adapter().EvaluateReadOnlyAsync(Request(), 101, 7, 12, Sha));
        Denied(await Adapter().EvaluateReadOnlyAsync(Request(), 102, 7, 11, Sha));
        Denied(await Adapter().EvaluateReadOnlyAsync(
            Request() with { ResourceId = 102 }, 101, 7, 11, Sha));
    }

    [Fact]
    public async Task StaleSourceShaOrUnsafeProofDoNotReachApplicationReaders()
    {
        var a = new AuthorityReader();
        var r = new ResourceReader();
        var service = new V5TenancySecurityApplicationService(a, r);
        Assert.False((await service.PrepareAsync(
            AppRequest() with { ObservedSourceSha = OtherSha })).NeedsAuthenticatedAdapter);
        Assert.False((await service.PrepareAsync(
            AppRequest() with { SanitizedProofReference = "https://invalid.example/token" }))
            .NeedsAuthenticatedAdapter);
        Assert.Equal(0, a.Calls);
        Assert.Equal(0, r.Calls);
        Denied(await Adapter().EvaluateReadOnlyAsync(
            Request() with { ExpectedSourceSha = OtherSha }, 101, 7, 11, Sha));
        Denied(await Adapter().EvaluateReadOnlyAsync(Request(), 101, 7, 11, OtherSha));
    }

    [Fact]
    public async Task RevocationMembershipPermissionAndServerAuthenticationFailClosed()
    {
        foreach (var evidence in new[]
        {
            Evidence() with { PermissionRevoked = true },
            Evidence() with { CurrentMembershipVerified = false },
            Evidence() with { ExplicitPermissionVerified = false },
            Evidence() with { ServerAuthenticationVerified = false },
            Evidence() with { AuthorizationReadFromTrustedBackend = false },
            Evidence() with { Authority = Authority() with { ActiveMembershipVerified = false } },
            Evidence() with { Authority = Authority() with { RequiredRoleVerified = false } }
        })
            Denied(await Check(evidence));
    }

    [Fact]
    public async Task ApplicationRejectsMissingMemberRoleAndTenantBeforePersistence()
    {
        foreach (var actor in new[]
        {
            Authority() with { ActiveMembershipVerified = false },
            Authority() with { RequiredRoleVerified = false },
            Authority() with { AuthenticatedTenantId = null },
            Authority() with { AuthenticatedActorId = null }
        })
        {
            var a = new AuthorityReader { Current = actor };
            var r = new ResourceReader();
            Assert.False((await new V5TenancySecurityApplicationService(a, r)
                .PrepareAsync(AppRequest())).NeedsAuthenticatedAdapter);
            Assert.Equal(0, r.Calls);
        }
    }

    [Fact]
    public async Task ForeignOrMissingBranchCannotPassBranchRead()
    {
        Assert.Equal("BRANCH_SCOPE_MISMATCH",
            (await Check(Evidence() with { Resource = Resource() with { OwnerBranchId = 4 } },
                Request(Rules.Surface.BranchRead))).Code);
        Denied(await Check(
            Evidence() with { Authority = Authority() with { AuthenticatedBranchId = null } },
            Request(Rules.Surface.BranchRead)));
    }

    [Fact]
    public async Task RevokedExpiredOrUnverifiedFilesNeverGrantDownloads()
    {
        foreach (var file in new[]
        {
            Resource() with { AccessRevoked = true },
            Resource() with { SignedLinkExpired = true },
            Resource() with { OwnershipReadFromPersistence = false }
        })
            Denied(await Check(Evidence() with { Resource = file },
                Request(Rules.Surface.FileDownload)));
    }

    [Fact]
    public async Task JobRequiresServiceGrantAuditAndSafeIdempotency()
    {
        var job = Request(Rules.Surface.BackgroundJob);
        Denied(await Check(Evidence(), job));
        var privileged = Evidence() with
        {
            Authority = Authority() with
            {
                PrivilegedServiceGrantVerified = true,
                AuditedServiceContextVerified = true
            }
        };
        Denied(await Check(privileged, job));
        Denied(await Check(privileged, job with { IdempotencyKey = new string('x', 129) }));
        Denied(await Check(privileged, job with { IdempotencyKey = "unsafe\nkey" }));
        var preflight = await Check(privileged, job with { IdempotencyKey = "tenant-7-job-101" });
        Assert.True(preflight.NeedsAuthenticatedExecutionAdapter);
        NoGrant(preflight);
    }

    [Fact]
    public async Task ControlPlaneRequiresAuditedAuthorityAndNeverGrantsAccess()
    {
        var control = Request(Rules.Surface.ControlPlane);
        Denied(await Check(Evidence(), control));
        var privileged = Evidence() with
        {
            Authority = Authority() with
            {
                PrivilegedServiceGrantVerified = true,
                AuditedServiceContextVerified = true
            }
        };
        var preflight = await Check(privileged, control);
        Assert.True(preflight.NeedsAuthenticatedExecutionAdapter);
        NoGrant(preflight);
    }

    [Fact]
    public async Task TenantQueryBypassAndEnumerationNeverReachReaders()
    {
        var a = new AuthorityReader();
        var r = new ResourceReader();
        var service = new V5TenancySecurityApplicationService(a, r);
        foreach (var request in new[]
        {
            AppRequest() with { RequestsTenantFilterBypass = true },
            AppRequest() with { AttemptsCrossTenantEnumeration = true }
        })
        {
            var result = await service.PrepareAsync(request);
            Assert.Equal("TENANT_QUERY_BYPASS_DENIED", result.Code);
            Assert.False(result.NeedsAuthenticatedAdapter);
        }
        Assert.Equal(0, a.Calls);
        Assert.Equal(0, r.Calls);
        Denied(await Check(Evidence(), Request() with { RequestsTenantFilterBypass = true }));
        Denied(await Check(Evidence(), Request() with { AttemptsCrossTenantEnumeration = true }));
    }

    [Fact]
    public async Task MissingPersistedResourceIsNotTreatedAsGlobalScope()
    {
        var r = new ResourceReader { Current = null };
        var service = new V5TenancySecurityApplicationService(new AuthorityReader(), r);
        Assert.False((await service.PrepareAsync(AppRequest())).NeedsAuthenticatedAdapter);
        Assert.Equal(7, r.SeenTenant);
        Denied(await Check(Evidence() with
        { Resource = Resource() with { OwnerTenantId = null } }));
    }

    [Fact]
    public async Task InstitutionalWriteAndUnknownSecuritySurfaceFailClosed()
    {
        Denied(await Check(Evidence(), Request() with { RequestsInstitutionalWrite = true }));
        Denied(await Check(Evidence(), Request((Rules.Surface)999)));
        Assert.False(Rules.Evaluate(Authority(), Resource(),
            new Rules.Request((Rules.Surface)999, false, null, false))
            .RequiresServerEnforcement);
    }

    [Fact]
    public async Task CancellationInterruptsIsolatedAndApplicationReaders()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Adapter().EvaluateReadOnlyAsync(Request(), 101, 7, 11, Sha, cancelled.Token));
        var a = new AuthorityReader();
        var r = new ResourceReader();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new V5TenancySecurityApplicationService(a, r)
                .PrepareAsync(AppRequest(), cancelled.Token));
        Assert.Equal(0, a.Calls);
        Assert.Equal(0, r.Calls);
    }
}
