using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>C10-04.M05.02: live Application DTO preflight, synthetic evidence source.</summary>
public sealed class C1004CacheJobsM0502DtoSafetyTests
{
    private const string Secret = "PRIVATE_TENANT_TOKEN_NEVER_LEAK";
    private const string Digest = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private sealed class Source : IC1004VerifiedWorkEvidenceSource
    {
        public int Reads { get; private set; }
        public int Failure { get; set; }
        public C1004CacheJobsApplicationService.VerifiedSnapshot? Snapshot { get; set; } = Good();
        public ValueTask<C1004CacheJobsApplicationService.VerifiedSnapshot?> ReadVerifiedAsync(
            string workId, CancellationToken cancellationToken)
        {
            Reads++;
            if (Failure == 1) throw new InvalidOperationException(Secret);
            if (Failure == 2)
                return ValueTask.FromException<C1004CacheJobsApplicationService.VerifiedSnapshot?>(
                    new InvalidOperationException(Secret));
            if (Failure == 3)
                throw new OperationCanceledException(cancellationToken);
            return ValueTask.FromResult(Snapshot);
        }
    }

    private static C1004CacheJobsApplicationService.VerifiedSnapshot Good() =>
        new(ContextoTenantActual.DesdeMembresia(new UsuarioEmpresa(10, 100, 5), 10, 100),
            10, 100, 7, true, false, true, false, false,
            new HashSet<string> { "export.request" }, 0, 3, Now.AddMinutes(30), Now);
    private static C1004CacheJobsApplicationService.Request Work() =>
        new("work-7", "export.request", "idem-12345678", Digest, 100, 7);

    private static void AssertNoEffects(C1004CacheJobsApplicationService.Result result)
    {
        Assert.False(result.CanQueueWork);
        Assert.False(result.CanWriteData);
        Assert.False(result.CanIssueProviderGrant);
        Assert.False(result.CanReadPrivateArtifact);
        Assert.False(result.CertifiesTenantSecurity);
    }

    [Fact]
    public async Task ValidDtoRequiresExternalAuthenticatedAdapterAndCas()
    {
        var source = new Source();
        var result = await new C1004CacheJobsApplicationService(source).PrepareAsync(Work());
        Assert.Equal("AUTHENTICATED_ADAPTER_AND_EXCLUSIVE_CAS_REQUIRED", result.Code);
        Assert.Equal(1, source.Reads);
        AssertNoEffects(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ProviderExceptionsBecomeSafeCodeWithoutLeakingPrivateErrors(int mode)
    {
        var source = new Source { Failure = mode };
        var result = await new C1004CacheJobsApplicationService(source).PrepareAsync(Work());
        Assert.Equal("VERIFIED_SOURCE_UNAVAILABLE", result.Code);
        Assert.DoesNotContain(Secret, result.Code);
        Assert.Equal(1, source.Reads);
        AssertNoEffects(result);
    }

    [Fact]
    public async Task SourceCancellationIsPropagatedRatherThanSwallowed()
    {
        var source = new Source { Failure = 3 };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new C1004CacheJobsApplicationService(source).PrepareAsync(Work()));
        Assert.Equal(1, source.Reads);
    }

    [Fact]
    public async Task MalformedDtoDoesNotQueryAnyProvider()
    {
        var source = new Source();
        var service = new C1004CacheJobsApplicationService(source);
        var invalid = new C1004CacheJobsApplicationService.Request?[]
        {
            null,
            Work() with { WorkId = "../private" },
            Work() with { Operation = "https://provider.example" },
            Work() with { IdempotencyKey = "" },
            Work() with { ExpectedVersion = 0 }
        };
        foreach (var request in invalid)
        {
            var result = await service.PrepareAsync(request);
            Assert.Equal("REQUEST_INVALID", result.Code);
            AssertNoEffects(result);
        }
        Assert.Equal(0, source.Reads);
    }

    [Fact]
    public async Task ForgedTenantHintDeniedWithNoProviderGrant()
    {
        var result = await new C1004CacheJobsApplicationService(new Source()).PrepareAsync(
            Work() with { ClientTenantHint = 200 });
        Assert.Equal("CLIENT_TENANT_OVERRIDE_DENIED", result.Code);
        AssertNoEffects(result);
    }

    [Fact]
    public async Task MissingOrRevokedEvidenceFailsClosed()
    {
        var source = new Source { Snapshot = null };
        var service = new C1004CacheJobsApplicationService(source);
        var missing = await service.PrepareAsync(Work());
        Assert.Equal("VERIFIED_BACKEND_CONTEXT_REQUIRED", missing.Code);
        AssertNoEffects(missing);
        source.Snapshot = Good() with { PermissionRevoked = true };
        var revoked = await service.PrepareAsync(Work());
        Assert.Equal("ACTIVE_MEMBERSHIP_AND_OPERATION_PERMISSION_REQUIRED", revoked.Code);
        AssertNoEffects(revoked);
    }
}
