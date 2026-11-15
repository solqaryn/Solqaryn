using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C1004CacheJobsApplicationServiceTests
{
    private const string Sha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private sealed class VerifiedSource : IC1004VerifiedWorkEvidenceSource
    {
        public C1004CacheJobsApplicationService.VerifiedSnapshot? Snapshot { get; set; } = Good();
        public int Reads { get; private set; }

        public ValueTask<C1004CacheJobsApplicationService.VerifiedSnapshot?> ReadVerifiedAsync(
            string workId, CancellationToken cancellationToken)
        {
            Reads++;
            return ValueTask.FromResult(Snapshot);
        }
    }

    private static ContextoTenantActual Context(int actor = 10, int empresa = 100) =>
        ContextoTenantActual.DesdeMembresia(new UsuarioEmpresa(actor, empresa, 5), actor, empresa);

    private static C1004CacheJobsApplicationService.VerifiedSnapshot Good() =>
        new(Context(), 10, 100, 3, true, false, true, false, false,
            new HashSet<string> { "export.request", "cache.invalidate" },
            0, 3, Now.AddMinutes(30), Now);

    private static C1004CacheJobsApplicationService.Request Work() =>
        new("work-1", "export.request", "idem-12345678", Sha, 100, 3);

    [Fact]
    public async Task ValidDataStillCannotQueueWriteOrGrantAccess()
    {
        var source = new VerifiedSource();
        var result = await new C1004CacheJobsApplicationService(source).PrepareAsync(Work());
        Assert.Equal("AUTHENTICATED_ADAPTER_AND_EXCLUSIVE_CAS_REQUIRED", result.Code);
        Assert.False(result.CanQueueWork);
        Assert.False(result.CanWriteData);
        Assert.False(result.CanIssueProviderGrant);
        Assert.False(result.CanReadPrivateArtifact);
        Assert.False(result.CertifiesTenantSecurity);
        Assert.Equal(1, source.Reads);
    }

    [Fact]
    public async Task MissingOrHostileInputDoesNotQueryBackend()
    {
        var source = new VerifiedSource();
        var service = new C1004CacheJobsApplicationService(source);
        Assert.Equal("REQUEST_INVALID", (await service.PrepareAsync(null)).Code);
        Assert.Equal("REQUEST_INVALID",
            (await service.PrepareAsync(Work() with { WorkId = "../evil" })).Code);
        Assert.Equal("REQUEST_INVALID",
            (await service.PrepareAsync(Work() with { Operation = "https://evil" })).Code);
        Assert.Equal("REQUEST_INVALID",
            (await service.PrepareAsync(Work() with { ExpectedVersion = 0 })).Code);
        Assert.Equal(0, source.Reads);
    }

    [Fact]
    public async Task MissingVerifiedSourceAndRevokedMembershipAreDenied()
    {
        var source = new VerifiedSource { Snapshot = null };
        var service = new C1004CacheJobsApplicationService(source);
        Assert.Equal("VERIFIED_BACKEND_CONTEXT_REQUIRED",
            (await service.PrepareAsync(Work())).Code);
        source.Snapshot = Good() with { ActiveMembership = false };
        Assert.Equal("ACTIVE_MEMBERSHIP_AND_OPERATION_PERMISSION_REQUIRED",
            (await service.PrepareAsync(Work())).Code);
        source.Snapshot = Good() with { PermissionRevoked = true };
        Assert.Equal("ACTIVE_MEMBERSHIP_AND_OPERATION_PERMISSION_REQUIRED",
            (await service.PrepareAsync(Work())).Code);
    }

    [Fact]
    public async Task CrossTenantHintCannotOverrideVerifiedOwner()
    {
        var service = new C1004CacheJobsApplicationService(new VerifiedSource());
        Assert.Equal("CLIENT_TENANT_OVERRIDE_DENIED",
            (await service.PrepareAsync(Work() with { ClientTenantHint = 200 })).Code);
    }

    [Fact]
    public async Task CrossTenantVerifiedContextIsDeniedByDomainRules()
    {
        var source = new VerifiedSource { Snapshot = Good() with { PersistedResourceEmpresaId = 200 } };
        Assert.Equal("WORK_POLICY_OR_TENANT_DENIED",
            (await new C1004CacheJobsApplicationService(source).PrepareAsync(
                Work() with { ClientTenantHint = null })).Code);
    }

    [Fact]
    public async Task RevokedServiceGrantAndUnallowlistedOperationDenied()
    {
        var source = new VerifiedSource { Snapshot = Good() with { IsServiceActor = true } };
        var service = new C1004CacheJobsApplicationService(source);
        Assert.Equal("ACTIVE_MEMBERSHIP_AND_OPERATION_PERMISSION_REQUIRED",
            (await service.PrepareAsync(Work())).Code);
        source.Snapshot = Good();
        Assert.Equal("OPERATION_NOT_ALLOWLISTED",
            (await service.PrepareAsync(Work() with { Operation = "backup.delete" })).Code);
    }

    [Fact]
    public async Task StaleVersionAndMalformedDigestCannotProceed()
    {
        var source = new VerifiedSource();
        var service = new C1004CacheJobsApplicationService(source);
        Assert.Equal("RESOURCE_VERSION_MISMATCH",
            (await service.PrepareAsync(Work() with { ExpectedVersion = 2 })).Code);
        Assert.Equal("WORK_PARAMETERS_INVALID",
            (await service.PrepareAsync(Work() with { PayloadSha256 = new string('z', 64) })).Code);
    }

    [Fact]
    public async Task RetryBudgetOrDeadlineViolationIsRejected()
    {
        var source = new VerifiedSource { Snapshot = Good() with { RetryAttempt = 3 } };
        var service = new C1004CacheJobsApplicationService(source);
        Assert.Equal("WORK_POLICY_OR_TENANT_DENIED",
            (await service.PrepareAsync(Work())).Code);
        source.Snapshot = Good() with { DeadlineUtc = Now };
        Assert.Equal("WORK_POLICY_OR_TENANT_DENIED",
            (await service.PrepareAsync(Work())).Code);
    }

    [Fact]
    public async Task SourceCancellationIsNotSwallowed()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new C1004CacheJobsApplicationService(new VerifiedSource())
                .PrepareAsync(Work(), cts.Token));
    }

    [Fact]
    public void ConstructorRequiresVerifiedEvidenceSource()
    {
        Assert.Throws<ArgumentNullException>(() => new C1004CacheJobsApplicationService(null!));
    }
}
