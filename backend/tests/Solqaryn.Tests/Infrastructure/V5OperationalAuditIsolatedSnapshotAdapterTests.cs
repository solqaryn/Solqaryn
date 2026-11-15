using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5OperationalAuditIsolatedSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";
    private const string Id = "C15-05.M03.04";
    private const string Author = "javier-task-05";
    private const string Reviewer = "javier-task-04";

    private static V5OperationalAuditDomainRules.WorkItem Complete() =>
        new(Id, "LISTO", Author, Sha, Reviewer, "check:exact-sha", "a5:independent");

    private static V5OperationalAuditIsolatedSnapshotAdapter Adapter(
        V5OperationalAuditDomainRules.WorkItem? item = null,
        IEnumerable<string>? tasks = null) =>
        new("v5-source-revision", Sha,
            new[] { item ?? Complete() }, new[] { Id },
            tasks ?? new[] { Author, Reviewer });

    [Fact]
    public void CompleteSampleStillRequiresExternallyAuthenticatedEvidence()
    {
        var adapter = Adapter();
        var result = adapter.PrepareReadOnly(Sha, "v5-source-revision");
        Assert.Equal("AUTHENTICATED_EVIDENCE_REQUIRED", result.Code);
        Assert.Equal(1, result.ItemsExamined);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(adapter.AllowsPersistence);
        Assert.False(adapter.AllowsInstitutionalWrite);
        Assert.False(adapter.CanBypassTenantAuthorization);
    }

    [Fact]
    public void DifferentShaAndRevisionFailClosed()
    {
        var adapter = Adapter();
        Assert.Equal("CHECK_SHA_MISMATCH",
            adapter.PrepareReadOnly(OtherSha, "v5-source-revision").Code);
        Assert.Equal("CHECK_SHA_MISMATCH",
            adapter.PrepareReadOnly(null, "v5-source-revision").Code);
        Assert.Equal("SOURCE_REVISION_MISMATCH",
            adapter.PrepareReadOnly(Sha, "stale").Code);
        Assert.Equal("SOURCE_REVISION_MISMATCH",
            adapter.PrepareReadOnly(Sha, null).Code);
    }

    [Fact]
    public void IncompleteAndUnownedStatesCannotCertify()
    {
        var pending = new V5OperationalAuditDomainRules.WorkItem(
            Id, "PENDIENTE", null, null, null, null, null);
        Assert.Equal("SOURCE_OR_A5_INCOMPLETE",
            Adapter(pending).PrepareReadOnly(Sha, "v5-source-revision").Code);
        Assert.Equal("UNAUTHORIZED_OWNER",
            Adapter(Complete() with { OwnerTaskId = null })
                .PrepareReadOnly(Sha, "v5-source-revision").Code);
    }

    [Fact]
    public void OwnA5AndExternalReviewerAreDenied()
    {
        Assert.Equal("INDEPENDENT_REVIEW_REQUIRED",
            Adapter(Complete() with { ReviewerTaskId = Author })
                .PrepareReadOnly(Sha, "v5-source-revision").Code);
        Assert.Equal("INDEPENDENT_REVIEW_REQUIRED",
            Adapter(Complete() with { ReviewerTaskId = "external-account" })
                .PrepareReadOnly(Sha, "v5-source-revision").Code);
    }

    [Fact]
    public void MissingEvidenceOrA5CannotPass()
    {
        Assert.Equal("CHECK_EVIDENCE_REQUIRED",
            Adapter(Complete() with { CheckEvidenceReference = null })
                .PrepareReadOnly(Sha, "v5-source-revision").Code);
        Assert.Equal("REVIEW_EVIDENCE_REQUIRED",
            Adapter(Complete() with { ReviewEvidenceReference = null })
                .PrepareReadOnly(Sha, "v5-source-revision").Code);
        Assert.Equal("SOURCE_OR_A5_INCOMPLETE",
            Adapter(Complete() with { ReviewerTaskId = null })
                .PrepareReadOnly(Sha, "v5-source-revision").Code);
    }

    [Fact]
    public void UnknownIdAndTaskCatalogMismatchFailClosed()
    {
        Assert.Equal("UNKNOWN_WORK_ITEM",
            Adapter(Complete() with { Id = "C15-05.M03.05" })
                .PrepareReadOnly(Sha, "v5-source-revision").Code);
        Assert.Equal("TASK_CATALOG_MISMATCH",
            Adapter(tasks: new[] { Author })
                .PrepareReadOnly(Sha, "v5-source-revision").Code);
    }

    [Fact]
    public void InputMutationsCannotAlterCapturedSnapshot()
    {
        var work = new List<V5OperationalAuditDomainRules.WorkItem> { Complete() };
        var ids = new HashSet<string> { Id };
        var tasks = new HashSet<string> { Author, Reviewer };
        var adapter = new V5OperationalAuditIsolatedSnapshotAdapter(
            "v5-source-revision", Sha, work, ids, tasks);
        work.Clear();
        ids.Clear();
        tasks.Clear();
        Assert.Equal("AUTHENTICATED_EVIDENCE_REQUIRED",
            adapter.PrepareReadOnly(Sha, "v5-source-revision").Code);
    }

    [Fact]
    public void NullCollectionsThrowBeforeSideEffects()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new V5OperationalAuditIsolatedSnapshotAdapter("v5", Sha, null!,
                new[] { Id }, new[] { Author, Reviewer }));
    }
}
