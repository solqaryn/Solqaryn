using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5WorkItemMemoryAdapterTests
{
    private const string Id = "C00-02.M03.04";
    private const string Author = "6af16bb9492c8191b8e8c7d606f11507";
    private const string Other = "6adbb9fff1548191a1bd5b30d9e9984a";
    private const string Outside = "6adbbc9ad9e88191bea8eb69104afb25";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static V5WorkItemMemoryAdapter Adapter(int maxRequests = 1000) =>
        new(new[] { new V5WorkItemRules.Snapshot(Id, "PENDIENTE", null) },
            new HashSet<string> { Id }, new HashSet<string> { Author, Other },
            maxRequests);

    private static V5WorkItemMemoryAdapter.Result Claim(
        V5WorkItemMemoryAdapter adapter, string actor = Author, string token = "claim",
        bool g = true, bool o = true, bool dag = true, bool gate = true) =>
        adapter.Claim(Id, actor, token, g, o, dag, gate);

    [Fact]
    public void ClaimValidatingAndCloseDevRequireAllTransitionsAndAudits()
    {
        var a = Adapter();
        Assert.Equal("APPLIED_IN_MEMORY", Claim(a).Code);
        Assert.Equal("EN_PROGRESO", a.Read(Id, Author).Snapshot!.State);
        Assert.Equal("APPLIED_IN_MEMORY",
            a.MarkValidating(Id, Author, "candidate", "blob-sha").Code);
        Assert.Equal("AUDIT_REQUIRED",
            a.CloseDev(Id, Author, "close", Sha, true, true, true, false, true, true).Code);
        Assert.Equal("APPLIED_IN_MEMORY",
            a.CloseDev(Id, Author, "close", Sha, true, true, true, true, true, true).Code);
        Assert.Equal("LISTO", a.Read(Id, Author).Snapshot!.State);
    }

    [Fact]
    public void UnauthorizedActorsAndUnknownCatalogAreDenied()
    {
        var a = Adapter();
        Assert.Equal("UNAUTHORIZED_ACTOR", Claim(a, Outside).Code);
        Assert.Equal("UNAUTHORIZED_ACTOR", a.Read(Id, Outside).Code);
        Assert.Equal("UNKNOWN_WORK_ITEM",
            a.Claim("C00-02.M06.07", Author, "claim", true, true, true, true).Code);
        Assert.Equal("PENDIENTE", a.Read(Id, Author).Snapshot!.State);
    }

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public void ClosedQueueOrDagGateDeniesClaim(bool g, bool o, bool dag, bool gate)
    {
        var a = Adapter();
        Assert.Equal("DEPENDENCY_BLOCKED", Claim(a, g: g, o: o, dag: dag, gate: gate).Code);
        Assert.Equal("PENDIENTE", a.Read(Id, Author).Snapshot!.State);
    }

    [Fact]
    public void OwnerCannotBeReplacedAndValidatingRequiresFingerprint()
    {
        var a = Adapter();
        Assert.True(Claim(a).Applied);
        Assert.Equal("CLAIM_CONFLICT", Claim(a, Other, "other").Code);
        Assert.Equal("OWNER_OR_STATE_MISMATCH",
            a.MarkValidating(Id, Other, "foreign", "fingerprint").Code);
        Assert.Equal("CANDIDATE_INCOMPLETE",
            a.MarkValidating(Id, Author, "no-fingerprint", "").Code);
        Assert.Equal(Author, a.Read(Id, Author).Snapshot!.OwnerTaskId);
    }

    [Fact]
    public void ReplayIsIdempotentButTokenReuseAfterStateChangeIsRejected()
    {
        var a = Adapter();
        Assert.True(Claim(a).Applied);
        Assert.Equal("IDEMPOTENT_REPLAY", Claim(a).Code);
        Assert.True(a.MarkValidating(Id, Author, "candidate", "sha").Applied);
        Assert.Equal("IDEMPOTENCY_CONFLICT", Claim(a).Code);
        Assert.Equal("VALIDANDO", a.Read(Id, Author).Snapshot!.State);
    }

    [Fact]
    public void ExactCommitShaIsRequiredBeforeCloseDev()
    {
        var a = Adapter();
        Claim(a);
        a.MarkValidating(Id, Author, "candidate", "blob");
        Assert.Equal("CHECK_SHA_MISMATCH",
            a.CloseDev(Id, Author, "close", "short", true, true, true, true, true, true).Code);
        Assert.Equal("VALIDANDO", a.Read(Id, Author).Snapshot!.State);
    }

    [Fact]
    public async Task ConcurrentClaimsYieldExactlyOneWinner()
    {
        var a = Adapter();
        var results = await Task.WhenAll(Enumerable.Range(0, 32)
            .Select(i => Task.Run(() => Claim(a, Author, "claim-" + i))));
        Assert.Single(results.Where(x => x.Applied));
        Assert.Equal(31, results.Count(x => x.Code == "CLAIM_CONFLICT"));
        Assert.Equal("EN_PROGRESO", a.Read(Id, Author).Snapshot!.State);
    }

    [Fact]
    public void CapacityLimitFailsClosedWithoutMutatingState()
    {
        var a = Adapter(maxRequests: 1);
        Assert.True(Claim(a).Applied);
        Assert.Equal("CAPACITY_REACHED",
            a.MarkValidating(Id, Author, "candidate", "blob").Code);
        Assert.Equal("EN_PROGRESO", a.Read(Id, Author).Snapshot!.State);
    }

    [Fact]
    public void MissingTokenCannotWrite()
    {
        var a = Adapter();
        Assert.Equal("IDEMPOTENCY_TOKEN_REQUIRED", Claim(a, token: " ").Code);
        Assert.Equal("PENDIENTE", a.Read(Id, Author).Snapshot!.State);
    }
}
