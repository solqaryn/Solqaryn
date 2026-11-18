using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C10-04.M05.05: adversarial tests of the real application security gate.
/// Verified evidence is a fixture, never a live JWT, tenant DB or provider grant.
/// </summary>
public sealed class C1004CacheJobsM0505AdversarialTests
{
    private const string Digest = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private sealed class Source : IC1004VerifiedWorkEvidenceSource
    {
        public C1004CacheJobsApplicationService.VerifiedSnapshot? Evidence { get; set; } = SafeSnapshot();
        public Exception? Failure { get; set; }
        public int Reads { get; private set; }
        public ValueTask<C1004CacheJobsApplicationService.VerifiedSnapshot?> ReadVerifiedAsync(
            string workId, CancellationToken cancellationToken)
        {
            Reads++;
            if (Failure is not null) throw Failure;
            return ValueTask.FromResult(Evidence);
        }
    }

    private static C1004CacheJobsApplicationService.VerifiedSnapshot SafeSnapshot() =>
        new(ContextoTenantActual.DesdeMembresia(new UsuarioEmpresa(10, 100, 5), 10, 100),
            10, 100, 7, true, false, true, false, false,
            new HashSet<string>(StringComparer.Ordinal) { "export.request", "cache.invalidate" },
            0, 3, Now.AddMinutes(30), Now);

    private static C1004CacheJobsApplicationService.Request Request() =>
        new("work-7", "export.request", "idem-12345678", Digest, 100, 7);

    private static void NeverAuthorizes(C1004CacheJobsAuthorizationGate.Decision result)
    {
        Assert.False(result.CanEnqueue);
        Assert.False(result.CanWrite);
        Assert.False(result.CanAcquireLease);
        Assert.False(result.CanGrantProviderAccess);
        Assert.False(result.CanReadPrivateArtifact);
        Assert.False(result.CertifiesRuntimeSecurity);
    }

    [Theory]
    [InlineData("revoked", "PERMISSION_OR_MEMBERSHIP_DENIED")]
    [InlineData("inactive", "PERMISSION_OR_MEMBERSHIP_DENIED")]
    [InlineData("no-operation-grant", "PERMISSION_OR_MEMBERSHIP_DENIED")]
    [InlineData("service-without-grant", "PERMISSION_OR_MEMBERSHIP_DENIED")]
    [InlineData("actor-mismatch", "ACTOR_OR_TENANT_MISMATCH")]
    [InlineData("tenant-mismatch", "ACTOR_OR_TENANT_MISMATCH")]
    [InlineData("stale-version", "PERSISTED_VERSION_STALE")]
    [InlineData("expired", "TENANT_OR_POLICY_DENIED")]
    [InlineData("retry-exhausted", "TENANT_OR_POLICY_DENIED")]
    public async Task NineAdversarialPolicyCasesDenyAllExternalEffects(
        string scenario, string expected)
    {
        var baseline = SafeSnapshot();
        var snapshot = scenario switch
        {
            "revoked" => baseline with { PermissionRevoked = true },
            "inactive" => baseline with { ActiveMembership = false },
            "no-operation-grant" => baseline with { ExplicitOperationPermission = false },
            "service-without-grant" => baseline with { IsServiceActor = true, ServiceGrantVerified = false },
            "actor-mismatch" => baseline with { VerifiedActorId = 20 },
            "tenant-mismatch" => baseline with { PersistedResourceEmpresaId = 200 },
            "stale-version" => baseline with { PersistedVersion = 8 },
            "expired" => baseline with { DeadlineUtc = Now },
            "retry-exhausted" => baseline with { RetryAttempt = 3 },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var source = new Source { Evidence = snapshot };
        var decision = await new C1004CacheJobsAuthorizationGate(source).CheckAsync(Request());
        Assert.Equal(expected, decision.Code);
        Assert.Equal(1, source.Reads);
        NeverAuthorizes(decision);
    }

    [Fact]
    public async Task HostileRequestShapeIsRejectedBeforeReadingVerifiedEvidence()
    {
        var source = new Source();
        var gate = new C1004CacheJobsAuthorizationGate(source);
        var malformed = new C1004CacheJobsApplicationService.Request?[]
        {
            null,
            Request() with { WorkId = "../tenant-b" },
            Request() with { Operation = "http://provider.example" },
            Request() with { IdempotencyKey = null },
            Request() with { PayloadSha256 = null },
            Request() with { ExpectedVersion = 0 }
        };
        foreach (var request in malformed)
        {
            var result = await gate.CheckAsync(request);
            Assert.Equal("REQUEST_INVALID", result.Code);
            NeverAuthorizes(result);
        }
        Assert.Equal(0, source.Reads);
    }

    [Fact]
    public async Task PrivateProviderExceptionNeverAppearsInSafeResultCode()
    {
        const string marker = "PRIVATE_SERVICE_SECRET_DO_NOT_RENDER";
        var source = new Source { Failure = new InvalidOperationException(marker) };
        var decision = await new C1004CacheJobsAuthorizationGate(source).CheckAsync(Request());
        Assert.Equal("VERIFIED_SOURCE_UNAVAILABLE", decision.Code);
        Assert.DoesNotContain(marker, decision.Code, StringComparison.Ordinal);
        NeverAuthorizes(decision);
        Assert.Equal(1, source.Reads);
    }

    [Fact]
    public async Task ForgedTenantHintCannotBypassPersistedTenantAndNeverGrantsLease()
    {
        var source = new Source();
        var decision = await new C1004CacheJobsAuthorizationGate(source).CheckAsync(
            Request() with { ClientTenantHint = 200 });
        Assert.Equal("CLIENT_TENANT_OVERRIDE_DENIED", decision.Code);
        NeverAuthorizes(decision);
    }

    [Fact]
    public async Task RestoreAndUnlistedOperationNeverGainExecutionAuthority()
    {
        var gate = new C1004CacheJobsAuthorizationGate(new Source());
        foreach (var operation in new[] { "backup.restore", "backup.delete", "tenant.export.all" })
        {
            var decision = await gate.CheckAsync(Request() with { Operation = operation });
            Assert.Equal("OPERATION_NOT_ALLOWLISTED", decision.Code);
            NeverAuthorizes(decision);
        }
    }

    [Fact]
    public async Task MissingSnapshotFailsClosedWithoutLeakingAnyArtifact()
    {
        var decision = await new C1004CacheJobsAuthorizationGate(
            new Source { Evidence = null }).CheckAsync(Request());
        Assert.Equal("VERIFIED_BACKEND_CONTEXT_REQUIRED", decision.Code);
        NeverAuthorizes(decision);
    }

    [Fact]
    public async Task CancelledCheckNeverReadsEvidenceOrIssuesAnEffect()
    {
        var source = new Source();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new C1004CacheJobsAuthorizationGate(source).CheckAsync(Request(), cts.Token));
        Assert.Equal(0, source.Reads);
    }
}
