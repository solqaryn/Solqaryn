using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C00-03.M05.03: real integration of the V5 in-process mapping provider with
/// the authorized coverage projection. No MySQL, Sheets, JWT or durable lease.
/// </summary>
public sealed class V5C0003M0503ProviderIntegrationTests
{
    private const string Actor = "6adbb9fff1548191a1bd5b30d9e9984a";
    private const string Scope = "C00-03";
    private const string WorkItem = "C00-03.M03.02";
    private const string Requirement = "R-COVERAGE";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static readonly IReadOnlySet<string> Actors = new HashSet<string> { Actor };
    private static readonly IReadOnlySet<string> Requirements = new HashSet<string> { Requirement };
    private static readonly IReadOnlySet<string> Catalog = new HashSet<string> { WorkItem };
    private static readonly IReadOnlySet<string> Scopes = new HashSet<string> { Scope, "C00-02" };

    private static V5RequirementCoverageRules.Entry Mapping(string evidence = "test:xunit") =>
        new(Requirement, WorkItem, "V5_FINAL", "contract:v5", "test:provider", evidence);

    private static V5CoverageMemoryAdapter.Result Save(
        V5CoverageMemoryAdapter adapter, long revision = 0,
        string key = "save-1", string scope = Scope,
        V5RequirementCoverageRules.Entry? entry = null) =>
        adapter.TrySave(Actor, scope, entry ?? Mapping(), revision, key,
            Actors, Scopes, Requirements, Catalog);

    private static V5RequirementCoverageProjectionService.ProjectionResult Project(
        V5RequirementCoverageRules.Entry entry,
        string scope = Scope, string tenant = "tenant-a")
    {
        var trustedScopes = new Dictionary<string, IReadOnlySet<string>>
        {
            [Actor] = Scopes
        };
        var trustedTenants = new Dictionary<string, string>
        {
            [Scope] = "tenant-a",
            ["C00-02"] = "tenant-b"
        };
        var trustedWorkItems = new Dictionary<string, string> { [WorkItem] = Scope };
        var trustedRequirements = new Dictionary<string, string> { [Requirement] = Scope };
        var snapshots = new Dictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
        {
            [WorkItem] = new(WorkItem, "LISTO", Actor, "Matriz integrada", Sha)
        };
        return new V5CoverageTenantProjectionService().Project(
            Actor, scope, tenant, new[] { entry }, snapshots, Requirements,
            Catalog, Sha, Actors, trustedScopes, trustedTenants,
            trustedWorkItems, trustedRequirements);
    }

    [Fact]
    public void StoredMappingCanBeReadAndProjectedWithoutFalselyCertifyingEvidence()
    {
        var provider = new V5CoverageMemoryAdapter();
        var saved = Save(provider);
        Assert.True(saved.Accepted);
        Assert.Equal("SAVED", saved.Code);
        Assert.Equal(1, saved.Revision);

        var read = provider.TryRead(Actor, Scope, Requirement, WorkItem, "V5_FINAL",
            Actors, Scopes);
        Assert.Equal("FOUND", read.Code);
        var stored = Assert.IsType<V5CoverageMemoryAdapter.Stored>(read.Item);
        Assert.Equal(1, stored.Revision);
        Assert.Equal(Mapping(), stored.Entry);

        var projection = Project(stored.Entry);
        Assert.True(projection.Verdict.Accepted);
        Assert.Equal("OK", projection.Verdict.Code);
        var row = Assert.Single(projection.Rows);
        Assert.Equal(WorkItem, row.WorkItemId);
        Assert.Contains("EVIDENCE_NOT_VERIFIED", row.Outstanding);
    }

    [Fact]
    public void ProviderNamespaceAndTrustedProjectionRejectCrossScopeObservation()
    {
        var provider = new V5CoverageMemoryAdapter();
        Assert.True(Save(provider).Accepted);

        // The same authorized actor may know two scopes; data must not cross namespaces.
        var foreignRead = provider.TryRead(Actor, "C00-02", Requirement,
            WorkItem, "V5_FINAL", Actors, Scopes);
        Assert.True(foreignRead.Allowed);
        Assert.Equal("NOT_FOUND", foreignRead.Code);
        Assert.Null(foreignRead.Item);

        // A caller cannot project an item from C00-03 under the other tenant,
        // even when the globally authorized catalog knows the item.
        var forbidden = Project(Mapping(), "C00-02", "tenant-b");
        Assert.False(forbidden.Verdict.Accepted);
        Assert.Equal("WORK_ITEM_SCOPE_DENIED", forbidden.Verdict.Code);
        Assert.Empty(forbidden.Rows);
    }

    [Fact]
    public void RejectedStaleWriteCannotAlterWhatAuthorizedProjectionObserves()
    {
        var provider = new V5CoverageMemoryAdapter();
        Assert.True(Save(provider).Accepted);
        var changed = Mapping("test:unverified-replacement");

        var rejected = Save(provider, revision: 0, key: "save-2", entry: changed);
        Assert.False(rejected.Accepted);
        Assert.Equal("REVISION_CONFLICT", rejected.Code);

        var read = provider.TryRead(Actor, Scope, Requirement, WorkItem, "V5_FINAL",
            Actors, Scopes);
        var stored = Assert.IsType<V5CoverageMemoryAdapter.Stored>(read.Item);
        Assert.Equal(1, stored.Revision);
        Assert.Equal(Mapping(), stored.Entry);
        var projected = Project(stored.Entry);
        Assert.True(projected.Verdict.Accepted);
        Assert.Equal("test:xunit", Assert.Single(projected.Rows).EvidenceReference);
        Assert.Contains("EVIDENCE_NOT_VERIFIED", projected.Rows[0].Outstanding);
    }
}
