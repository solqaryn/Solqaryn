using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C15-05.M05.01: real xUnit verification of the in-memory unit contract.
/// This does NOT validate the Google Sheets V17 lock or persistent CAS.
/// </summary>
public sealed class C1505M0501MultiItemUnitTests
{
    private const string IdA = "C15-05.M04.01";
    private const string IdB = "C15-05.M04.06";
    private const string ActorA = "6adc2e4bd8a48191ac7d8641ed95a1a3";
    private const string ActorB = "6af16bbad26881918c283abe06e1f32a";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static V5WorkItemMemoryAdapter NewAdapter(int maxRequests = 1000) =>
        new(
            new[] {
                new V5WorkItemRules.Snapshot(IdA, "PENDIENTE", null),
                new V5WorkItemRules.Snapshot(IdB, "PENDIENTE", null)
            },
            new HashSet<string>(StringComparer.Ordinal) { IdA, IdB },
            new HashSet<string>(StringComparer.Ordinal) { ActorA, ActorB },
            maxRequests);

    private static V5WorkItemMemoryAdapter.Result Claim(
        V5WorkItemMemoryAdapter adapter, string id, string actor, string token,
        bool g = true, bool o = true, bool dag = true, bool gate = true) =>
        adapter.Claim(id, actor, token, g, o, dag, gate);

    [Fact]
    public void SameTokenInIndependentRowsDoesNotStealAnOwnerOrDuplicateAClaim()
    {
        var adapter = NewAdapter();
        Assert.Equal("APPLIED_IN_MEMORY", Claim(adapter, IdA, ActorA, "shared-token").Code);
        Assert.Equal("APPLIED_IN_MEMORY", Claim(adapter, IdB, ActorB, "shared-token").Code);
        Assert.Equal("IDEMPOTENT_REPLAY", Claim(adapter, IdA, ActorA, "shared-token").Code);
        Assert.Equal("IDEMPOTENT_REPLAY", Claim(adapter, IdB, ActorB, "shared-token").Code);
        Assert.Equal(ActorA, adapter.Read(IdA, ActorA).Snapshot!.OwnerTaskId);
        Assert.Equal(ActorB, adapter.Read(IdB, ActorA).Snapshot!.OwnerTaskId);
    }

    [Fact]
    public async Task TwoRowsRacingFromTwoActorsProduceExactlyOneWinnerPerRow()
    {
        var adapter = NewAdapter();
        var results = await Task.WhenAll(Enumerable.Range(0, 64).Select(i => Task.Run(() =>
            Claim(adapter, i % 2 == 0 ? IdA : IdB,
                i % 4 < 2 ? ActorA : ActorB, "race-" + i))));
        Assert.Equal(2, results.Count(x => x.Applied));
        Assert.Equal(62, results.Count(x => x.Code == "CLAIM_CONFLICT"));
        Assert.Equal("EN_PROGRESO", adapter.Read(IdA, ActorA).Snapshot!.State);
        Assert.Equal("EN_PROGRESO", adapter.Read(IdB, ActorB).Snapshot!.State);
    }

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public void AnyClosedQueueOrDagGateLeavesItsRowUntouched(
        bool g, bool o, bool dag, bool gate)
    {
        var adapter = NewAdapter();
        Assert.Equal("DEPENDENCY_BLOCKED",
            Claim(adapter, IdA, ActorA, "closed", g, o, dag, gate).Code);
        Assert.Equal("PENDIENTE", adapter.Read(IdA, ActorA).Snapshot!.State);
        Assert.Null(adapter.Read(IdA, ActorA).Snapshot!.OwnerTaskId);
        Assert.Equal("APPLIED_IN_MEMORY", Claim(adapter, IdB, ActorB, "other").Code);
    }

    [Fact]
    public void ForeignOwnerCannotAdvanceOrCloseAnotherRowsWork()
    {
        var adapter = NewAdapter();
        Assert.True(Claim(adapter, IdA, ActorA, "a").Applied);
        Assert.True(Claim(adapter, IdB, ActorB, "b").Applied);
        Assert.Equal("OWNER_OR_STATE_MISMATCH",
            adapter.MarkValidating(IdA, ActorB, "invalid", "fingerprint").Code);
        Assert.True(adapter.MarkValidating(IdB, ActorB, "valid", "fingerprint").Applied);
        Assert.Equal("OWNER_OR_STATE_MISMATCH",
            adapter.CloseDev(IdB, ActorA, "foreign-close", Sha,
                true, true, true, true, true, true).Code);
        Assert.Equal("EN_PROGRESO", adapter.Read(IdA, ActorA).Snapshot!.State);
        Assert.Equal("VALIDANDO", adapter.Read(IdB, ActorB).Snapshot!.State);
    }

    [Fact]
    public void ReusingAnOldClaimTokenAfterValidationCannotReclaimEitherRow()
    {
        var adapter = NewAdapter();
        Assert.True(Claim(adapter, IdA, ActorA, "idempotency").Applied);
        Assert.True(adapter.MarkValidating(IdA, ActorA, "validate", "blob").Applied);
        Assert.Equal("IDEMPOTENCY_CONFLICT",
            Claim(adapter, IdA, ActorA, "idempotency").Code);
        Assert.Equal("PENDIENTE", adapter.Read(IdB, ActorB).Snapshot!.State);
        Assert.Null(adapter.Read(IdB, ActorB).Snapshot!.OwnerTaskId);
    }

    [Fact]
    public void BoundedCapacityRejectsSecondRowWithoutChangingItsState()
    {
        var adapter = NewAdapter(maxRequests: 1);
        Assert.True(Claim(adapter, IdA, ActorA, "one").Applied);
        Assert.Equal("CAPACITY_REACHED", Claim(adapter, IdB, ActorB, "two").Code);
        Assert.Equal("PENDIENTE", adapter.Read(IdB, ActorA).Snapshot!.State);
        Assert.Null(adapter.Read(IdB, ActorA).Snapshot!.OwnerTaskId);
    }

    [Fact]
    public void InvalidCommitShaCannotCloseValidatingOrChangeAnIndependentRow()
    {
        var adapter = NewAdapter();
        Assert.True(Claim(adapter, IdA, ActorA, "start").Applied);
        Assert.True(adapter.MarkValidating(IdA, ActorA, "validating", "blob").Applied);
        Assert.Equal("CHECK_SHA_MISMATCH",
            adapter.CloseDev(IdA, ActorA, "bad-sha", "not-forty-hex",
                true, true, true, true, true, true).Code);
        Assert.Equal("VALIDANDO", adapter.Read(IdA, ActorA).Snapshot!.State);
        Assert.Equal("PENDIENTE", adapter.Read(IdB, ActorB).Snapshot!.State);
        Assert.Null(adapter.Read(IdB, ActorB).Snapshot!.OwnerTaskId);
    }
}
