using System.Collections.Generic;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1304VisualReviewSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTimeOffset Now =
        new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Source : IC1304VisualReviewAuthoritySource
    {
        public C1304VisualReviewAuthorityAdapter.Authority? Current = Good();
        public Exception? Failure;
        public int Calls;
        public Task<C1304VisualReviewAuthorityAdapter.Authority?> ReadCurrentAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref Calls);
            if (Failure is not null) throw Failure;
            return Task.FromResult(Current);
        }
    }

    private static C1304VisualReviewAuthorityAdapter.Authority Good() =>
        new(8, 11, true, true, true, false, true, Sha, Now.AddMinutes(5),
            new HashSet<string>(StringComparer.Ordinal)
            { "private-shell", "visual-audit" });

    private static (C1304VisualReviewSnapshotAdapter Adapter, Source Source) Setup()
    {
        var source = new Source();
        return (new C1304VisualReviewSnapshotAdapter(source, new Clock()), source);
    }

    [Fact]
    public async Task SaveAndReadProvideOnlyStagedSnapshotNotRuntimeAuthority()
    {
        var (adapter, source) = Setup();
        var saved = await adapter.WriteAsync(11, "builder-review", Sha, 0,
            new[] { "private-shell" });
        Assert.Equal("SNAPSHOT_STAGED_BROWSER_A5_REQUIRED", saved.Code);
        Assert.Equal(1, saved.Revision);
        Assert.Equal(11, saved.Data!.TenantId);
        Assert.Equal(Sha, saved.Data.SourceSha);
        Assert.False(saved.GrantsJwtOrRbac);
        Assert.False(saved.GrantsTenantAccess);
        Assert.False(saved.CertifiesBrowserReview);
        Assert.False(saved.PersistsDurably);
        Assert.False(saved.AuthorizesQaOrProduction);
        var read = await adapter.ReadAsync(11, "builder-review", Sha);
        Assert.Equal(1, read.Revision);
        Assert.Equal(new[] { "private-shell" }, read.Data!.Surfaces);
        Assert.Equal(2, source.Calls);
    }

    [Fact]
    public async Task SequentialRevisionsRequireExactCompareAndSwap()
    {
        var (adapter, _) = Setup();
        Assert.Equal(1, (await adapter.WriteAsync(11, "builder-review", Sha, 0,
            new[] { "private-shell" })).Revision);
        Assert.Equal("SNAPSHOT_REVISION_CONFLICT",
            (await adapter.WriteAsync(11, "builder-review", Sha, 0,
                new[] { "visual-audit" })).Code);
        var updated = await adapter.WriteAsync(11, "builder-review", Sha, 1,
            new[] { "visual-audit" });
        Assert.Equal(2, updated.Revision);
        Assert.Equal(new[] { "visual-audit" },
            (await adapter.ReadAsync(11, "builder-review", Sha)).Data!.Surfaces);
        Assert.Equal("SNAPSHOT_REVISION_CONFLICT",
            (await adapter.WriteAsync(11, "builder-review", Sha, 1,
                new[] { "private-shell" })).Code);
    }

    [Fact]
    public async Task TenantBHasNoVisibilityOfTenantAAndCannotForgeIt()
    {
        var (adapter, source) = Setup();
        await adapter.WriteAsync(11, "builder-review", Sha, 0,
            new[] { "private-shell" });
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.ReadAsync(22, "builder-review", Sha)).Code);
        source.Current = Good() with { TenantId = 22 };
        Assert.Equal("SNAPSHOT_NOT_FOUND",
            (await adapter.ReadAsync(22, "builder-review", Sha)).Code);
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.ReadAsync(11, "builder-review", Sha)).Code);
        Assert.Equal(1, (await adapter.WriteAsync(22, "builder-review", Sha, 0,
            new[] { "private-shell" })).Revision);
    }

    [Fact]
    public async Task RevokedAndUntrustedAuthorizationsRejectExistingSnapshot()
    {
        var (adapter, source) = Setup();
        await adapter.WriteAsync(11, "builder-review", Sha, 0,
            new[] { "private-shell" });
        foreach (var bad in new[]
        {
            Good() with { RoleRevoked = true },
            Good() with { MembershipCurrent = false },
            Good() with { ExplicitReviewPermission = false },
            Good() with { SourceAuthenticated = false },
            Good() with { IdentityVerified = false }
        })
        {
            source.Current = bad;
            var r = await adapter.ReadAsync(11, "builder-review", Sha);
            Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED", r.Code);
            Assert.Null(r.Data);
        }
    }

    [Fact]
    public async Task ShaMismatchAndExpiredAuthorityDenyWithoutReadingPayload()
    {
        var (adapter, source) = Setup();
        await adapter.WriteAsync(11, "builder-review", Sha, 0,
            new[] { "private-shell" });
        source.Current = Good() with { SourceSha = Sha.ToUpperInvariant() };
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.ReadAsync(11, "builder-review", Sha)).Code);
        source.Current = Good() with { ValidUntilUtc = Now };
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.ReadAsync(11, "builder-review", Sha)).Code);
        source.Current = Good() with { ValidUntilUtc = Now.AddMinutes(16) };
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.ReadAsync(11, "builder-review", Sha)).Code);
    }

    [Fact]
    public async Task SurfaceAllowlistIsRevalidatedAndSensitiveKeysAreRejected()
    {
        var (adapter, source) = Setup();
        Assert.Equal("SURFACE_NOT_APPROVED_BY_CURRENT_AUTHORITY",
            (await adapter.WriteAsync(11, "builder-review", Sha, 0,
                new[] { "not-approved" })).Code);
        Assert.Equal("INVALID_SNAPSHOT_SCOPE",
            (await adapter.WriteAsync(11, "builder-review", Sha, 0,
                new[] { "private-shell", "private-shell" })).Code);
        await adapter.WriteAsync(11, "builder-review", Sha, 0,
            new[] { "private-shell" });
        source.Current = Good() with {
            ServerApprovedSurfaces = new HashSet<string> { "visual-audit" }
        };
        Assert.Equal("SURFACE_NOT_APPROVED_BY_CURRENT_AUTHORITY",
            (await adapter.ReadAsync(11, "builder-review", Sha)).Code);
        source.Current = Good() with {
            ServerApprovedSurfaces = new HashSet<string> { "visual-audit\nunsafe" }
        };
        Assert.Equal("CURRENT_VERIFIED_VISUAL_REVIEW_AUTHORITY_REQUIRED",
            (await adapter.ReadAsync(11, "builder-review", Sha)).Code);
    }

    [Fact]
    public async Task ReturnedCollectionsCannotMutateStoredSnapshot()
    {
        var (adapter, _) = Setup();
        var input = new[] { "private-shell" };
        var saved = await adapter.WriteAsync(11, "builder-review", Sha, 0, input);
        input[0] = "visual-audit";
        var mutable = Assert.IsAssignableFrom<IList<string>>(saved.Data!.Surfaces);
        Assert.Throws<NotSupportedException>(() => mutable[0] = "visual-audit");
        Assert.Equal(new[] { "private-shell" },
            (await adapter.ReadAsync(11, "builder-review", Sha)).Data!.Surfaces);
    }

    [Fact]
    public async Task MalformedScopesFailBeforeConsultingTrustedSource()
    {
        var (adapter, source) = Setup();
        Assert.Equal("INVALID_SNAPSHOT_SCOPE",
            (await adapter.WriteAsync(0, "builder-review", Sha, 0,
                new[] { "private-shell" })).Code);
        Assert.Equal("INVALID_SNAPSHOT_SCOPE",
            (await adapter.WriteAsync(11, "bad/key", Sha, 0,
                new[] { "private-shell" })).Code);
        Assert.Equal("INVALID_SNAPSHOT_SCOPE",
            (await adapter.WriteAsync(11, "builder-review", "not-sha", 0,
                new[] { "private-shell" })).Code);
        Assert.Equal("INVALID_SNAPSHOT_SCOPE",
            (await adapter.WriteAsync(11, "builder-review", Sha, -1,
                new[] { "private-shell" })).Code);
        Assert.Equal("INVALID_SNAPSHOT_SCOPE",
            (await adapter.ReadAsync(11, "../secret", Sha)).Code);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task ExternalSourceErrorsRemainOpaque()
    {
        var (adapter, source) = Setup();
        source.Failure = new InvalidOperationException("private-token-and-password");
        var result = await adapter.WriteAsync(11, "builder-review", Sha, 0,
            new[] { "private-shell" });
        Assert.Equal("CURRENT_AUTHORITY_UNAVAILABLE", result.Code);
        Assert.DoesNotContain("private-token", result.Code);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task CancellationIsPropagatedAndCannotWrite()
    {
        var (adapter, source) = Setup();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            adapter.WriteAsync(11, "builder-review", Sha, 0,
                new[] { "private-shell" }, cts.Token));
        Assert.Equal(0, source.Calls);
        Assert.Equal("SNAPSHOT_NOT_FOUND",
            (await adapter.ReadAsync(11, "builder-review", Sha)).Code);
    }

    [Fact]
    public async Task ConcurrentSameRevisionHasExactlyOneWinner()
    {
        var (adapter, _) = Setup();
        await adapter.WriteAsync(11, "builder-review", Sha, 0,
            new[] { "private-shell" });
        var contenders = await Task.WhenAll(
            Task.Run(() => adapter.WriteAsync(11, "builder-review", Sha, 1,
                new[] { "visual-audit" })),
            Task.Run(() => adapter.WriteAsync(11, "builder-review", Sha, 1,
                new[] { "private-shell" })));
        Assert.Single(contenders.Where(x => x.Revision == 2));
        Assert.Single(contenders.Where(x => x.Code == "SNAPSHOT_REVISION_CONFLICT"));
        Assert.Equal(2, (await adapter.ReadAsync(11, "builder-review", Sha)).Revision);
    }

    [Fact]
    public async Task TenantCapacityIsBoundedButDoesNotBlockOtherTenants()
    {
        var (adapter, source) = Setup();
        for (var i = 0; i < 64; i++)
        {
            var r = await adapter.WriteAsync(11, "key-" + i, Sha, 0,
                new[] { "private-shell" });
            Assert.Equal(1, r.Revision);
        }
        Assert.Equal("TENANT_SNAPSHOT_CAPACITY_REACHED",
            (await adapter.WriteAsync(11, "extra-key", Sha, 0,
                new[] { "private-shell" })).Code);
        source.Current = Good() with { TenantId = 22 };
        Assert.Equal(1, (await adapter.WriteAsync(22, "extra-key", Sha, 0,
            new[] { "private-shell" })).Revision);
    }
}
