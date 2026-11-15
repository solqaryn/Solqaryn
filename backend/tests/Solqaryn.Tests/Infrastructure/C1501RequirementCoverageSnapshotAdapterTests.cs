using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1501RequirementCoverageSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Req = "REQ-001";
    private const string Work = "C15-01.M03.04";

    private static V5RequirementCoverageRules.Entry Entry() =>
        new(Req, Work, "V5", "doc:contract", "ci:xunit", "git:proof");

    private static C1501RequirementCoverageSnapshotAdapter.VerifiedSnapshot Snapshot() =>
        new(Sha, true, true, true,
            new HashSet<string>(StringComparer.Ordinal) { Req },
            new HashSet<string>(StringComparer.Ordinal) { Work },
            new[] { Entry() },
            new Dictionary<string,V5RequirementCoverageProjectionService.WorkItemReadModel>
            {
                [Work] = new(Work, "LISTO", "dev-task", "acceptance", Sha)
            });

    private static C1501RequirementCoverageSnapshotAdapter.Request Request() =>
        new(Req, Work, Sha);

    private sealed class Reader(C1501RequirementCoverageSnapshotAdapter.VerifiedSnapshot? result)
        : IC1501TrustedRequirementSnapshotSource
    {
        public int Calls { get; private set; }
        public ValueTask<C1501RequirementCoverageSnapshotAdapter.VerifiedSnapshot?> ReadAsync(
            string requirementKey, string workItemId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(result);
        }
    }

    private static async Task<C1501RequirementCoverageSnapshotAdapter.Result> Inspect(
        C1501RequirementCoverageSnapshotAdapter.Request? req = null,
        C1501RequirementCoverageSnapshotAdapter.VerifiedSnapshot? snap = null)
    {
        var svc = new C1501RequirementCoverageSnapshotAdapter(
            new Reader(snap ?? Snapshot()));
        return await svc.InspectAsync(req ?? Request());
    }

    [Fact]
    public async Task GoodSnapshotStillRequiresCausalEvidenceAndIndependentA5()
    {
        var r = await Inspect();
        Assert.Equal("EXTERNAL_REQUIREMENT_EVIDENCE_AND_A5_REQUIRED", r.Code);
        Assert.Equal(1, r.RowsExamined);
        Assert.True(r.RequiresExternalCausalEvidence);
        Assert.False(r.GrantsA5);
        Assert.False(r.GrantsInstitutionalWrites);
        Assert.False(r.CertifiesCoverage);
        Assert.False(r.AuthorizesTenantAccess);
    }

    [Fact]
    public async Task InvalidInputNeverReadsSource()
    {
        var reader = new Reader(Snapshot());
        var svc = new C1501RequirementCoverageSnapshotAdapter(reader);
        Assert.Equal("INVALID_REQUIREMENT_OR_SHA", (await svc.InspectAsync(null)).Code);
        Assert.Equal("INVALID_REQUIREMENT_OR_SHA",
            (await svc.InspectAsync(Request() with { ExpectedHeadSha = "sha-short" })).Code);
        Assert.Equal("INVALID_REQUIREMENT_OR_SHA",
            (await svc.InspectAsync(Request() with { WorkItemId = "x/../other" })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ClientCatalogOrWriteOverridesDoNotRead()
    {
        var reader = new Reader(Snapshot());
        var svc = new C1501RequirementCoverageSnapshotAdapter(reader);
        Assert.Equal("UNTRUSTED_CATALOG_OR_WRITE_DENIED",
            (await svc.InspectAsync(Request() with { ClientControlsCatalog = true })).Code);
        Assert.Equal("UNTRUSTED_CATALOG_OR_WRITE_DENIED",
            (await svc.InspectAsync(Request() with { RequestsA5OrWrite = true })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task FakeShaOrRevokedReviewerEvidenceRejected()
    {
        Assert.Equal("CURRENT_INSTITUTIONAL_SNAPSHOT_REQUIRED",
            (await Inspect(snap: Snapshot() with { ObservedHeadSha = new string('f',40) })).Code);
        Assert.Equal("CURRENT_INSTITUTIONAL_SNAPSHOT_REQUIRED",
            (await Inspect(snap: Snapshot() with { ReviewerScopeCurrent = false })).Code);
        Assert.Equal("CURRENT_INSTITUTIONAL_SNAPSHOT_REQUIRED",
            (await Inspect(snap: Snapshot() with { ReadbackAndAuditAvailable = false })).Code);
    }

    [Fact]
    public async Task UnauthenticatedSnapshotCannotClaimSuccess() =>
        Assert.Equal("CURRENT_INSTITUTIONAL_SNAPSHOT_REQUIRED",
            (await Inspect(snap: Snapshot() with { SourceAuthenticatedByBackend = false })).Code);

    [Fact]
    public async Task ForeignOrUnknownMappingIsDenied()
    {
        Assert.Equal("EXACT_CATALOG_MEMBERSHIP_REQUIRED",
            (await Inspect(snap: Snapshot() with { AuthorizedRequirementKeys =
                new HashSet<string> { "REQ-OTHER" } })).Code);
        Assert.Equal("EXACT_CATALOG_MEMBERSHIP_REQUIRED",
            (await Inspect(snap: Snapshot() with { ExistingCatalogWorkItemIds =
                new HashSet<string> { "C15-01.M03.03" } })).Code);
    }

    [Fact]
    public async Task CaseInsensitiveCatalogCannotExpandOrdinalMembership()
    {
        Assert.Equal("EXACT_CATALOG_MEMBERSHIP_REQUIRED",
            (await Inspect(snap: Snapshot() with { AuthorizedRequirementKeys =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "req-001" } })).Code);
        Assert.Equal("EXACT_CATALOG_MEMBERSHIP_REQUIRED",
            (await Inspect(snap: Snapshot() with { ExistingCatalogWorkItemIds =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "c15-01.m03.04" } })).Code);
    }

    [Fact]
    public async Task AmbiguousOrDuplicateMappingCannotBeCertified()
    {
        Assert.Equal("EXACT_UNAMBIGUOUS_MAPPING_REQUIRED",
            (await Inspect(snap: Snapshot() with { Mappings =
                new[] { Entry(), Entry() } })).Code);
    }

    [Fact]
    public async Task PlanItemWithOldHeadFailsClosed()
    {
        var p = new Dictionary<string,V5RequirementCoverageProjectionService.WorkItemReadModel>
        {
            [Work] = new(Work,"LISTO",null,"acceptance",new string('f',40))
        };
        Assert.Equal("SOURCE_REF_MOVED",
            (await Inspect(snap: Snapshot() with { ObservedPlan = p })).Code);
    }

    [Fact]
    public async Task InvalidPlanStateIsRejected()
    {
        var p = new Dictionary<string,V5RequirementCoverageProjectionService.WorkItemReadModel>
        {
            [Work] = new(Work,"COMPLETE",null,"acceptance",Sha)
        };
        Assert.Equal("INVALID_STATE",
            (await Inspect(snap: Snapshot() with { ObservedPlan = p })).Code);
    }

    [Fact]
    public async Task NullSourceResponseCannotCertify()
    {
        var svc = new C1501RequirementCoverageSnapshotAdapter(new Reader(null));
        Assert.Equal("CURRENT_INSTITUTIONAL_SNAPSHOT_REQUIRED",
            (await svc.InspectAsync(Request())).Code);
    }

    [Fact]
    public async Task CancellationBeforeSourceReadPropagates()
    {
        var reader = new Reader(Snapshot());
        var svc = new C1501RequirementCoverageSnapshotAdapter(reader);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await svc.InspectAsync(Request(),cts.Token));
        Assert.Equal(0,reader.Calls);
    }
}
