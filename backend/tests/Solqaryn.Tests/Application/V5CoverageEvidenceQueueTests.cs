using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5CoverageEvidenceQueueTests
{
    private const string Actor = "6aeacb7820ac8191b7d768a6f33dfa09";
    private const string Other = "6adbb9fff1548191a1bd5b30d9e9984a";
    private const string Foreign = "6adbbc9ad9e88191bea8eb69104afb25";
    private const string Scope = "C00-03";
    private const string A = "C00-03.M03.05";
    private const string B = "C00-03.M03.04";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly IReadOnlySet<string> Actors = new HashSet<string> { Actor, Other };
    private static readonly IReadOnlySet<string> Scopes = new HashSet<string> { Scope };
    private static readonly IReadOnlySet<string> Requirements = new HashSet<string> { "R1" };
    private static readonly IReadOnlySet<string> Catalog = new HashSet<string> { A, B };
    private readonly V5CoverageEvidenceQueue _queue = new();

    private static V5RequirementCoverageRules.Entry Mapping(string id = A) =>
        new("R1", id, "V5_FINAL", "contract:ref", "test:ref", "evidence:ref");

    private static IReadOnlyDictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
        Plan(string state = "LISTO", string? owner = Other, string sha = Sha) =>
        new Dictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
        {
            [A] = new(A, state, owner, "Expected coverage", sha),
            [B] = new(B, "PENDIENTE", null, "Second expected", sha)
        };

    private V5CoverageEvidenceQueue.Result Build(
        IReadOnlyCollection<V5RequirementCoverageRules.Entry>? mappings = null,
        IReadOnlyDictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>? plan = null,
        string actor = Actor, string scope = Scope, string sha = Sha, int max = 1000) =>
        _queue.Build(actor, scope, Actors, Scopes, mappings ?? new[] { Mapping() },
            plan ?? Plan(), Requirements, Catalog, sha, max);

    [Fact]
    public void ExistingClosedItemProducesUnverifiedEvidenceIntentNotCertification()
    {
        var result = Build();
        Assert.True(result.Accepted);
        Assert.Equal("EVIDENCE_INTENTS_ONLY", result.Code);
        var intent = Assert.Single(result.Intents);
        Assert.Equal(A, intent.WorkItemId);
        Assert.Equal("LISTO", intent.State);
        Assert.Equal(Sha, intent.ExactHeadSha);
        Assert.Contains("EVIDENCE_NOT_VERIFIED", intent.MissingEvidence);
    }

    [Fact]
    public void OwnClosedItemCannotBeAddedToIndependentReviewQueue()
    {
        var result = Build(plan: Plan(owner: Actor));
        Assert.True(result.Accepted);
        Assert.Empty(result.Intents);
    }

    [Fact]
    public void PendingItemsPreserveMissingRuntimeAndEvidenceStatus()
    {
        var result = Build(plan: Plan(state: "PENDIENTE", owner: null));
        Assert.True(result.Accepted);
        var intent = Assert.Single(result.Intents);
        Assert.Contains("WORK_ITEM_NOT_LISTO", intent.MissingEvidence);
        Assert.Contains("EVIDENCE_NOT_VERIFIED", intent.MissingEvidence);
    }

    [Fact]
    public void MappingBatchIsDeterministicallySortedWithoutCreatingCatalogEntries()
    {
        var result = Build(new[] { Mapping(A), Mapping(B) });
        Assert.True(result.Accepted);
        Assert.Equal(2, result.Intents.Count);
        Assert.Equal(new[] { B, A }, result.Intents.Select(x => x.WorkItemId).ToArray());
    }

    [Fact]
    public void CrossAccountAndUnknownScopeAreDenied()
    {
        Assert.Equal("UNAUTHORIZED_ACTOR", Build(actor: Foreign).Code);
        Assert.Equal("SCOPE_DENIED", Build(scope: "foreign").Code);
        Assert.Empty(Build(actor: Foreign).Intents);
    }

    [Fact]
    public void DuplicatesUnknownIdsAndStaleHeadFailClosed()
    {
        Assert.Equal("DUPLICATE_COVERAGE_MAPPING",
            Build(new[] { Mapping(), Mapping() }).Code);
        Assert.Equal("UNKNOWN_WORK_ITEM",
            Build(new[] { Mapping("C12-99.M01.01") }).Code);
        Assert.Equal("SOURCE_REF_MOVED",
            Build(plan: Plan(sha: new string('a', 40))).Code);
        Assert.Equal("CHECK_SHA_MISMATCH", Build(sha: "invalid").Code);
    }

    [Fact]
    public void CapacityAndEmptyInputNeverClaimUnverifiedSuccess()
    {
        Assert.Equal("CAPACITY_REACHED",
            Build(new[] { Mapping(A), Mapping(B) }, max: 1).Code);
        Assert.Equal("INVALID_CAPACITY", Build(max: 1001).Code);
        Assert.Equal("COVERAGE_EMPTY",
            Build(Array.Empty<V5RequirementCoverageRules.Entry>()).Code);
    }
}
