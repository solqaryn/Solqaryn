using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Adapter = Solqaryn.Infrastructure.AutomationV5.C1204IsolatedManifestAdapter;
using Worker = Solqaryn.Infrastructure.AutomationV5.C1204SyntheticManifestReviewWorker;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1204SyntheticManifestReviewWorkerTests
{
    private static readonly string Sha = new('a', 40);
    private static C1204VerifiedActor Actor(string tenant = "tenant-A") =>
        new("actor-01", tenant, true, true, true, true);
    private static Worker.Ticket Ticket(string key = "restore-one", string tenant = "tenant-A",
        long revision = 1) => new(tenant, key, Sha, revision);

    private static Adapter Seed(string tenant = "tenant-A", params string[] keys)
    {
        var adapter = new Adapter();
        foreach (var key in keys)
        {
            var staged = adapter.Stage(Actor(tenant),
                new Adapter.Draft(tenant, key, Sha, 0,
                    new[] { Adapter.Checkpoint.SchemaInventoried,
                            Adapter.Checkpoint.RollbackOutlined }));
            Assert.Equal(1, staged.Revision);
        }
        return adapter;
    }

    private static Worker Work(Adapter adapter) =>
        new(new C1204SyntheticManifestReadPort(adapter));

    private sealed class Spy : IC1204SyntheticManifestReadPort
    {
        public int Calls;
        public bool Throw;
        public Adapter.Result? Forged;
        public ValueTask<Adapter.Result> ReadAsync(C1204VerifiedActor actor, string tenant,
            string key, string sourceSha, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Throw) throw new InvalidOperationException("PRIVATE_SQL_PASSWORD");
            return ValueTask.FromResult(Forged ?? new Adapter.Result(
                "ISOLATED_MANIFEST_NOT_FOUND", 0, null));
        }
    }

    [Fact]
    public async Task RealIsolatedAdapterReadForTwoTicketsIsNonAuthoritative()
    {
        var adapter = Seed("tenant-A", "restore-one", "restore-two");
        var worker = Work(adapter);
        var result = await worker.ReviewAsync(Actor(),
            new[] { Ticket("restore-one"), Ticket("restore-two") });
        Assert.Equal("SYNTHETIC_MANIFEST_REVIEW_ONLY_NO_MYSQL_PROOF", result.Code);
        Assert.Equal(2, result.Examined);
        Assert.False(result.CertifiesMysqlBackupOrRestore);
        Assert.False(result.AuthorizesSqlOrCutover);
        Assert.False(result.GrantsTenantAccess);
        Assert.False(result.ExecutesHostedWorker);
        Assert.False(result.WritesPersistentData);
        Assert.False(result.AuthorizesQaOrProduction);
        Assert.Equal(1, adapter.Read(Actor(), "tenant-A", "restore-one", Sha).Revision);
    }

    [Fact]
    public async Task TenantBCannotReadTenantAAndNoPartialSuccessLeaks()
    {
        var spy = new Spy();
        var worker = new Worker(spy);
        var result = await worker.ReviewAsync(Actor(),
            new[] { Ticket("restore-one"), Ticket("restore-two", "tenant-B") });
        Assert.Equal("INVALID_OR_CROSS_TENANT_MANIFEST", result.Code);
        Assert.Equal(0, spy.Calls);
        Assert.Equal(0, result.Examined);
    }

    [Fact]
    public async Task RevokedJwtMembershipRoleOrMfaPreventsAnySourceCalls()
    {
        var spy = new Spy();
        var actor = Actor();
        foreach (var denied in new[] {
            actor with { JwtAuthenticatedByServer = false },
            actor with { MembershipCurrent = false },
            actor with { MembershipRevoked = true },
            actor with { CanReviewCutover = false },
            actor with { RecentMfaVerified = false },
            actor with { TenantId = "tenant-B" }
        })
        {
            var result = await new Worker(spy).ReviewAsync(denied, new[] { Ticket() });
            Assert.NotEqual("SYNTHETIC_MANIFEST_REVIEW_ONLY_NO_MYSQL_PROOF", result.Code);
            Assert.False(result.GrantsTenantAccess);
        }
        Assert.Equal(0, spy.Calls);
    }

    [Fact]
    public async Task ForgedShaDuplicateKeyAndStaleRevisionNeverRead()
    {
        var spy = new Spy();
        var worker = new Worker(spy);
        var cases = new IReadOnlyList<Worker.Ticket>[] {
            new[] { Ticket() with { SourceSha = new string('A', 40) } },
            new[] { Ticket(), Ticket() },
            new[] { Ticket() with { Revision = 0 } },
            new[] { Ticket() with { Revision = 1000001 } },
            new[] { Ticket() with { Key = "../private" } },
            new[] { Ticket() with { SourceSha = "short" } }
        };
        foreach (var batch in cases)
            Assert.NotEqual("SYNTHETIC_MANIFEST_REVIEW_ONLY_NO_MYSQL_PROOF",
                (await worker.ReviewAsync(Actor(), batch)).Code);
        Assert.Equal(0, spy.Calls);
    }

    [Fact]
    public async Task ZeroAndNineTicketsRejectBeforeTouchingSource()
    {
        var spy = new Spy();
        var worker = new Worker(spy);
        Assert.Equal("INVALID_SYNTHETIC_MANIFEST_BATCH",
            (await worker.ReviewAsync(Actor(), Array.Empty<Worker.Ticket>())).Code);
        var large = Enumerable.Range(0, 9).Select(i => Ticket("restore-" + i)).ToArray();
        Assert.Equal("INVALID_SYNTHETIC_MANIFEST_BATCH",
            (await worker.ReviewAsync(Actor(), large)).Code);
        Assert.Equal(0, spy.Calls);
    }

    [Fact]
    public async Task WrongActualRevisionRejectsAfterRealAdapterRead()
    {
        var adapter = Seed("tenant-A", "restore-one");
        var result = await Work(adapter).ReviewAsync(Actor(), new[] { Ticket(revision: 2) });
        Assert.Equal("SYNTHETIC_MANIFEST_NOT_VERIFIED", result.Code);
        Assert.Equal(0, result.Examined);
    }

    [Fact]
    public async Task UntrustedProviderDataDoesNotGrantAnyPermission()
    {
        var spy = new Spy { Forged = new Adapter.Result("SYNTHETIC_MANIFEST_ONLY_NO_MYSQL_PROOF", 1,
            new Adapter.Manifest("tenant-B", "restore-one", Sha, 1,
                Array.AsReadOnly(new[] { Adapter.Checkpoint.RollbackOutlined }))) };
        var result = await new Worker(spy).ReviewAsync(Actor(), new[] { Ticket() });
        Assert.Equal("SYNTHETIC_MANIFEST_NOT_VERIFIED", result.Code);
        Assert.False(result.AuthorizesSqlOrCutover);
        Assert.Equal(1, spy.Calls);
    }

    [Fact]
    public async Task SourceFailureIsOpaqueAndNeverReturnsSqlSecrets()
    {
        var spy = new Spy { Throw = true };
        var result = await new Worker(spy).ReviewAsync(Actor(), new[] { Ticket() });
        Assert.Equal("SYNTHETIC_SOURCE_UNAVAILABLE", result.Code);
        Assert.DoesNotContain("PASSWORD", result.Code);
        Assert.Equal(0, result.Examined);
    }

    [Fact]
    public async Task CancellationBeforeSourceReadCannotClaimPositive()
    {
        var spy = new Spy();
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Worker(spy).ReviewAsync(Actor(), new[] { Ticket() }, cancel.Token));
        Assert.Equal(0, spy.Calls);
    }

    [Fact]
    public void MissingAdapterOrPortFailsClosed()
    {
        Assert.Throws<ArgumentNullException>(() => new C1204SyntheticManifestReadPort(null!));
        Assert.Throws<ArgumentNullException>(() => new Worker(null!));
    }
}
