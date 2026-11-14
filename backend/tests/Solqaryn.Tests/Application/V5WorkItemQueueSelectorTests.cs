using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5WorkItemQueueSelectorTests
{
    private const string First = "C00-02.M03.05";
    private const string Second = "C00-03.M03.05";
    private const string Author = "6aeacb7820ac8191b7d768a6f33dfa09";
    private const string Other = "6af16bb9492c8191b8e8c7d606f11507";
    private const string Outsider = "6adbbc9ad9e88191bea8eb69104afb25";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly IReadOnlySet<string> Catalog = new HashSet<string> { First, Second };
    private static readonly IReadOnlySet<string> Actors = new HashSet<string> { Author, Other };
    private static readonly IReadOnlySet<string> Areas = new HashSet<string> { "backend/worker", "backend/coverage" };
    private readonly V5WorkItemQueueSelector _selector = new();

    private static V5WorkItemQueueSelector.QueueEntry Item(string id = First, int row = 54,
        bool g = true, bool o = true, bool dag = true, bool gate = true,
        string? owner = null, string state = "PENDIENTE",
        string area = "backend/worker", string sha = Sha) =>
        new(row, new V5WorkItemRules.Snapshot(id, state, owner),
            g, o, dag, gate, sha, area);

    private V5WorkItemQueueSelector.Selection Select(
        IEnumerable<V5WorkItemQueueSelector.QueueEntry> items,
        string actor = Author, string sha = Sha,
        IReadOnlySet<string>? active = null, int capacity = 1000) =>
        _selector.PrepareNext(items, actor, Catalog, Actors, Areas,
            active ?? new HashSet<string>(), sha, capacity);

    [Fact]
    public void SelectsOldestEligibleWithoutExecutingOrModifyingTheSnapshot()
    {
        var first = Item();
        var result = Select(new[] { Item(Second, 90, area: "backend/coverage"), first });
        Assert.True(result.Allowed);
        Assert.Equal(54, result.PlanRow);
        Assert.Equal(First, result.ClaimIntent!.MicrotaskId);
        Assert.Equal("PENDIENTE", result.ClaimIntent.ExpectedState);
        Assert.Equal("EN_PROGRESO", result.ClaimIntent.RequestedState);
        Assert.Equal(Author, result.ClaimIntent.RequestedOwnerTaskId);
        Assert.Null(first.WorkItem.OwnerTaskId);
        Assert.Equal("PENDIENTE", first.WorkItem.State);
    }

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public void ClosedGatesSkipToIndependentCandidate(bool g, bool o, bool dag, bool gate)
    {
        var result = Select(new[] { Item(g: g, o: o, dag: dag, gate: gate),
            Item(Second, 90, area: "backend/coverage") });
        Assert.True(result.Allowed);
        Assert.Equal(Second, result.ClaimIntent!.MicrotaskId);
    }

    [Fact]
    public void ActiveFileAreaAndOccupiedOwnerDoNotDispatch()
    {
        var result = Select(new[] { Item(), Item(Second, 90, area: "backend/coverage") },
            active: new HashSet<string> { "backend/worker" });
        Assert.Equal(Second, result.ClaimIntent!.MicrotaskId);
        var occupied = Select(new[] { Item(owner: Other), Item(Second, 90,
            state: "VALIDANDO", area: "backend/coverage") });
        Assert.False(occupied.Allowed);
        Assert.Equal("NO_ELIGIBLE_CANDIDATE", occupied.Code);
    }

    [Fact]
    public void UnauthorizedActorNeverObtainsClaimIntent()
    {
        var result = Select(new[] { Item() }, actor: Outsider);
        Assert.Equal("UNAUTHORIZED_ACTOR", result.Code);
        Assert.Null(result.ClaimIntent);
    }

    [Fact]
    public void DuplicatesAndStaleUntrustedDataRejectWholeBatch()
    {
        Assert.Equal("UNTRUSTED_QUEUE_SNAPSHOT",
            Select(new[] { Item(), Item(First, 90) }).Code);
        Assert.Equal("UNTRUSTED_QUEUE_SNAPSHOT",
            Select(new[] { Item(), Item(Second, 54, area: "backend/coverage") }).Code);
        Assert.Equal("UNTRUSTED_QUEUE_SNAPSHOT",
            Select(new[] { Item(sha: new string('a', 40)) }).Code);
        Assert.Equal("UNTRUSTED_QUEUE_SNAPSHOT",
            Select(new[] { Item(area: "untrusted") }).Code);
        Assert.Equal("UNTRUSTED_QUEUE_SNAPSHOT",
            Select(new[] { Item("C00-05.M03.05") }).Code);
        Assert.Equal("CHECK_SHA_MISMATCH", Select(new[] { Item() }, sha: "short").Code);
    }

    [Fact]
    public void BoundedCapacityEmptyQueueAndInvalidCapacityFailClosed()
    {
        Assert.Equal("CAPACITY_REACHED",
            Select(new[] { Item(), Item(Second, 90, area: "backend/coverage") },
                capacity: 1).Code);
        Assert.Equal("INVALID_CAPACITY", Select(new[] { Item() }, capacity: 0).Code);
        Assert.Equal("NO_ELIGIBLE_CANDIDATE",
            Select(Array.Empty<V5WorkItemQueueSelector.QueueEntry>()).Code);
    }
}
