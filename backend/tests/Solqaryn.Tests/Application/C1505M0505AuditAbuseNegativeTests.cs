using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C15-05.M05.05: adversarial in-memory state machine tests.
/// These do not certify distributed Google Sheets V17 locking, HTTP JWT or MySQL.
/// </summary>
public sealed class C1505M0505AuditAbuseNegativeTests
{
    private const string IdA = "C15-05.M04.01";
    private const string IdB = "C15-05.M04.06";
    private const string ActorA = "6adc2e4bd8a48191ac7d8641ed95a1a3";
    private const string ActorB = "6af16bbad26881918c283abe06e1f32a";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static IReadOnlySet<string> Catalog() =>
        new HashSet<string>(StringComparer.Ordinal) { IdA, IdB };
    private static IReadOnlySet<string> Actors() =>
        new HashSet<string>(StringComparer.Ordinal) { ActorA, ActorB };

    private static V5WorkItemMemoryAdapter Adapter() => new(
        new[]
        {
            new V5WorkItemRules.Snapshot(IdA, "PENDIENTE", null),
            new V5WorkItemRules.Snapshot(IdB, "PENDIENTE", null)
        }, Catalog(), Actors());

    private static V5WorkItemMemoryAdapter.Result Claim(
        V5WorkItemMemoryAdapter adapter, string id = IdA,
        string actor = ActorA, string token = "claim",
        bool g = true, bool o = true, bool dag = true, bool ready = true)
        => adapter.Claim(id, actor, token, g, o, dag, ready);

    [Fact]
    public void UnauthorizedTaskCannotReadOwnerOrState()
    {
        var a = Adapter();
        var denied = a.Read(IdA, "unlisted-task");
        Assert.False(denied.Applied);
        Assert.Equal("UNAUTHORIZED_ACTOR", denied.Code);
        Assert.Null(denied.Snapshot);
    }

    [Fact]
    public void UnknownWorkItemReadDoesNotExposeInternalSnapshot()
    {
        var denied = Adapter().Read("C99-99.M99.99", ActorA);
        Assert.False(denied.Applied);
        Assert.Equal("UNKNOWN_WORK_ITEM", denied.Code);
        Assert.Null(denied.Snapshot);
    }

    [Fact]
    public void ForgedTaskCannotClaimEvenWhenAllDependencyFlagsAreTrue()
    {
        var a = Adapter();
        var denied = Claim(a, actor: "unlisted-task");
        Assert.Equal("UNAUTHORIZED_ACTOR", denied.Code);
        Assert.Equal("PENDIENTE", a.Read(IdA, ActorA).Snapshot!.State);
    }

    [Fact]
    public void UnknownScopeCannotBeClaimedOrChangeAnAuthorizedScope()
    {
        var a = Adapter();
        Assert.Equal("UNKNOWN_WORK_ITEM", Claim(a, id: "C99-99.M99.99").Code);
        Assert.Null(a.Read(IdA, ActorA).Snapshot!.OwnerTaskId);
    }

    [Fact]
    public void BlankTokenIsRejectedWithoutReservingTheItem()
    {
        var a = Adapter();
        Assert.Equal("IDEMPOTENCY_TOKEN_REQUIRED", Claim(a, token: " ").Code);
        Assert.Equal("PENDIENTE", a.Read(IdA, ActorA).Snapshot!.State);
    }

    [Fact]
    public void ClosedDagDoesNotGrantAClaimOrAffectAnIndependentRow()
    {
        var a = Adapter();
        Assert.Equal("DEPENDENCY_BLOCKED", Claim(a, dag: false).Code);
        Assert.Null(a.Read(IdA, ActorA).Snapshot!.OwnerTaskId);
        Assert.Equal("PENDIENTE", a.Read(IdB, ActorB).Snapshot!.State);
    }

    [Fact]
    public void ForeignOwnerCannotMarkAnotherTasksWorkValidating()
    {
        var a = Adapter();
        Assert.True(Claim(a).Applied);
        Assert.Equal("OWNER_OR_STATE_MISMATCH",
            a.MarkValidating(IdA, ActorB, "foreign", "blob").Code);
        Assert.Equal("EN_PROGRESO", a.Read(IdA, ActorA).Snapshot!.State);
    }

    [Fact]
    public void ForeignOwnerCannotCloseAnAlreadyValidatingItem()
    {
        var a = Adapter();
        Assert.True(Claim(a).Applied);
        Assert.True(a.MarkValidating(IdA, ActorA, "move", "blob").Applied);
        Assert.Equal("OWNER_OR_STATE_MISMATCH",
            a.CloseDev(IdA, ActorB, "foreign-close", Sha,
                true, true, true, true, true, true).Code);
        Assert.Equal("VALIDANDO", a.Read(IdA, ActorA).Snapshot!.State);
    }

    [Fact]
    public void ForgedCommitShaCannotCloseEvenWithSixPositiveFlags()
    {
        var a = Adapter();
        Assert.True(Claim(a).Applied);
        Assert.True(a.MarkValidating(IdA, ActorA, "move", "blob").Applied);
        Assert.Equal("CHECK_SHA_MISMATCH",
            a.CloseDev(IdA, ActorA, "bad-sha", "not-a-git-sha",
                true, true, true, true, true, true).Code);
        Assert.Equal("VALIDANDO", a.Read(IdA, ActorA).Snapshot!.State);
    }

    [Fact]
    public void SelfReviewAndUnlistedReviewerCannotSatisfyA5()
    {
        var snapshot = new V5WorkItemRules.Snapshot(IdA, "LISTO", ActorA);
        Assert.Equal("REVIEW_NOT_INDEPENDENT",
            V5WorkItemRules.CanReviewA5(snapshot, ActorA, ActorA, Actors()).Code);
        Assert.Equal("REVIEW_NOT_INDEPENDENT",
            V5WorkItemRules.CanReviewA5(snapshot, ActorA, "unlisted-task", Actors()).Code);
        Assert.True(V5WorkItemRules.CanReviewA5(snapshot, ActorA, ActorB, Actors()).Allowed);
    }

    [Fact]
    public void TokenReuseForDifferentOperationCannotAdvanceWork()
    {
        var a = Adapter();
        Assert.True(Claim(a, token: "same-token").Applied);
        Assert.Equal("IDEMPOTENCY_CONFLICT",
            a.MarkValidating(IdA, ActorA, "same-token", "blob").Code);
        Assert.Equal("EN_PROGRESO", a.Read(IdA, ActorA).Snapshot!.State);
    }

    [Fact]
    public void DuplicateInitialWorkItemsAreRejectedBeforeAnyMutation()
    {
        Assert.Throws<ArgumentException>(() => new V5WorkItemMemoryAdapter(
            new[]
            {
                new V5WorkItemRules.Snapshot(IdA, "PENDIENTE", null),
                new V5WorkItemRules.Snapshot(IdA, "PENDIENTE", null)
            }, Catalog(), Actors()));
    }
}
