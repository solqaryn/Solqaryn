using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5OperationalAuditApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly V5OperationalAuditApplicationService Service = new();

    private static V5OperationalAuditDomainRules.SourceSnapshot Source() =>
        new("catalog-revision-1",
            new HashSet<string>(StringComparer.Ordinal)
            {
                "C15-05.M03.01", "C15-05.M03.02"
            },
            new HashSet<string>(StringComparer.Ordinal)
            {
                "task-author", "task-reviewer"
            });

    private static V5OperationalAuditDomainRules.WorkItem Ready(string id) =>
        new(id, "LISTO", "task-author", Sha, "task-reviewer",
            "github:exact-commit-check", "bitacora:independent-review");

    private static V5OperationalAuditApplicationService.Request Request(
        params V5OperationalAuditDomainRules.WorkItem[] items) =>
        new(Sha, Sha, "catalog-revision-1", Source(), items,
            new HashSet<string>(StringComparer.Ordinal)
            {
                "task-author", "task-reviewer"
            });

    [Fact]
    public void CompleteSnapshotStillRequiresAuthenticatedExternalEvidence()
    {
        var result = Service.Prepare(Request(
            Ready("C15-05.M03.01"), Ready("C15-05.M03.02")));
        Assert.Equal("AUTHENTICATED_EVIDENCE_REQUIRED", result.Code);
        Assert.Equal(2, result.ItemsExamined);
        Assert.False(result.AllowsInstitutionalWrite);
    }

    [Fact]
    public void StaleOrInvalidShaCannotBeUsedForPreflight()
    {
        var input = Request(Ready("C15-05.M03.01"));
        Assert.Equal("CHECK_SHA_MISMATCH",
            Service.Prepare(input with { ObservedHeadSha = new string('f', 40) }).Code);
        Assert.Equal("CHECK_SHA_MISMATCH",
            Service.Prepare(input with { ExpectedHeadSha = "short" }).Code);
    }

    [Fact]
    public void StaleRevisionAndUnauthorizedTaskCatalogFailClosed()
    {
        var input = Request(Ready("C15-05.M03.01"));
        Assert.Equal("SOURCE_REVISION_MISMATCH",
            Service.Prepare(input with { ExpectedSourceRevision = "stale" }).Code);
        Assert.Equal("TASK_CATALOG_MISMATCH", Service.Prepare(input with
        {
            ExpectedAuthorizedTaskIds = new HashSet<string> { "task-author", "outside" }
        }).Code);
    }

    [Fact]
    public void IncompleteOrSelfReviewedSnapshotCannotReleaseWork()
    {
        var input = Request(Ready("C15-05.M03.01"));
        var incomplete = Service.Prepare(input);
        Assert.Equal("SOURCE_OR_A5_INCOMPLETE", incomplete.Code);
        Assert.False(incomplete.AllowsInstitutionalWrite);

        var self = Ready("C15-05.M03.02") with
        {
            ReviewerTaskId = "task-author"
        };
        var denied = Service.Prepare(Request(Ready("C15-05.M03.01"), self));
        Assert.Equal("INDEPENDENT_REVIEW_REQUIRED", denied.Code);
        Assert.False(denied.AllowsInstitutionalWrite);
    }

    [Fact]
    public void BoundedPreflightRefusesOversizedOrUntrustedCatalog()
    {
        var input = Request(Ready("C15-05.M03.01"), Ready("C15-05.M03.02"));
        Assert.Equal("CAPACITY_EXCEEDED",
            Service.Prepare(input with { MaxWorkItems = 1 }).Code);
        Assert.Equal("CAPACITY_EXCEEDED",
            Service.Prepare(input with { MaxWorkItems = 2341 }).Code);
    }

    [Fact]
    public void DuplicateAndUnknownIdsStayRejectedByDomainRules()
    {
        var duplicate = Service.Prepare(Request(
            Ready("C15-05.M03.01"), Ready("C15-05.M03.01")));
        Assert.Equal("DUPLICATE_WORK_ITEM", duplicate.Code);
        var unknown = Service.Prepare(Request(
            Ready("C15-05.M03.01"), Ready("C99-99.M99.99")));
        Assert.Equal("UNKNOWN_WORK_ITEM", unknown.Code);
    }
}
