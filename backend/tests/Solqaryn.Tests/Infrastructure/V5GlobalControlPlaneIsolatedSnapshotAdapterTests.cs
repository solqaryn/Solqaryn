using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5GlobalControlPlaneDomainRules;
using Gate = Solqaryn.Application.AutomationV5.V5GlobalControlPlaneAuthorizationGate;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5GlobalControlPlaneIsolatedSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";
    private static readonly DateTimeOffset Now =
        new(2026, 11, 15, 13, 0, 0, TimeSpan.Zero);

    private static Rules.TrustedSnapshot Snapshot() => new(
        "actor-a", "tenant-a", "tenant-a", true, true, true,
        false, false, true, true, true, "audit-42",
        false, false, null, null, null, false, null);

    private static Gate.Evidence Evidence() =>
        new(Sha, "actor-a", "tenant-a", true, true, true, false, Now, Snapshot());

    private static Gate.Request Request() =>
        new(Rules.Operation.OwnTenantMetadataRead, "tenant-a", Sha,
            null, false, false, false);

    private static V5GlobalControlPlaneIsolatedSnapshotAdapter Adapter(
        Gate.Evidence? evidence = null) =>
        new("actor-a", "tenant-a", Sha, evidence ?? Evidence());

    private static ValueTask<Gate.Result> Evaluate(
        V5GlobalControlPlaneIsolatedSnapshotAdapter adapter, Gate.Request? request = null,
        string? actor = "actor-a", string? tenant = "tenant-a",
        string? sha = Sha, CancellationToken cancellationToken = default) =>
        adapter.EvaluateReadOnlyAsync(request ?? Request(), actor, tenant, sha, cancellationToken);

    [Fact]
    public async Task ValidSnapshotOnlyRequiresFutureAuthenticatedRuntimeAdapter()
    {
        var adapter = Adapter();
        var result = await Evaluate(adapter);
        Assert.Equal("AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED", result.Code);
        Assert.False(result.GrantsAccess);
        Assert.False(result.GrantsTenantEnumeration);
        Assert.False(result.GrantsCrossTenantRead);
        Assert.False(result.GrantsInstitutionalWrite);
        Assert.False(result.GrantsExceptionalLease);
        Assert.False(adapter.GrantsAccess);
        Assert.False(adapter.AllowsPersistence);
        Assert.False(adapter.GrantsPlatformDirectory);
        Assert.False(adapter.GrantsExceptionalTenantLease);
        Assert.False(adapter.IsLiveProvider);
    }

    [Fact]
    public async Task NullRequestAndForgedActorAreDenied()
    {
        var adapter = Adapter();
        Assert.Equal("ISOLATED_CONTROL_PLANE_REQUEST_REQUIRED",
            (await adapter.EvaluateReadOnlyAsync(null, "actor-a", "tenant-a", Sha)).Code);
        Assert.Equal("ISOLATED_CONTROL_PLANE_ACTOR_MISMATCH",
            (await Evaluate(adapter, actor: "actor-b")).Code);
        Assert.Equal("ISOLATED_CONTROL_PLANE_ACTOR_MISMATCH",
            (await Evaluate(adapter, actor: "../actor-a")).Code);
    }

    [Fact]
    public async Task CrossTenantRequestsFailBeforeApplicationGate()
    {
        var adapter = Adapter();
        Assert.Equal("ISOLATED_CONTROL_PLANE_TENANT_MISMATCH",
            (await Evaluate(adapter, tenant: "tenant-b")).Code);
        Assert.Equal("ISOLATED_CONTROL_PLANE_TENANT_MISMATCH",
            (await Evaluate(adapter, Request() with { RequestedTenantId = "tenant-b" })).Code);
    }

    [Fact]
    public async Task StaleAndMissingSourceShaNeverBecomesPass()
    {
        var adapter = Adapter();
        Assert.Equal("ISOLATED_CONTROL_PLANE_SOURCE_SHA_MISMATCH",
            (await Evaluate(adapter, sha: OtherSha)).Code);
        Assert.Equal("ISOLATED_CONTROL_PLANE_SOURCE_SHA_MISMATCH",
            (await Evaluate(adapter, sha: null)).Code);
        Assert.Equal("ISOLATED_CONTROL_PLANE_SOURCE_SHA_MISMATCH",
            (await Evaluate(adapter, Request() with { ExpectedSourceSha = OtherSha })).Code);
        var stale = Adapter(Evidence() with { ObservedSourceSha = OtherSha });
        Assert.Equal("ISOLATED_CONTROL_PLANE_SOURCE_SHA_MISMATCH",
            (await Evaluate(stale)).Code);
    }

    [Fact]
    public async Task CapturedActorTenantAndSnapshotMustMatch()
    {
        var badActor = Adapter(Evidence() with { VerifiedActorId = "actor-b" });
        Assert.Equal("ISOLATED_CONTROL_PLANE_CAPTURED_AUTHORITY_MISMATCH",
            (await Evaluate(badActor)).Code);
        var badTenant = Adapter(Evidence() with { VerifiedTenantId = "tenant-b" });
        Assert.Equal("ISOLATED_CONTROL_PLANE_CAPTURED_AUTHORITY_MISMATCH",
            (await Evaluate(badTenant)).Code);
        var badSnapshot = Adapter(Evidence() with
        { Snapshot = Snapshot() with { VerifiedTargetTenantId = "tenant-b" } });
        Assert.Equal("ISOLATED_CONTROL_PLANE_CAPTURED_AUTHORITY_MISMATCH",
            (await Evaluate(badSnapshot)).Code);
    }

    [Fact]
    public async Task RevocationAndMfaRemainMandatory()
    {
        var revoked = Adapter(Evidence() with { PermissionRevoked = true });
        Assert.Equal("CONTROL_PLANE_CURRENT_BACKEND_AUTHORITY_REQUIRED",
            (await Evaluate(revoked)).Code);
        var withoutMfa = Adapter(Evidence() with
        { Snapshot = Snapshot() with { RecentMfaVerified = false } });
        Assert.Equal("MFA_AND_REAUTHENTICATION_REQUIRED",
            (await Evaluate(withoutMfa)).Code);
    }

    [Fact]
    public async Task PlatformDirectoryNeedsSeparateGrant()
    {
        var directory = Request() with { Operation = Rules.Operation.PlatformTenantDirectoryRead };
        Assert.Equal("INDEPENDENT_PLATFORM_GRANT_REQUIRED",
            (await Evaluate(Adapter(), directory)).Code);
        var granted = Adapter(Evidence() with { Snapshot = Snapshot() with
        { PlatformIdentityVerified = true, PlatformGrantVerified = true } });
        var result = await Evaluate(granted, directory);
        Assert.True(result.NeedsAuthenticatedRuntimeAdapter);
        Assert.False(result.GrantsTenantEnumeration);
    }

    [Fact]
    public async Task ClientSuppliedAuthorityAndWritesAreDenied()
    {
        var adapter = Adapter();
        Assert.Equal("CONTROL_PLANE_CLIENT_AUTHORITY_DENIED",
            (await Evaluate(adapter, Request() with { ClientAuthorityHint = true })).Code);
        Assert.Equal("CONTROL_PLANE_UNSAFE_OPERATION_DENIED",
            (await Evaluate(adapter, Request() with { WriteOrExport = true })).Code);
        Assert.Equal("CONTROL_PLANE_UNSAFE_OPERATION_DENIED",
            (await Evaluate(adapter, Request() with { SensitiveData = true })).Code);
    }

    [Fact]
    public async Task CancellationAndMalformedConstructorInputsFailClosed()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Evaluate(Adapter(), cancellationToken: cts.Token));
        Assert.Throws<ArgumentException>(() =>
            new V5GlobalControlPlaneIsolatedSnapshotAdapter("../a", "tenant-a", Sha, Evidence()));
        Assert.Throws<ArgumentException>(() =>
            new V5GlobalControlPlaneIsolatedSnapshotAdapter("actor-a", "tenant-a", "short", Evidence()));
        Assert.Throws<ArgumentNullException>(() =>
            new V5GlobalControlPlaneIsolatedSnapshotAdapter("actor-a", "tenant-a", Sha, null!));
    }
}
