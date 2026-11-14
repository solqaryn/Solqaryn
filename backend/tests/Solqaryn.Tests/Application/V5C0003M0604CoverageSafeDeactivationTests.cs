using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C00-03.M06.04: non-destructive authorization gate rehearsal against the real
/// V5CoverageMemoryAdapter. No durable database, Sheets CAS or live task shutdown.
/// </summary>
public sealed class V5C0003M0604CoverageSafeDeactivationTests
{
    private const string Actor = "6af16bb9492c8191b8e8c7d606f11507";
    private const string Other = "6adbb9fff1548191a1bd5b30d9e9984a";
    private const string Scope = "C00-03";
    private const string OtherScope = "C00-02";
    private const string Requirement = "R-COVERAGE";
    private const string WorkItem = "C00-03.M03.02";
    private const string Version = "V5_FINAL";

    private static readonly IReadOnlySet<string> Actors = new HashSet<string> { Actor, Other };
    private static readonly IReadOnlySet<string> Scopes = new HashSet<string> { Scope, OtherScope };
    private static readonly IReadOnlySet<string> Requirements = new HashSet<string> { Requirement };
    private static readonly IReadOnlySet<string> Catalog = new HashSet<string> { WorkItem };

    private static V5RequirementCoverageRules.Entry Mapping(string evidence = "test:baseline") =>
        new(Requirement, WorkItem, Version, "contract:v5", "test:rehearsal", evidence);

    private static V5CoverageMemoryAdapter.Result Save(
        V5CoverageMemoryAdapter adapter,
        long revision = 0,
        string key = "original",
        string evidence = "test:baseline",
        IReadOnlySet<string>? allowedActors = null,
        IReadOnlySet<string>? allowedScopes = null) =>
        adapter.TrySave(Actor, Scope, Mapping(evidence), revision, key,
            allowedActors ?? Actors, allowedScopes ?? Scopes, Requirements, Catalog);

    private static V5CoverageMemoryAdapter.ReadResult Read(
        V5CoverageMemoryAdapter adapter,
        IReadOnlySet<string>? allowedActors = null,
        IReadOnlySet<string>? allowedScopes = null,
        string scope = Scope) =>
        adapter.TryRead(Actor, scope, Requirement, WorkItem, Version,
            allowedActors ?? Actors, allowedScopes ?? Scopes);

    [Theory]
    [InlineData(false, true, "UNAUTHORIZED_ACTOR")]
    [InlineData(true, false, "SCOPE_DENIED")]
    public void RevokingEitherWriteGateRefusesNewMutationButPreservesLegitimateEvidence(
        bool allowActor, bool allowScope, string expectedCode)
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.True(Save(adapter).Accepted);
        var before = Read(adapter).Item;
        Assert.NotNull(before);

        IReadOnlySet<string> actors = allowActor ? Actors : new HashSet<string> { Other };
        IReadOnlySet<string> scopes = allowScope ? Scopes : new HashSet<string> { OtherScope };
        var refused = Save(adapter, revision: 1, key: "blocked",
            evidence: "test:should-not-save", allowedActors: actors, allowedScopes: scopes);
        Assert.False(refused.Accepted);
        Assert.Equal(expectedCode, refused.Code);
        Assert.Equal(before, Read(adapter).Item);
        Assert.Equal(1, Read(adapter).Item!.Revision);
    }

    [Fact]
    public void RevokedActorCannotReadStoredEvidenceButRestoringActorPreservesIt()
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.True(Save(adapter).Accepted);
        var before = Read(adapter).Item;

        var denied = Read(adapter, allowedActors: new HashSet<string> { Other });
        Assert.False(denied.Allowed);
        Assert.Equal("UNAUTHORIZED_ACTOR", denied.Code);
        Assert.Null(denied.Item);

        var restored = Read(adapter);
        Assert.True(restored.Allowed);
        Assert.Equal("FOUND", restored.Code);
        Assert.Equal(before, restored.Item);
    }

    [Fact]
    public void ClosedScopeHidesStoredSnapshotButReopeningRestoresSameRevisionAndPayload()
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.True(Save(adapter).Accepted);
        var before = Read(adapter).Item;

        var denied = Read(adapter, allowedScopes: new HashSet<string> { OtherScope });
        Assert.False(denied.Allowed);
        Assert.Equal("SCOPE_DENIED", denied.Code);
        Assert.Null(denied.Item);

        var restored = Read(adapter);
        Assert.Equal(before, restored.Item);
        Assert.Equal(Mapping(), restored.Item!.Entry);
        Assert.Equal(1, restored.Item.Revision);
    }

    [Fact]
    public void ReopeningDoesNotBypassOptimisticRevisionAndCannotReplaceLegitimateEvidence()
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.True(Save(adapter).Accepted);
        var before = Read(adapter).Item;

        Assert.Equal("SCOPE_DENIED", Save(adapter, revision: 1, key: "while-closed",
            evidence: "test:temporary", allowedScopes: new HashSet<string> { OtherScope }).Code);
        var stale = Save(adapter, revision: 0, key: "after-reopen",
            evidence: "test:stale");
        Assert.False(stale.Accepted);
        Assert.Equal("REVISION_CONFLICT", stale.Code);
        Assert.Equal(before, Read(adapter).Item);
    }

    [Fact]
    public void IdempotencyKeyReusedWithDifferentEvidenceCannotOverwriteStoredSnapshot()
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.True(Save(adapter).Accepted);
        var original = Read(adapter).Item;

        var collision = Save(adapter, revision: 1, key: "original", evidence: "test:wrong");
        Assert.False(collision.Accepted);
        Assert.Equal("IDEMPOTENCY_CONFLICT", collision.Code);
        Assert.Equal(original, Read(adapter).Item);
    }

    [Fact]
    public void CapacityGateDenialCannotEraseEarlierLegitimateCoverage()
    {
        var adapter = new V5CoverageMemoryAdapter(capacity: 1);
        Assert.True(Save(adapter).Accepted);
        var original = Read(adapter).Item;

        var denied = Save(adapter, revision: 1, key: "second", evidence: "test:new");
        Assert.False(denied.Accepted);
        Assert.Equal("CAPACITY_REACHED", denied.Code);
        Assert.Equal(original, Read(adapter).Item);
    }

    [Fact]
    public void AlternateScopeCannotReadLegitimateSnapshotAfterAuthorizationRestoration()
    {
        var adapter = new V5CoverageMemoryAdapter();
        Assert.True(Save(adapter).Accepted);
        var foreign = Read(adapter, scope: OtherScope);
        Assert.True(foreign.Allowed);
        Assert.Equal("NOT_FOUND", foreign.Code);
        Assert.Null(foreign.Item);
        Assert.Equal(Mapping(), Read(adapter).Item!.Entry);
    }
}
