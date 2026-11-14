using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class V5WorkItemRulesTests
{
    private const string Owner = "javier-dev-06";
    private const string Reviewer = "javier-dev-05";
    private const string Id = "C00-02.M03.01";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly IReadOnlySet<string> Catalog = new HashSet<string> { Id };
    private static readonly IReadOnlySet<string> Actors = new HashSet<string> { Owner, Reviewer };

    [Fact]
    public void Claim_AcceptsExistingPendingUnownedItemWithAllGates()
    {
        var result = V5WorkItemRules.CanClaim(new(Id, "PENDIENTE", null),
            Owner, Catalog, Actors, true, true, true, true);
        Assert.True(result.Allowed);
        Assert.Equal("OK", result.Code);
    }

    [Theory]
    [InlineData("UNLISTED", "PENDIENTE", null, "INVALID_ID")]
    [InlineData(Id, "VALIDANDO", null, "CLAIM_CONFLICT")]
    [InlineData(Id, "PENDIENTE", Reviewer, "CLAIM_CONFLICT")]
    public void Claim_RejectsUnknownIdsAndOwnedOrNonpendingItems(
        string id, string state, string? currentOwner, string expectedCode)
    {
        var decision = V5WorkItemRules.CanClaim(new(id, state, currentOwner),
            Owner, Catalog, Actors, true, true, true, true);
        Assert.False(decision.Allowed);
        Assert.Equal(expectedCode, decision.Code);
    }

    [Fact]
    public void Claim_RejectsUnauthorizedCallerEvenWhenQueueIsReady()
    {
        var decision = V5WorkItemRules.CanClaim(new(Id, "PENDIENTE", null),
            "external-task", Catalog, Actors, true, true, true, true);
        Assert.Equal("UNAUTHORIZED_ACTOR", decision.Code);
    }

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public void Claim_DoesNotTreatQueueOrDagAsPermissionIfAnyGateIsClosed(
        bool g, bool o, bool dagValidated, bool dagGate)
    {
        var decision = V5WorkItemRules.CanClaim(new(Id, "PENDIENTE", null),
            Owner, Catalog, Actors, g, o, dagValidated, dagGate);
        Assert.Equal("DEPENDENCY_BLOCKED", decision.Code);
    }

    [Fact]
    public void Candidate_RequiresOriginalOwnerAndConcreteFingerprint()
    {
        var item = new V5WorkItemRules.Snapshot(Id, "EN_PROGRESO", Owner);
        Assert.True(V5WorkItemRules.CanMarkValidating(item, Owner, "blob-fingerprint").Allowed);
        Assert.Equal("OWNER_OR_STATE_MISMATCH",
            V5WorkItemRules.CanMarkValidating(item, Reviewer, "blob-fingerprint").Code);
        Assert.Equal("CANDIDATE_INCOMPLETE",
            V5WorkItemRules.CanMarkValidating(item, Owner, "").Code);
    }

    [Fact]
    public void CloseDev_RequiresAllAuditsRealShaAndIndependentReviewPending()
    {
        var item = new V5WorkItemRules.Snapshot(Id, "VALIDANDO", Owner);
        Assert.True(V5WorkItemRules.CanCloseDev(item, Owner, Sha,
            true, true, true, true, true, true).Allowed);
        Assert.Equal("AUDIT_REQUIRED", V5WorkItemRules.CanCloseDev(item, Owner, Sha,
            true, true, true, false, true, true).Code);
        Assert.Equal("AUDIT_REQUIRED", V5WorkItemRules.CanCloseDev(item, Owner, Sha,
            true, true, true, true, true, false).Code);
        Assert.Equal("CHECK_SHA_MISMATCH", V5WorkItemRules.CanCloseDev(item, Owner, "partial-sha",
            true, true, true, true, true, true).Code);
        Assert.Equal("OWNER_OR_STATE_MISMATCH", V5WorkItemRules.CanCloseDev(item, Reviewer, Sha,
            true, true, true, true, true, true).Code);
    }

    [Fact]
    public void A5_RequiresIndependentAuthorizedReviewerAndClosedDevState()
    {
        var item = new V5WorkItemRules.Snapshot(Id, "LISTO", Owner);
        Assert.True(V5WorkItemRules.CanReviewA5(item, Owner, Reviewer, Actors).Allowed);
        Assert.Equal("REVIEW_NOT_INDEPENDENT",
            V5WorkItemRules.CanReviewA5(item, Owner, Owner, Actors).Code);
        Assert.Equal("REVIEW_NOT_INDEPENDENT",
            V5WorkItemRules.CanReviewA5(item, Owner, "foreign", Actors).Code);
        Assert.Equal("OWNER_OR_STATE_MISMATCH",
            V5WorkItemRules.CanReviewA5(item with { State = "VALIDANDO" }, Owner, Reviewer, Actors).Code);
    }
}
