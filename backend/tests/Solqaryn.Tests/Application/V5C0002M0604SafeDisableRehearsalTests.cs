using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C00-02.M06.04: non-destructive deactivation rehearsal against the real V5
/// in-memory adapter. This is NOT a Sheets CAS, MySQL restore, or live rollback.
/// </summary>
public sealed class V5C0002M0604SafeDisableRehearsalTests
{
    private const string Current = "C00-02.M06.04";
    private const string Completed = "C00-02.M05.03";
    private const string Owner = "6aeacb7820ac8191b7d768a6f33dfa09";
    private const string Other = "6af16bb9492c8191b8e8c7d606f11507";

    private static V5WorkItemMemoryAdapter Adapter() => new(
        new[]
        {
            new V5WorkItemRules.Snapshot(Completed, "LISTO", Owner),
            new V5WorkItemRules.Snapshot(Current, "PENDIENTE", null)
        },
        new HashSet<string> { Completed, Current },
        new HashSet<string> { Owner, Other });

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public void ClosingAnyDevelopmentGatePreservesLegitimateStoredSnapshots(
        bool dependencies, bool developmentEnabled, bool dagValidated, bool dagOpen)
    {
        var adapter = Adapter();
        var before = adapter.Read(Completed, Owner).Snapshot;
        Assert.NotNull(before);
        Assert.Equal("LISTO", before.State);

        var refused = adapter.Claim(
            Current, Owner, "deactivation-rehearsal",
            dependencies, developmentEnabled, dagValidated, dagOpen);
        Assert.False(refused.Applied);
        Assert.Equal("DEPENDENCY_BLOCKED", refused.Code);

        var preserved = adapter.Read(Completed, Owner).Snapshot;
        Assert.Equal(before, preserved);
        Assert.Equal(Owner, preserved!.OwnerTaskId);
        var unopened = adapter.Read(Current, Owner).Snapshot;
        Assert.Equal("PENDIENTE", unopened!.State);
        Assert.Null(unopened.OwnerTaskId);
    }

    [Fact]
    public void ReenablingGateAllowsOnlyNewAuthorizedClaimAndKeepsCompletedItem()
    {
        var adapter = Adapter();
        var completed = adapter.Read(Completed, Owner).Snapshot;
        var refused = adapter.Claim(
            Current, Owner, "disable-first",
            true, false, true, true);
        Assert.Equal("DEPENDENCY_BLOCKED", refused.Code);

        var resumed = adapter.Claim(
            Current, Owner, "resume-after-gate",
            true, true, true, true);
        Assert.True(resumed.Applied);
        Assert.Equal("EN_PROGRESO", resumed.Snapshot!.State);
        Assert.Equal(Owner, resumed.Snapshot.OwnerTaskId);
        Assert.Equal(completed, adapter.Read(Completed, Owner).Snapshot);
        Assert.Equal("IDEMPOTENT_REPLAY",
            adapter.Claim(Current, Owner, "resume-after-gate",
                true, true, true, true).Code);
    }

    [Fact]
    public void DeactivationCannotEraseCompletedItemOrGrantForeignOwnership()
    {
        var adapter = Adapter();
        var before = adapter.Read(Completed, Owner).Snapshot;
        var refused = adapter.Claim(
            Current, Other, "foreign-while-disabled",
            true, false, true, true);
        Assert.False(refused.Applied);
        Assert.Equal("DEPENDENCY_BLOCKED", refused.Code);
        Assert.Equal(before, adapter.Read(Completed, Owner).Snapshot);

        var claimed = adapter.Claim(
            Current, Owner, "authorized-after-enable",
            true, true, true, true);
        Assert.True(claimed.Applied);
        Assert.Equal("CLAIM_CONFLICT",
            adapter.Claim(Current, Other, "foreign-after-enable",
                true, true, true, true).Code);
        Assert.Equal("EN_PROGRESO", adapter.Read(Current, Owner).Snapshot!.State);
        Assert.Equal(Owner, adapter.Read(Current, Owner).Snapshot!.OwnerTaskId);
        Assert.Equal(before, adapter.Read(Completed, Owner).Snapshot);
    }
}
