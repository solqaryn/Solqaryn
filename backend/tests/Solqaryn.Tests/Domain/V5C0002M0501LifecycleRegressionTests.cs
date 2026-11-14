using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

/// <summary>
/// C00-02.M05.01: deterministic unit regression for the pure V5 workflow rules.
/// This fixture does not acquire a Google Sheets lease, create a real claim,
/// validate a live JWT, or grant platform access.
/// </summary>
public sealed class V5C0002M0501LifecycleRegressionTests
{
    private const string ItemId = "C00-02.M05.01";
    private const string Owner = "6af16bbad26881918c283abe06e1f32a";
    private const string IndependentReviewer = "6adc2e55f47c81919f6374e561687076";
    private const string RetiredActor = "6adbbc9ad9e88191bea8eb69104afb25";
    private const string TestSha = "0123456789abcdef0123456789abcdef01234567";

    private static readonly IReadOnlySet<string> Catalog = new HashSet<string>(StringComparer.Ordinal) { ItemId };
    private static readonly IReadOnlySet<string> Authorized = new HashSet<string>(StringComparer.Ordinal)
    {
        "6adbb9fff1548191a1bd5b30d9e9984a",
        "6af16bb9492c8191b8e8c7d606f11507",
        "6adc2e4bd8a48191ac7d8641ed95a1a3",
        "6aeacb7820ac8191b7d768a6f33dfa09",
        "6adc2e55f47c81919f6374e561687076",
        Owner
    };

    [Theory]
    [InlineData("6adbb9fff1548191a1bd5b30d9e9984a")]
    [InlineData("6af16bb9492c8191b8e8c7d606f11507")]
    [InlineData("6adc2e4bd8a48191ac7d8641ed95a1a3")]
    [InlineData("6aeacb7820ac8191b7d768a6f33dfa09")]
    [InlineData("6adc2e55f47c81919f6374e561687076")]
    [InlineData(Owner)]
    public void KnownTaskNeedsAllGatesToProposeClaim(string actor)
    {
        var decision = V5WorkItemRules.CanClaim(new(ItemId, "PENDIENTE", null),
            actor, Catalog, Authorized, true, true, true, true);
        Assert.True(decision.Allowed);
        Assert.Equal("OK", decision.Code);
    }

    [Fact]
    public void RetiredOrUnknownTaskCannotClaimEvenWithAllGates()
    {
        foreach (var actor in new[] { RetiredActor, "unknown-task", "" })
        {
            var decision = V5WorkItemRules.CanClaim(new(ItemId, "PENDIENTE", null),
                actor, Catalog, Authorized, true, true, true, true);
            Assert.False(decision.Allowed);
            Assert.Equal("UNAUTHORIZED_ACTOR", decision.Code);
        }
    }

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public void EachIndependentQueueOrDagGateMustStayOpen(bool g, bool o, bool validated, bool gate)
    {
        var decision = V5WorkItemRules.CanClaim(new(ItemId, "PENDIENTE", null),
            Owner, Catalog, Authorized, g, o, validated, gate);
        Assert.False(decision.Allowed);
        Assert.Equal("DEPENDENCY_BLOCKED", decision.Code);
    }

    [Theory]
    [InlineData("EN_PROGRESO", null)]
    [InlineData("VALIDANDO", null)]
    [InlineData("PENDIENTE", "some-other-owner")]
    public void AlreadyClaimedOrNonPendingIsNeverReclaimed(string state, string? currentOwner)
    {
        var decision = V5WorkItemRules.CanClaim(new(ItemId, state, currentOwner),
            Owner, Catalog, Authorized, true, true, true, true);
        Assert.Equal("CLAIM_CONFLICT", decision.Code);
    }

    [Fact]
    public void CloseRequiresOwnValidatingStateFullShaAndEveryAudit()
    {
        var item = new V5WorkItemRules.Snapshot(ItemId, "VALIDANDO", Owner);
        Assert.True(V5WorkItemRules.CanCloseDev(item, Owner, TestSha,
            true, true, true, true, true, true).Allowed);
        Assert.Equal("AUDIT_REQUIRED", V5WorkItemRules.CanCloseDev(item, Owner, TestSha,
            true, true, true, false, true, true).Code);
        Assert.Equal("AUDIT_REQUIRED", V5WorkItemRules.CanCloseDev(item, Owner, TestSha,
            true, true, true, true, true, false).Code);
        Assert.Equal("CHECK_SHA_MISMATCH", V5WorkItemRules.CanCloseDev(item, Owner, "short",
            true, true, true, true, true, true).Code);
    }

    [Fact]
    public void IndependentA5MustBeDifferentAndOnClosedDevItem()
    {
        var item = new V5WorkItemRules.Snapshot(ItemId, "LISTO", Owner);
        Assert.True(V5WorkItemRules.CanReviewA5(item, Owner, IndependentReviewer, Authorized).Allowed);
        Assert.Equal("REVIEW_NOT_INDEPENDENT",
            V5WorkItemRules.CanReviewA5(item, Owner, Owner, Authorized).Code);
        Assert.Equal("REVIEW_NOT_INDEPENDENT",
            V5WorkItemRules.CanReviewA5(item, Owner, RetiredActor, Authorized).Code);
        Assert.Equal("OWNER_OR_STATE_MISMATCH",
            V5WorkItemRules.CanReviewA5(item with { State = "VALIDANDO" },
                Owner, IndependentReviewer, Authorized).Code);
    }
}
