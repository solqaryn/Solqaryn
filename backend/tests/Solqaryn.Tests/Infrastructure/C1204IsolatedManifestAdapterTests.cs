using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Solqaryn.Application.AutomationV5;
using Xunit;
using Adapter = Solqaryn.Infrastructure.AutomationV5.C1204IsolatedManifestAdapter;
using Marker = Solqaryn.Infrastructure.AutomationV5.C1204IsolatedManifestAdapter.Checkpoint;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1204IsolatedManifestAdapterTests
{
    private static readonly string Sha = new('a', 40);
    private static C1204VerifiedActor Actor(string tenant = "tenant-A") =>
        new("actor-01", tenant, true, true, true, true);
    private static Adapter.Draft Draft(long revision = 0, string key = "restore-plan",
        IReadOnlyList<Marker>? steps = null) =>
        new("tenant-A", key, Sha, revision,
            steps ?? new[] { Marker.SchemaInventoried, Marker.RollbackOutlined });

    [Fact]
    public void SyntheticManifestCannotGrantRuntimeOrMySqlAuthority()
    {
        var adapter = new Adapter();
        var result = adapter.Stage(Actor(), Draft());
        Assert.Equal("SYNTHETIC_MANIFEST_ONLY_NO_MYSQL_PROOF", result.Code);
        Assert.Equal(1, result.Revision);
        Assert.Equal(new[] { Marker.SchemaInventoried, Marker.RollbackOutlined },
            result.Data!.Checkpoints);
        Assert.False(result.IsAuthoritativeEvidence);
        Assert.False(result.CertifiesMySqlRestore);
        Assert.False(result.GrantsSqlOrMigration);
        Assert.False(result.AuthorizesCutover);
        Assert.False(result.AuthorizesQaOrProduction);
        Assert.Equal(1, adapter.Read(Actor(), "tenant-A", "restore-plan", Sha).Revision);
    }

    [Fact]
    public void TenantBAndDifferentActorCannotReadOrOverwriteTenantA()
    {
        var adapter = new Adapter();
        adapter.Stage(Actor(), Draft());
        Assert.Equal("CURRENT_SERVER_ACTOR_REQUIRED",
            adapter.Read(Actor("tenant-B"), "tenant-A", "restore-plan", Sha).Code);
        Assert.Equal("ISOLATED_MANIFEST_NOT_FOUND",
            adapter.Read(Actor("tenant-B"), "tenant-B", "restore-plan", Sha).Code);
        Assert.Equal("ISOLATED_MANIFEST_NOT_FOUND",
            adapter.Read(Actor() with { ActorId = "other" }, "tenant-A", "restore-plan", Sha).Code);
        Assert.Equal("CURRENT_SERVER_ACTOR_REQUIRED",
            adapter.Stage(Actor("tenant-B"), Draft()).Code);
    }

    [Fact]
    public void RevokedOrUnverifiedActorCannotAccessStoredManifest()
    {
        var adapter = new Adapter();
        adapter.Stage(Actor(), Draft());
        var denied = new[]
        {
            Actor() with { JwtAuthenticatedByServer = false },
            Actor() with { MembershipCurrent = false },
            Actor() with { MembershipRevoked = true },
            Actor() with { CanReviewCutover = false },
            Actor() with { RecentMfaVerified = false },
            Actor() with { ActorId = "invalid\nactor" }
        };
        foreach (var actor in denied)
        {
            Assert.Equal("CURRENT_SERVER_ACTOR_REQUIRED",
                adapter.Read(actor, "tenant-A", "restore-plan", Sha).Code);
            Assert.Equal("CURRENT_SERVER_ACTOR_REQUIRED",
                adapter.Stage(actor, Draft(1)).Code);
        }
    }

    [Fact]
    public void RevisionAndShaRequireExactMatchWithNoRollbackByReplay()
    {
        var adapter = new Adapter();
        Assert.Equal(1, adapter.Stage(Actor(), Draft()).Revision);
        Assert.Equal("ISOLATED_REVISION_CONFLICT", adapter.Stage(Actor(), Draft()).Code);
        Assert.Equal("ISOLATED_REVISION_CONFLICT", adapter.Stage(Actor(), Draft(9)).Code);
        Assert.Equal(2, adapter.Stage(Actor(), Draft(1)).Revision);
        Assert.Equal("ISOLATED_REVISION_CONFLICT", adapter.Stage(Actor(), Draft(1)).Code);
        Assert.Equal("ISOLATED_MANIFEST_NOT_FOUND",
            adapter.Read(Actor(), "tenant-A", "restore-plan", new string('b', 40)).Code);
        Assert.Equal("ISOLATED_REVISION_CONFLICT",
            adapter.Stage(Actor(), Draft(2) with { ExpectedSourceSha = new string('b', 40) }).Code);
    }

    [Fact]
    public void CallerMutationsAndOutputCastCannotChangeSnapshot()
    {
        var adapter = new Adapter();
        var input = new List<Marker> { Marker.SchemaInventoried };
        adapter.Stage(Actor(), Draft(steps: input));
        input[0] = Marker.RollbackOutlined;
        var read = adapter.Read(Actor(), "tenant-A", "restore-plan", Sha);
        Assert.Equal(new[] { Marker.SchemaInventoried }, read.Data!.Checkpoints);
        Assert.Throws<NotSupportedException>(() =>
        {
            ((IList<Marker>)read.Data.Checkpoints)[0] = Marker.RollbackOutlined;
        });
        Assert.Equal(new[] { Marker.SchemaInventoried },
            adapter.Read(Actor(), "tenant-A", "restore-plan", Sha).Data!.Checkpoints);
    }

    [Fact]
    public void InvalidOrDuplicateMarkersFailClosed()
    {
        var adapter = new Adapter();
        Assert.Equal("INVALID_ISOLATED_MANIFEST",
            adapter.Stage(Actor(), Draft(steps: new[] { Marker.SchemaInventoried, Marker.SchemaInventoried })).Code);
        Assert.Equal("INVALID_ISOLATED_MANIFEST",
            adapter.Stage(Actor(), Draft(steps: new[] { (Marker)99 })).Code);
        Assert.Equal("INVALID_ISOLATED_MANIFEST",
            adapter.Stage(Actor(), Draft(steps: Array.Empty<Marker>())).Code);
        Assert.Equal("INVALID_ISOLATED_MANIFEST",
            adapter.Stage(Actor(), Draft() with { ExpectedSourceSha = "invalid" }).Code);
        Assert.Equal("INVALID_ISOLATED_MANIFEST",
            adapter.Stage(Actor(), Draft(key: "../escape")).Code);
    }

    [Fact]
    public async Task ConcurrentWritersWithSameRevisionHaveSingleWinner()
    {
        var adapter = new Adapter();
        adapter.Stage(Actor(), Draft());
        var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            adapter.Stage(Actor(), Draft(1, steps: new[] { Marker.SyntheticCountsCompared })))).ToArray();
        var results = await Task.WhenAll(tasks);
        Assert.Equal(1, results.Count(x => x.Revision == 2));
        Assert.Equal(7, results.Count(x => x.Code == "ISOLATED_REVISION_CONFLICT"));
    }

    [Fact]
    public void CapacityIsTenantBoundedAndDoesNotEvictExisting()
    {
        var adapter = new Adapter();
        for (var i = 0; i < 32; i++)
            Assert.Equal(1, adapter.Stage(Actor(), Draft(key: "manifest-" + i)).Revision);
        Assert.Equal("ISOLATED_TENANT_CAPACITY_REACHED",
            adapter.Stage(Actor(), Draft(key: "manifest-33")).Code);
        Assert.Equal(1, adapter.Read(Actor(), "tenant-A", "manifest-0", Sha).Revision);
        Assert.Equal(1, adapter.Stage(Actor("tenant-B"),
            Draft(key: "separate-1") with { TenantId = "tenant-B" }).Revision);
    }

    [Fact]
    public void CancellationIsNotReportedAsSuccess()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var adapter = new Adapter();
        Assert.Throws<OperationCanceledException>(() => adapter.Stage(Actor(), Draft(), cts.Token));
        Assert.Throws<OperationCanceledException>(() =>
            adapter.Read(Actor(), "tenant-A", "restore-plan", Sha, cts.Token));
    }
}
