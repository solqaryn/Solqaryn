using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5WorkItemTransitionPlannerTests
{
    private const string Id = "C00-02.M03.02";
    private const string Author = "6adbb9fff1548191a1bd5b30d9e9984a";
    private const string Reviewer = "6adc2e55f47c81919f6374e561687076";
    private const string Foreign = "6adbbc9ad9e88191bea8eb69104afb25";
    private const string CommitSha = "0123456789abcdef0123456789abcdef01234567";

    private static readonly IReadOnlySet<string> Catalog = new HashSet<string> { Id };
    private static readonly IReadOnlySet<string> AllowedTasks = new HashSet<string> { Author, Reviewer };
    private readonly V5WorkItemTransitionPlanner _planner = new();

    [Fact]
    public void Claim_ProducesImmutableExpectedSnapshotWithoutMutatingState()
    {
        var original = new V5WorkItemRules.Snapshot(Id, "PENDIENTE", null);
        var proposed = _planner.PrepareClaim(
            original, Author, Catalog, AllowedTasks, true, true, true, true);

        Assert.True(proposed.Allowed);
        Assert.NotNull(proposed.Intent);
        Assert.Equal("PENDIENTE", proposed.Intent.ExpectedState);
        Assert.Null(proposed.Intent.ExpectedOwnerTaskId);
        Assert.Equal("EN_PROGRESO", proposed.Intent.RequestedState);
        Assert.Equal(Author, proposed.Intent.RequestedOwnerTaskId);
        Assert.Equal("PENDIENTE", original.State);
        Assert.Null(original.OwnerTaskId);
        // The planner does not own a lease, a database or a Sheets writer.
        Assert.Equal(proposed, _planner.PrepareClaim(
            original, Author, Catalog, AllowedTasks, true, true, true, true));
    }

    [Fact]
    public void Claim_DeniesNeverExposeAWriteIntent()
    {
        var unowned = new V5WorkItemRules.Snapshot(Id, "PENDIENTE", null);
        var foreign = _planner.PrepareClaim(
            unowned, Foreign, Catalog, AllowedTasks, true, true, true, true);
        var gated = _planner.PrepareClaim(
            unowned, Author, Catalog, AllowedTasks, true, false, true, true);
        var owned = _planner.PrepareClaim(
            unowned with { OwnerTaskId = Reviewer }, Author,
            Catalog, AllowedTasks, true, true, true, true);

        Assert.Equal("UNAUTHORIZED_ACTOR", foreign.Decision.Code);
        Assert.Equal("DEPENDENCY_BLOCKED", gated.Decision.Code);
        Assert.Equal("CLAIM_CONFLICT", owned.Decision.Code);
        Assert.Null(foreign.Intent);
        Assert.Null(gated.Intent);
        Assert.Null(owned.Intent);
    }

    [Fact]
    public void ValidatingRequiresAuthorizedOriginalOwnerAndCandidateFingerprint()
    {
        var item = new V5WorkItemRules.Snapshot(Id, "EN_PROGRESO", Author);
        var good = _planner.PrepareValidating(item, Author, AllowedTasks, "candidate-blob-sha");
        Assert.True(good.Allowed);
        Assert.Equal("VALIDANDO", good.Intent!.RequestedState);
        Assert.Equal("candidate-blob-sha", good.Intent.CandidateFingerprint);

        Assert.Equal("UNAUTHORIZED_ACTOR", _planner.PrepareValidating(
            item, Foreign, AllowedTasks, "candidate-blob-sha").Decision.Code);
        Assert.Equal("OWNER_OR_STATE_MISMATCH", _planner.PrepareValidating(
            item, Reviewer, AllowedTasks, "candidate-blob-sha").Decision.Code);
        Assert.Equal("CANDIDATE_INCOMPLETE", _planner.PrepareValidating(
            item, Author, AllowedTasks, " ").Decision.Code);
    }

    [Fact]
    public void CloseDevRequiresCausalAuditsFullShaAndIndependentReviewPending()
    {
        var item = new V5WorkItemRules.Snapshot(Id, "VALIDANDO", Author);
        var passed = _planner.PrepareCloseDev(
            item, Author, AllowedTasks, CommitSha,
            true, true, true, true, true, true);
        Assert.True(passed.Allowed);
        Assert.Equal("LISTO", passed.Intent!.RequestedState);
        Assert.Equal(Author, passed.Intent.ExpectedOwnerTaskId);

        Assert.Equal("AUDIT_REQUIRED", _planner.PrepareCloseDev(
            item, Author, AllowedTasks, CommitSha,
            true, true, true, false, true, true).Decision.Code);
        Assert.Equal("AUDIT_REQUIRED", _planner.PrepareCloseDev(
            item, Author, AllowedTasks, CommitSha,
            true, true, true, true, true, false).Decision.Code);
        Assert.Equal("CHECK_SHA_MISMATCH", _planner.PrepareCloseDev(
            item, Author, AllowedTasks, "not-a-sha",
            true, true, true, true, true, true).Decision.Code);
        Assert.Equal("UNAUTHORIZED_ACTOR", _planner.PrepareCloseDev(
            item, Foreign, AllowedTasks, CommitSha,
            true, true, true, true, true, true).Decision.Code);
    }

    [Fact]
    public void A5NeverAuthorizesTheAuthorOrAnExternalTask()
    {
        var closed = new V5WorkItemRules.Snapshot(Id, "LISTO", Author);
        Assert.True(_planner.CheckIndependentA5(closed, Author, Reviewer, AllowedTasks).Allowed);
        Assert.Equal("REVIEW_NOT_INDEPENDENT", _planner.CheckIndependentA5(
            closed, Author, Author, AllowedTasks).Code);
        Assert.Equal("REVIEW_NOT_INDEPENDENT", _planner.CheckIndependentA5(
            closed, Author, Foreign, AllowedTasks).Code);
        Assert.Equal("OWNER_OR_STATE_MISMATCH", _planner.CheckIndependentA5(
            closed with { State = "VALIDANDO" }, Author, Reviewer, AllowedTasks).Code);
    }
}
