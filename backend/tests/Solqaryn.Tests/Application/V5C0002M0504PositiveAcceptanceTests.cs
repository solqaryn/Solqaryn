using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C00-02.M05.04: positive acceptance of the actual V5 in-memory lifecycle.
/// This does not simulate institutional Sheets CAS, a JWT session, or MySQL.
/// </summary>
public sealed class V5C0002M0504PositiveAcceptanceTests
{
    private const string Item = "C00-02.M05.04";
    private const string OtherItem = "C00-02.M05.05";
    private const string Author = "6af16bb9492c8191b8e8c7d606f11507";
    private const string Independent = "6adbb9fff1548191a1bd5b30d9e9984a";
    private const string PublishedSha = "0123456789abcdef0123456789abcdef01234567";

    private static V5WorkItemMemoryAdapter Adapter() => new(
        new[]
        {
            new V5WorkItemRules.Snapshot(Item, "PENDIENTE", null),
            new V5WorkItemRules.Snapshot(OtherItem, "PENDIENTE", null)
        },
        new HashSet<string> { Item, OtherItem },
        new HashSet<string> { Author, Independent });

    [Fact]
    public void AuthorizedPositiveAcceptanceCyclePreservesAuthorAndRequiresAudits()
    {
        var adapter = Adapter();

        var claim = adapter.Claim(Item, Author, "positive-claim",
            queueDependenciesOk: true, queueDevelopmentEnabled: true,
            dagValidated: true, dagGateOpen: true);
        Assert.True(claim.Applied);
        Assert.Equal("EN_PROGRESO", claim.Snapshot!.State);
        Assert.Equal(Author, claim.Snapshot.OwnerTaskId);

        var candidate = adapter.MarkValidating(
            Item, Author, "positive-candidate", "test-blob-fingerprint");
        Assert.True(candidate.Applied);
        Assert.Equal("VALIDANDO", candidate.Snapshot!.State);

        var closed = adapter.CloseDev(Item, Author, "positive-close", PublishedSha,
            a0Passed: true, a1Passed: true, a2Passed: true,
            a3Passed: true, a4Passed: true, a5PendingRecorded: true);
        Assert.True(closed.Applied);
        Assert.Equal("LISTO", closed.Snapshot!.State);
        Assert.Equal(Author, closed.Snapshot.OwnerTaskId);

        var readback = adapter.Read(Item, Author);
        Assert.True(readback.Applied);
        Assert.Equal(closed.Snapshot, readback.Snapshot);
        Assert.True(V5WorkItemRules.CanReviewA5(
            readback.Snapshot!, Author, Independent,
            new HashSet<string> { Author, Independent }).Allowed);
        // An eligible independent reviewer is not evidence that A5 was executed.
    }

    [Fact]
    public void IndependentEligibleItemsCompleteWithoutChangingEachOthersOwners()
    {
        var adapter = Adapter();
        Assert.True(adapter.Claim(Item, Author, "claim-a", true, true, true, true).Applied);
        Assert.True(adapter.Claim(OtherItem, Independent, "claim-b", true, true, true, true).Applied);

        Assert.True(adapter.MarkValidating(Item, Author, "candidate-a", "fingerprint-a").Applied);
        Assert.True(adapter.MarkValidating(OtherItem, Independent, "candidate-b", "fingerprint-b").Applied);

        Assert.True(adapter.CloseDev(Item, Author, "close-a", PublishedSha,
            true, true, true, true, true, true).Applied);
        Assert.True(adapter.CloseDev(OtherItem, Independent, "close-b", PublishedSha,
            true, true, true, true, true, true).Applied);

        Assert.Equal(Author, adapter.Read(Item, Author).Snapshot!.OwnerTaskId);
        Assert.Equal(Independent, adapter.Read(OtherItem, Independent).Snapshot!.OwnerTaskId);
        Assert.Equal("LISTO", adapter.Read(Item, Author).Snapshot!.State);
        Assert.Equal("LISTO", adapter.Read(OtherItem, Independent).Snapshot!.State);
    }
}
