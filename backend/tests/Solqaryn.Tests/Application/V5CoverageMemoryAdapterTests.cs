using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5CoverageMemoryAdapterTests
{
    private const string Owner = "6af16bb9492c8191b8e8c7d606f11507";
    private const string Outside = "6adbbc9ad9e88191bea8eb69104afb25";
    private static readonly IReadOnlySet<string> Actors = new HashSet<string> { Owner };
    private static readonly IReadOnlySet<string> Scopes = new HashSet<string> { "C00-03" };
    private static readonly IReadOnlySet<string> Requirements = new HashSet<string> { "R01" };
    private static readonly IReadOnlySet<string> Items = new HashSet<string> { "C00-03.M03.04" };

    private static V5RequirementCoverageRules.Entry Entry() =>
        new("R01", "C00-03.M03.04", "V5 FINAL", "contract", "test", "evidence");

    private static V5CoverageMemoryAdapter.Result Save(V5CoverageMemoryAdapter adapter,
        long version = 0, string key = "evt-01", string actor = Owner,
        string scope = "C00-03", V5RequirementCoverageRules.Entry? item = null) =>
        adapter.TrySave(actor, scope, item ?? Entry(), version, key,
            Actors, Scopes, Requirements, Items);

    private static V5CoverageMemoryAdapter.ReadResult Read(V5CoverageMemoryAdapter adapter,
        string actor = Owner, string scope = "C00-03") =>
        adapter.TryRead(actor, scope, "R01", "C00-03.M03.04",
            "V5 FINAL", Actors, Scopes);

    [Fact]
    public void CreatesAndReadsIsolatedMappingWithRevisionOne()
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.Equal("SAVED", Save(adapter).Code);
        var read = Read(adapter);
        Assert.Equal("FOUND", read.Code);
        Assert.Equal(1, read.Item!.Revision);
        Assert.Equal(Entry(), read.Item.Entry);
    }

    [Fact]
    public void RepeatedSameIdempotencyKeyDoesNotApplyASecondWrite()
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.Equal("SAVED", Save(adapter).Code);
        var repeated = Save(adapter);
        Assert.Equal("IDEMPOTENT_REPLAY", repeated.Code);
        Assert.Equal(1, repeated.Revision);
    }

    [Fact]
    public void ConflictingIdempotencyKeyIsRejected()
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.True(Save(adapter).Accepted);
        var changed = Entry() with { EvidenceReference = "different" };
        Assert.Equal("IDEMPOTENCY_CONFLICT",
            Save(adapter, 1, "evt-01", item: changed).Code);
        Assert.Equal(Entry(), Read(adapter).Item!.Entry);
    }

    [Fact]
    public void StaleRevisionCannotOverwriteCurrentMapping()
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.True(Save(adapter).Accepted);
        var attempted = Entry() with { EvidenceReference = "new-evidence" };
        Assert.Equal("REVISION_CONFLICT",
            Save(adapter, 0, "evt-02", item: attempted).Code);
        Assert.Equal("SAVED", Save(adapter, 1, "evt-02", item: attempted).Code);
        Assert.Equal(2, Read(adapter).Item!.Revision);
    }

    [Fact]
    public void UnknownActorAndForeignScopeAreDeniedForWritesAndReads()
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.Equal("UNAUTHORIZED_ACTOR", Save(adapter, actor: Outside).Code);
        Assert.Equal("SCOPE_DENIED", Save(adapter, scope: "C99-99").Code);
        Assert.Equal("UNAUTHORIZED_ACTOR", Read(adapter, actor: Outside).Code);
        Assert.Equal("SCOPE_DENIED", Read(adapter, scope: "C99-99").Code);
        Assert.Null(Read(adapter).Item);
    }

    [Fact]
    public void UnknownCatalogEntryCannotBeStored()
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.Equal("UNKNOWN_REQUIREMENT",
            Save(adapter, item: Entry() with { RequirementKey = "unlisted" }).Code);
        Assert.Null(Read(adapter).Item);
    }

    [Fact]
    public async Task ConcurrentFirstWritersHaveExactlyOneWinner()
    {
        var adapter = new V5CoverageMemoryAdapter();
        var writes = await Task.WhenAll(Enumerable.Range(0, 32)
            .Select(i => Task.Run(() => Save(adapter, 0, $"evt-{i + 10}"))));
        Assert.Single(writes.Where(x => x.Accepted));
        Assert.Equal(31, writes.Count(x => x.Code == "REVISION_CONFLICT"));
        Assert.Equal(1, Read(adapter).Item!.Revision);
    }

    [Fact]
    public void CapacityIsBoundedAndDoesNotCreateExtraKeys()
    {
        var adapter = new V5CoverageMemoryAdapter(capacity: 1);
        Assert.True(Save(adapter).Accepted);
        Assert.Equal("CAPACITY_REACHED",
            Save(adapter, 1, "evt-02", item: Entry() with { EvidenceReference = "v2" }).Code);
        Assert.Equal(1, Read(adapter).Item!.Revision);
    }

    [Fact]
    public void InvalidPreconditionsFailClosed()
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.Equal("INVALID_WRITE_PRECONDITION", Save(adapter, -1).Code);
        Assert.Equal("INVALID_WRITE_PRECONDITION", Save(adapter, key: " ").Code);
        Assert.Equal("NOT_FOUND", Read(adapter).Code);
    }
}
