using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5UiLightnessBoundedReviewQueueTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static ContextoTenantActual Actor(int tenantId = 11)
    {
        var user = new Usuario { Id = 7, Activo = true, RolId = 999 };
        var tenant = new Empresa("A") { Id = tenantId };
        var membership = new UsuarioEmpresa(7, tenantId, 19);
        var role = new Rol { Id = 19, EmpresaId = tenantId, Activo = true };
        return IdentidadUniversalReglas.CrearContextoVerificado(
            user, tenant, membership, role);
    }

    private static V5UiLightnessTrustedSnapshot Snapshot() => new(
        new V5UiLightnessDomainRules.TrustedRequestContext(
            11, 11, true, true, true, true, true,
            false, false, false, false, false, "rev-4", "rev-4"),
        new V5UiLightnessDomainRules.TrustedBuildEvidence(
            590 * 1024, 12 * 1024, Sha, Sha, true, false));

    private static V5UiLightnessBoundedReviewQueue Create(int capacity = 2,
        int maxLifetimeKeys = 4, V5UiLightnessTrustedSnapshot? snapshot = null) =>
        new(new V5UiLightnessIsolatedSnapshotAdapter(
            11, "resource_41", Sha, snapshot ?? Snapshot()),
            11, Sha, capacity, maxLifetimeKeys);

    [Fact] public async Task NominalQueuePumpsRealPreflightButNeverGrantsAccess()
    {
        var q = Create();
        var result = q.Enqueue(Actor(), 11, "resource_41", Sha, "op_1");
        Assert.Equal("QUEUED_FOR_ISOLATED_REVIEW", result.Code);
        Assert.Equal(1, q.PendingCount);
        var reviewed = await q.ProcessNextAsync();
        Assert.Equal("EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED", reviewed.Code);
        Assert.False(reviewed.GrantsAccess);
        Assert.False(reviewed.IsLiveWorker);
        Assert.False(reviewed.WritesDatabase);
        Assert.False(reviewed.HasDurableIdempotency);
        Assert.False(reviewed.CertifiesCoreWebVitals);
        Assert.Equal(0, q.PendingCount);
    }

    [Fact] public void SameKeyReplayNeverCreatesDuplicateWork()
    {
        var q = Create();
        Assert.Equal("QUEUED_FOR_ISOLATED_REVIEW", q.Enqueue(Actor(), 11, "resource_41", Sha, "op_1").Code);
        Assert.Equal("REPLAY_REQUIRES_DURABLE_IDEMPOTENCY",
            q.Enqueue(Actor(), 11, "resource_41", Sha, "op_1").Code);
        Assert.Equal(1, q.PendingCount);
    }

    [Fact] public async Task CompletedReplayCannotClaimDurableSuccess()
    {
        var q = Create();
        q.Enqueue(Actor(), 11, "resource_41", Sha, "op_1");
        await q.ProcessNextAsync();
        Assert.Equal("REPLAY_REQUIRES_DURABLE_IDEMPOTENCY",
            q.Enqueue(Actor(), 11, "resource_41", Sha, "op_1").Code);
        Assert.Equal(0, q.PendingCount);
    }

    [Fact] public void SameKeyDifferentResourceIsConflict()
    {
        var q = Create();
        q.Enqueue(Actor(), 11, "resource_41", Sha, "op_1");
        Assert.Equal("IDEMPOTENCY_KEY_CONFLICT_DENIED",
            q.Enqueue(Actor(), 11, "resource_42", Sha, "op_1").Code);
        Assert.Equal(1, q.PendingCount);
    }

    [Fact] public void ForeignTenantIsDeniedBeforeQueueMutation()
    {
        var q = Create();
        Assert.Throws<InvalidOperationException>(() =>
            q.Enqueue(Actor(11), 22, "resource_41", Sha, "op_1"));
        Assert.Equal("ISOLATED_QUEUE_TENANT_SCOPE_MISMATCH",
            q.Enqueue(Actor(22), 22, "resource_41", Sha, "op_2").Code);
        Assert.Equal(0, q.PendingCount);
    }

    [Fact] public void QueueCapacityFailsClosedAndKeepsFirstItem()
    {
        var q = Create(capacity: 1);
        Assert.Equal("QUEUED_FOR_ISOLATED_REVIEW", q.Enqueue(Actor(), 11, "resource_41", Sha, "op_1").Code);
        Assert.Equal("BOUNDED_QUEUE_FULL", q.Enqueue(Actor(), 11, "resource_41", Sha, "op_2").Code);
        Assert.Equal(1, q.PendingCount);
    }

    [Fact] public async Task RegistryCapacityFailsClosedEvenAfterProcessing()
    {
        var q = Create(capacity: 1, maxLifetimeKeys: 1);
        q.Enqueue(Actor(), 11, "resource_41", Sha, "op_1");
        await q.ProcessNextAsync();
        Assert.Equal("IDEMPOTENCY_REGISTRY_FULL_FAIL_CLOSED",
            q.Enqueue(Actor(), 11, "resource_41", Sha, "op_2").Code);
    }

    [Fact] public void StaleShaAndUnsafeIdsAreRejected()
    {
        var q = Create();
        Assert.Equal("ISOLATED_QUEUE_SOURCE_SHA_MISMATCH",
            q.Enqueue(Actor(), 11, "resource_41", OtherSha, "op_1").Code);
        Assert.Equal("INVALID_RESOURCE_OR_IDEMPOTENCY_KEY",
            q.Enqueue(Actor(), 11, "../tenant_B", Sha, "op_1").Code);
        Assert.Equal("INVALID_RESOURCE_OR_IDEMPOTENCY_KEY",
            q.Enqueue(Actor(), 11, "resource_41", Sha, "\r\n").Code);
        Assert.Equal(0, q.PendingCount);
    }

    [Fact] public async Task CancellationKeepsItemForRetry()
    {
        var q = Create();
        q.Enqueue(Actor(), 11, "resource_41", Sha, "op_1");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            q.ProcessNextAsync(cts.Token));
        Assert.Equal(1, q.PendingCount);
    }

    [Fact] public async Task RevokedSnapshotIsNeverPromotedToSuccess()
    {
        var snapshot = Snapshot();
        var bad = snapshot with { Request = snapshot.Request with
        { CurrentMembershipVerified = false } };
        var q = Create(snapshot: bad);
        q.Enqueue(Actor(), 11, "resource_41", Sha, "op_1");
        var reviewed = await q.ProcessNextAsync();
        Assert.Equal("BACKEND_AUTHORITY_REQUIRED", reviewed.Code);
        Assert.False(reviewed.GrantsAccess);
    }

    [Fact] public async Task EmptyQueueProducesNoFalsePass()
    {
        Assert.Equal("NO_PENDING_ISOLATED_REVIEW",
            (await Create().ProcessNextAsync()).Code);
    }

    [Fact] public void InvalidConstructionRejectsUnboundedOrForeignInputs()
    {
        var adapter = new V5UiLightnessIsolatedSnapshotAdapter(
            11, "resource_41", Sha, Snapshot());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new V5UiLightnessBoundedReviewQueue(adapter, 11, Sha, capacity: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new V5UiLightnessBoundedReviewQueue(adapter, 11, Sha, capacity: 33));
        Assert.Throws<ArgumentException>(() =>
            new V5UiLightnessBoundedReviewQueue(adapter, 11, "BAD"));
    }
}
