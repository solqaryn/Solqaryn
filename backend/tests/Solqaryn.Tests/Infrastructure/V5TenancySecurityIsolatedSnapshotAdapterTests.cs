using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Gate = Solqaryn.Application.AutomationV5.V5TenancySecurityAuthorizationGate;
using Rules = Solqaryn.Domain.AutomationV5.V5TenancySecurityDomainRules;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5TenancySecurityIsolatedSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static Gate.VerifiedEvidence Verified() => new(
        ObservedSourceSha: Sha,
        VerifiedResourceId: 101,
        VerifiedTenantId: 7,
        VerifiedActorId: 11,
        ServerAuthenticationVerified: true,
        CurrentMembershipVerified: true,
        ExplicitPermissionVerified: true,
        AuthorizationReadFromTrustedBackend: true,
        PermissionRevoked: false,
        Authority: new Rules.TrustedAuthority(7, 11, 3, true, true, false, false),
        Resource: new Rules.PersistedResource(7, 7, 3, true, false, false));

    private static Gate.Request Request(Rules.Surface surface = Rules.Surface.BusinessRead) =>
        new(101, surface, Sha, false, false, false, null);

    private static V5TenancySecurityIsolatedSnapshotAdapter Adapter(
        Gate.VerifiedEvidence? evidence = null) =>
        new(101, 7, 11, Sha, evidence ?? Verified());

    private static ValueTask<Gate.Result> Evaluate(
        V5TenancySecurityIsolatedSnapshotAdapter adapter, Gate.Request? request = null) =>
        adapter.EvaluateReadOnlyAsync(request ?? Request(), 101, 7, 11, Sha);

    [Fact]
    public async Task VerifiedSnapshotOnlyRequiresAnotherAuthenticatedAdapter()
    {
        var adapter = Adapter();
        var result = await Evaluate(adapter);
        Assert.Equal("AUTHENTICATED_EXECUTION_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsAuthenticatedExecutionAdapter);
        Assert.False(result.GrantsResourceRead);
        Assert.False(result.GrantsFileDownload);
        Assert.False(result.GrantsBackgroundJob);
        Assert.False(result.GrantsControlPlane);
        Assert.False(result.GrantsInstitutionalWrite);
        Assert.False(result.AllowsTenantFilterBypass);
        Assert.False(adapter.GrantsAccess);
        Assert.False(adapter.AllowsPersistence);
        Assert.False(adapter.CertifiesHttpJwtOrMySql);
        Assert.False(adapter.IsLiveEvidenceProvider);
    }

    [Fact]
    public void InvalidConstructorIdentifiersAndEvidenceAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new V5TenancySecurityIsolatedSnapshotAdapter(0, 7, 11, Sha, Verified()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new V5TenancySecurityIsolatedSnapshotAdapter(101, 0, 11, Sha, Verified()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new V5TenancySecurityIsolatedSnapshotAdapter(101, 7, 0, Sha, Verified()));
        Assert.Throws<ArgumentException>(() =>
            new V5TenancySecurityIsolatedSnapshotAdapter(101, 7, 11, "short", Verified()));
        Assert.Throws<ArgumentException>(() =>
            new V5TenancySecurityIsolatedSnapshotAdapter(101, 7, 11, Sha.ToUpperInvariant(), Verified()));
        Assert.Throws<ArgumentNullException>(() =>
            new V5TenancySecurityIsolatedSnapshotAdapter(101, 7, 11, Sha, null!));
    }

    [Fact]
    public async Task MissingRequestNeverAuthorizes()
    {
        var result = await Adapter().EvaluateReadOnlyAsync(null, 101, 7, 11, Sha);
        Assert.Equal("ISOLATED_SECURITY_REQUEST_REQUIRED", result.Code);
        Assert.False(result.GrantsResourceRead);
    }

    [Fact]
    public async Task ForgedResourceAndDifferentRequestedResourceAreDenied()
    {
        var adapter = Adapter();
        Assert.Equal("ISOLATED_SECURITY_RESOURCE_MISMATCH",
            (await adapter.EvaluateReadOnlyAsync(Request(), 102, 7, 11, Sha)).Code);
        Assert.Equal("ISOLATED_SECURITY_RESOURCE_MISMATCH",
            (await adapter.EvaluateReadOnlyAsync(Request() with { ResourceId = 102 }, 101, 7, 11, Sha)).Code);
    }

    [Fact]
    public async Task ForeignTenantAndActorCannotReuseCapturedSnapshot()
    {
        var adapter = Adapter();
        Assert.Equal("ISOLATED_SECURITY_TENANT_MISMATCH",
            (await adapter.EvaluateReadOnlyAsync(Request(), 101, 8, 11, Sha)).Code);
        Assert.Equal("ISOLATED_SECURITY_ACTOR_MISMATCH",
            (await adapter.EvaluateReadOnlyAsync(Request(), 101, 7, 12, Sha)).Code);
    }

    [Fact]
    public async Task StaleObservedOrRequestedShaIsDenied()
    {
        var adapter = Adapter();
        Assert.Equal("ISOLATED_SECURITY_SHA_MISMATCH",
            (await adapter.EvaluateReadOnlyAsync(Request(), 101, 7, 11, OtherSha)).Code);
        Assert.Equal("ISOLATED_SECURITY_SHA_MISMATCH",
            (await adapter.EvaluateReadOnlyAsync(Request() with { ExpectedSourceSha = OtherSha }, 101, 7, 11, Sha)).Code);
    }

    [Fact]
    public async Task TamperedCapturedEvidenceCannotBeLaunderedByMatchingInputs()
    {
        var baseEvidence = Verified();
        foreach (var evidence in new[]
        {
            baseEvidence with { VerifiedResourceId = 102 },
            baseEvidence with { VerifiedTenantId = 8 },
            baseEvidence with { VerifiedActorId = 12 },
            baseEvidence with { ObservedSourceSha = OtherSha }
        })
        {
            Assert.Equal("ISOLATED_SECURITY_CAPTURED_EVIDENCE_MISMATCH",
                (await Evaluate(Adapter(evidence))).Code);
        }
    }

    [Fact]
    public async Task RevokedOrUntrustedBackendAuthorityFailsClosed()
    {
        var original = Verified();
        foreach (var evidence in new[]
        {
            original with { PermissionRevoked = true },
            original with { CurrentMembershipVerified = false },
            original with { ExplicitPermissionVerified = false },
            original with { ServerAuthenticationVerified = false },
            original with { AuthorizationReadFromTrustedBackend = false },
            original with { Resource = original.Resource! with { ParentTenantId = 8 } },
            original with { Resource = original.Resource! with { OwnershipReadFromPersistence = false } }
        })
        {
            var verdict = await Evaluate(Adapter(evidence));
            Assert.False(verdict.NeedsAuthenticatedExecutionAdapter);
            Assert.False(verdict.GrantsResourceRead);
        }
    }

    [Fact]
    public async Task FilterBypassEnumerationAndInstitutionalWriteCannotReachExecution()
    {
        var adapter = Adapter();
        foreach (var request in new[]
        {
            Request() with { RequestsTenantFilterBypass = true },
            Request() with { AttemptsCrossTenantEnumeration = true },
            Request() with { RequestsInstitutionalWrite = true }
        })
        {
            var result = await Evaluate(adapter, request);
            Assert.False(result.NeedsAuthenticatedExecutionAdapter);
            Assert.False(result.GrantsInstitutionalWrite);
            Assert.False(adapter.AllowsTenantFilterBypass);
        }
    }

    [Fact]
    public async Task BranchAndFileExpiryAreEnforcedByRealApplicationGate()
    {
        var baseEvidence = Verified();
        var wrongBranch = baseEvidence with
        {
            Resource = baseEvidence.Resource! with { OwnerBranchId = 99 }
        };
        Assert.Equal("BRANCH_SCOPE_MISMATCH",
            (await Evaluate(Adapter(wrongBranch), Request(Rules.Surface.BranchRead))).Code);

        var expired = baseEvidence with
        {
            Resource = baseEvidence.Resource! with { SignedLinkExpired = true }
        };
        Assert.Equal("FILE_ACCESS_REVOKED_OR_EXPIRED",
            (await Evaluate(Adapter(expired), Request(Rules.Surface.FileDownload))).Code);
    }

    [Fact]
    public async Task JobsAndControlPlaneNeverDispatchEvenWhenSnapshotIsPrivileged()
    {
        var baseEvidence = Verified();
        Assert.Equal("SERVICE_JOB_AUTHORITY_REQUIRED",
            (await Evaluate(Adapter(), Request(Rules.Surface.BackgroundJob))).Code);
        Assert.Equal("CONTROL_PLANE_AUTHORITY_REQUIRED",
            (await Evaluate(Adapter(), Request(Rules.Surface.ControlPlane))).Code);

        var privileged = baseEvidence with
        {
            Authority = baseEvidence.Authority! with
            {
                PrivilegedServiceGrantVerified = true,
                AuditedServiceContextVerified = true
            }
        };
        var adapter = Adapter(privileged);
        var job = await Evaluate(adapter, Request(Rules.Surface.BackgroundJob) with
        {
            IdempotencyKey = "tenant-7-job-101"
        });
        Assert.True(job.NeedsAuthenticatedExecutionAdapter);
        Assert.False(job.GrantsBackgroundJob);
        Assert.False(adapter.GrantsBackgroundJob);
        var controlPlane = await Evaluate(adapter, Request(Rules.Surface.ControlPlane));
        Assert.True(controlPlane.NeedsAuthenticatedExecutionAdapter);
        Assert.False(controlPlane.GrantsControlPlane);
    }

    [Fact]
    public async Task CancelledReadNeverGrantsAndReturnsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var adapter = Adapter();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await adapter.EvaluateReadOnlyAsync(Request(), 101, 7, 11, Sha, cancellation.Token));
        Assert.False(adapter.GrantsAccess);
    }
}
