using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1004CacheJobsIsolatedSnapshotAdapterTests
{
    private const string Digest = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static ContextoTenantActual Context(int actor = 10, int tenant = 100) =>
        ContextoTenantActual.DesdeMembresia(new UsuarioEmpresa(actor, tenant, 5), actor, tenant);

    private static C1004CacheJobsApplicationService.VerifiedSnapshot Valid(
        IReadOnlySet<string>? operations = null) =>
        new(Context(), 10, 100, 7, true, false, true, false, false,
            operations ?? new HashSet<string>(StringComparer.Ordinal) { "export.request", "cache.invalidate" },
            0, 3, Now.AddMinutes(30), Now);

    private static C1004CacheJobsApplicationService.Request Request() =>
        new("work-7", "export.request", "idem-12345678", Digest, 100, 7);

    private static C1004CacheJobsIsolatedSnapshotAdapter Adapter(
        C1004CacheJobsApplicationService.VerifiedSnapshot? snapshot = null) =>
        new("work-7", 100, 10, 7, snapshot ?? Valid());

    private static Task<C1004CacheJobsAuthorizationGate.Decision> Evaluate(
        C1004CacheJobsIsolatedSnapshotAdapter adapter,
        C1004CacheJobsApplicationService.Request? request = null) =>
        adapter.EvaluateReadOnlyAsync(request ?? Request(), "work-7", 100, 10, 7);

    [Fact]
    public async Task NominalReadOnlyPreflightNeverGrantsAnyRuntimeCapability()
    {
        var adapter = Adapter();
        var result = await Evaluate(adapter);
        Assert.Equal("PRECONDITIONS_ONLY_EXTERNAL_CAS_REQUIRED", result.Code);
        Assert.False(result.CanEnqueue);
        Assert.False(result.CanAcquireLease);
        Assert.False(result.CanWrite);
        Assert.False(result.CanGrantProviderAccess);
        Assert.False(result.CanReadPrivateArtifact);
        Assert.False(adapter.CanEnqueue);
        Assert.False(adapter.CanWrite);
        Assert.False(adapter.CanAcquireLease);
        Assert.False(adapter.CanGrantProviderAccess);
        Assert.False(adapter.CanReadPrivateArtifact);
        Assert.False(adapter.CanBypassTenantFilters);
        Assert.False(adapter.CertifiesJwtMySqlOrR2);
        Assert.False(adapter.IsLiveProvider);
    }

    [Fact]
    public void InvalidConstructorInputsFailClosed()
    {
        Assert.Throws<ArgumentException>(() => new C1004CacheJobsIsolatedSnapshotAdapter("../bad", 100, 10, 7, Valid()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new C1004CacheJobsIsolatedSnapshotAdapter("work-7", 0, 10, 7, Valid()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new C1004CacheJobsIsolatedSnapshotAdapter("work-7", 100, 0, 7, Valid()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new C1004CacheJobsIsolatedSnapshotAdapter("work-7", 100, 10, 0, Valid()));
        Assert.Throws<ArgumentNullException>(() => new C1004CacheJobsIsolatedSnapshotAdapter("work-7", 100, 10, 7, null!));
    }

    [Fact]
    public async Task NullOrForgedWorkIdentifierNeverReachesProvider()
    {
        var adapter = Adapter();
        Assert.Equal("ISOLATED_WORK_REQUEST_REQUIRED",
            (await adapter.EvaluateReadOnlyAsync(null, "work-7", 100, 10, 7)).Code);
        Assert.Equal("ISOLATED_WORK_ID_MISMATCH",
            (await adapter.EvaluateReadOnlyAsync(Request(), "work-8", 100, 10, 7)).Code);
        Assert.Equal("ISOLATED_WORK_ID_MISMATCH",
            (await adapter.EvaluateReadOnlyAsync(Request() with { WorkId = "work-8" }, "work-7", 100, 10, 7)).Code);
    }

    [Fact]
    public async Task TenantAndActorCrossingAlwaysDeny()
    {
        var adapter = Adapter();
        Assert.Equal("ISOLATED_TENANT_MISMATCH",
            (await adapter.EvaluateReadOnlyAsync(Request(), "work-7", 200, 10, 7)).Code);
        Assert.Equal("ISOLATED_ACTOR_MISMATCH",
            (await adapter.EvaluateReadOnlyAsync(Request(), "work-7", 100, 11, 7)).Code);
    }

    [Fact]
    public async Task StaleObservedOrRequestedVersionCannotPass()
    {
        var adapter = Adapter();
        Assert.Equal("ISOLATED_VERSION_MISMATCH",
            (await adapter.EvaluateReadOnlyAsync(Request(), "work-7", 100, 10, 6)).Code);
        Assert.Equal("ISOLATED_VERSION_MISMATCH",
            (await adapter.EvaluateReadOnlyAsync(Request() with { ExpectedVersion = 8 }, "work-7", 100, 10, 7)).Code);
    }

    [Fact]
    public async Task TamperedCapturedTenantActorOrVersionCannotBeLaundered()
    {
        foreach (var evidence in new[]
        {
            Valid() with { VerifiedActorId = 11 },
            Valid() with { PersistedResourceEmpresaId = 200 },
            Valid() with { PersistedVersion = 8 },
            Valid() with { Context = Context(11, 100) },
            Valid() with { Context = Context(10, 200) }
        })
        {
            Assert.Equal("ISOLATED_CAPTURED_AUTHORITY_MISMATCH",
                (await Evaluate(Adapter(evidence))).Code);
        }
    }

    [Fact]
    public async Task MutatingOriginalOperationSetCannotExpandCapturedAllowlist()
    {
        var mutable = new HashSet<string>(StringComparer.Ordinal) { "export.request" };
        var adapter = Adapter(Valid(mutable));
        mutable.Add("backup.delete");
        var malicious = Request() with { Operation = "backup.delete" };
        Assert.Equal("OPERATION_NOT_ALLOWLISTED", (await Evaluate(adapter, malicious)).Code);
        Assert.Equal("PRECONDITIONS_ONLY_EXTERNAL_CAS_REQUIRED",
            (await Evaluate(adapter)).Code);
    }

    [Fact]
    public async Task ClientTenantHintCannotOverridePersistedOwner()
    {
        Assert.Equal("CLIENT_TENANT_OVERRIDE_DENIED",
            (await Evaluate(Adapter(), Request() with { ClientTenantHint = 200 })).Code);
    }

    [Fact]
    public async Task RevocationMissingPermissionAndMissingServiceGrantDeny()
    {
        foreach (var evidence in new[]
        {
            Valid() with { ActiveMembership = false },
            Valid() with { PermissionRevoked = true },
            Valid() with { ExplicitOperationPermission = false },
            Valid() with { IsServiceActor = true, ServiceGrantVerified = false }
        })
        {
            Assert.Equal("PERMISSION_OR_MEMBERSHIP_DENIED",
                (await Evaluate(Adapter(evidence))).Code);
        }
    }

    [Fact]
    public async Task InvalidPayloadDeadlineAndExhaustedRetryBudgetDeny()
    {
        Assert.Equal("REQUEST_PARAMETERS_INVALID",
            (await Evaluate(Adapter(), Request() with { PayloadSha256 = "tampered" })).Code);
        Assert.Equal("TENANT_OR_POLICY_DENIED",
            (await Evaluate(Adapter(Valid() with { DeadlineUtc = Now }))).Code);
        Assert.Equal("TENANT_OR_POLICY_DENIED",
            (await Evaluate(Adapter(Valid() with { RetryAttempt = 3 }))).Code);
    }

    [Fact]
    public async Task NoExpiredLeaseOrJobCanBeGrantedByTheAdapter()
    {
        var adapter = Adapter();
        Assert.False(adapter.CanAcquireLease);
        Assert.False(adapter.CanEnqueue);
        Assert.False(adapter.CanReadPrivateArtifact);
        Assert.False(adapter.CanGrantProviderAccess);
        var result = await Evaluate(adapter);
        Assert.False(result.CanAcquireLease);
        Assert.False(result.CanEnqueue);
        Assert.False(result.CertifiesRuntimeSecurity);
    }

    [Fact]
    public async Task CancellationDoesNotAuthorizeOrStartWork()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var adapter = Adapter();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await adapter.EvaluateReadOnlyAsync(Request(), "work-7", 100, 10, 7, cts.Token));
        Assert.False(adapter.CanEnqueue);
    }
}
