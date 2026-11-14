using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C00-02.M05.03: integration of trusted queue intent with the in-memory V5
/// state provider. Neither component acquires an institutional Sheets lease.
/// </summary>
public sealed class V5C0002M0503ProviderIntegrationTests
{
    private const string Item = "C00-02.M05.03";
    private const string Author = "6adbb9fff1548191a1bd5b30d9e9984a";
    private const string Other = "6af16bbad26881918c283abe06e1f32a";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Area = "backend/v5-workitem-m05-provider";

    private static readonly IReadOnlySet<string> Catalog = new HashSet<string> { Item };
    private static readonly IReadOnlySet<string> Actors = new HashSet<string> { Author, Other };
    private static readonly IReadOnlySet<string> Areas = new HashSet<string> { Area };

    private static V5WorkItemMemoryAdapter Provider() =>
        new(new[] { new V5WorkItemRules.Snapshot(Item, "PENDIENTE", null) },
            Catalog, Actors);

    private static V5WorkItemQueueSelector.QueueEntry QueueEntry(
        string sha = Sha, bool dependencies = true) =>
        new(64, new V5WorkItemRules.Snapshot(Item, "PENDIENTE", null),
            dependencies, true, true, true, sha, Area);

    private static V5WorkItemQueueSelector.Selection Select(
        V5WorkItemQueueSelector.QueueEntry entry,
        IReadOnlySet<string>? activelyWritten = null, string head = Sha) =>
        new V5WorkItemQueueSelector().PrepareNext(
            new[] { entry }, Author, Catalog, Actors, Areas,
            activelyWritten ?? new HashSet<string>(), head);

    [Fact]
    public void TrustedSelectionDoesNotWriteUntilProviderAppliesSingleClaim()
    {
        var provider = Provider();
        var selected = Select(QueueEntry());
        Assert.True(selected.Allowed);
        Assert.Equal("CLAIM_INTENT_ONLY", selected.Code);
        Assert.Equal(64, selected.PlanRow);
        Assert.Equal(Item, selected.ClaimIntent!.MicrotaskId);
        Assert.Equal("EN_PROGRESO", selected.ClaimIntent.RequestedState);
        Assert.Equal(Author, selected.ClaimIntent.RequestedOwnerTaskId);
        Assert.Equal("PENDIENTE", provider.Read(Item, Author).Snapshot!.State);

        var write = provider.Claim(Item, Author, "claim-v5", true, true, true, true);
        Assert.True(write.Applied);
        Assert.Equal("APPLIED_IN_MEMORY", write.Code);
        Assert.Equal("EN_PROGRESO", write.Snapshot!.State);
        Assert.Equal(Author, write.Snapshot.OwnerTaskId);

        var replay = provider.Claim(Item, Author, "claim-v5", true, true, true, true);
        Assert.True(replay.Applied);
        Assert.Equal("IDEMPOTENT_REPLAY", replay.Code);
        Assert.Equal(write.Snapshot, provider.Read(Item, Author).Snapshot);
    }

    [Fact]
    public void CompetingAuthorizedActorCannotTakeClaimOrFinishItsAuthorsWork()
    {
        var provider = Provider();
        Assert.True(provider.Claim(Item, Author, "claim-primary", true, true, true, true).Applied);
        var competing = provider.Claim(Item, Other, "claim-other", true, true, true, true);
        Assert.False(competing.Applied);
        Assert.Equal("CLAIM_CONFLICT", competing.Code);
        Assert.Equal("OWNER_OR_STATE_MISMATCH",
            provider.MarkValidating(Item, Other, "candidate-other", "blob").Code);
        Assert.Equal("EN_PROGRESO", provider.Read(Item, Author).Snapshot!.State);
        Assert.Equal(Author, provider.Read(Item, Other).Snapshot!.OwnerTaskId);
    }

    [Fact]
    public void ActiveFileAreaStaleHeadAndClosedDagNeverReachProvider()
    {
        var provider = Provider();
        var active = Select(QueueEntry(), new HashSet<string> { Area });
        Assert.False(active.Allowed);
        Assert.Equal("NO_ELIGIBLE_CANDIDATE", active.Code);

        var moved = Select(QueueEntry(), head: new string('b', 40));
        Assert.False(moved.Allowed);
        Assert.Equal("UNTRUSTED_QUEUE_SNAPSHOT", moved.Code);

        var blocked = Select(QueueEntry(dependencies: false));
        Assert.False(blocked.Allowed);
        Assert.Equal("NO_ELIGIBLE_CANDIDATE", blocked.Code);
        Assert.Equal("PENDIENTE", provider.Read(Item, Author).Snapshot!.State);
        Assert.Null(provider.Read(Item, Author).Snapshot!.OwnerTaskId);
    }
}
