using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class V5OperationalAuditDomainRulesTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static V5OperationalAuditDomainRules.SourceSnapshot Source() =>
        new("V5-approved-revision",
            new HashSet<string>(StringComparer.Ordinal)
            {
                "C15-05.M03.01", "C15-05.M03.02"
            },
            new HashSet<string>(StringComparer.Ordinal) { "task-author", "task-reviewer" });

    private static V5OperationalAuditDomainRules.WorkItem Reviewed(string id) =>
        new(id, "LISTO", "task-author", Sha, "task-reviewer",
            "github:sha-check-success", "bitacora:a5-independent");

    private static V5OperationalAuditDomainRules.Assessment Evaluate(
        params V5OperationalAuditDomainRules.WorkItem[] items) =>
        V5OperationalAuditDomainRules.Evaluate(Source(), items);

    [Fact]
    public void FullyReviewedCallerSnapshotOnlyBecomesReadyForExternalVerification()
    {
        var result = Evaluate(Reviewed("C15-05.M03.01"), Reviewed("C15-05.M03.02"));
        Assert.True(result.ReadyForExternalVerification);
        Assert.Equal("READY_FOR_EXTERNAL_VERIFICATION", result.Code);
        Assert.Equal(2, result.ItemsExamined);
    }

    [Fact]
    public void PendingOrValidatingDoesNotBecomePassOrReleaseDependencies()
    {
        var pending = new V5OperationalAuditDomainRules.WorkItem(
            "C15-05.M03.01", "PENDIENTE", null, null, null, null, null);
        var validating = Reviewed("C15-05.M03.02") with
        {
            State = "VALIDANDO", ReviewerTaskId = null
        };
        Assert.Equal("SOURCE_OR_A5_INCOMPLETE", Evaluate(pending, validating).Code);
        Assert.Equal("SOURCE_OR_A5_INCOMPLETE",
            Evaluate(Reviewed("C15-05.M03.01")).Code);
    }

    [Fact]
    public void RejectsUnknownDuplicateAndUnauthorizedOwner()
    {
        Assert.Equal("UNKNOWN_WORK_ITEM",
            Evaluate(Reviewed("C15-05.M03.01"), Reviewed("C99-99.M99.99")).Code);
        Assert.Equal("DUPLICATE_WORK_ITEM",
            Evaluate(Reviewed("C15-05.M03.01"), Reviewed("C15-05.M03.01")).Code);
        Assert.Equal("UNAUTHORIZED_OWNER",
            Evaluate(Reviewed("C15-05.M03.01") with { OwnerTaskId = "untrusted" },
                Reviewed("C15-05.M03.02")).Code);
    }

    [Fact]
    public void SelfReviewOrUnauthorizedReviewerFailsClosed()
    {
        Assert.Equal("INDEPENDENT_REVIEW_REQUIRED",
            Evaluate(Reviewed("C15-05.M03.01") with { ReviewerTaskId = "task-author" },
                Reviewed("C15-05.M03.02")).Code);
        Assert.Equal("INDEPENDENT_REVIEW_REQUIRED",
            Evaluate(Reviewed("C15-05.M03.01") with { ReviewerTaskId = "third-party" },
                Reviewed("C15-05.M03.02")).Code);
    }

    [Fact]
    public void BadShaMissingChecksAndUnsafeEvidenceAreRejected()
    {
        Assert.Equal("INVALID_PUBLICATION_SHA",
            Evaluate(Reviewed("C15-05.M03.01") with { PublishedSha = "short" },
                Reviewed("C15-05.M03.02")).Code);
        Assert.Equal("CHECK_EVIDENCE_REQUIRED",
            Evaluate(Reviewed("C15-05.M03.01") with { CheckEvidenceReference = " " },
                Reviewed("C15-05.M03.02")).Code);
        Assert.Equal("REVIEW_EVIDENCE_REQUIRED",
            Evaluate(Reviewed("C15-05.M03.01") with
                { ReviewEvidenceReference = "unsafe\nreference" },
                Reviewed("C15-05.M03.02")).Code);
    }

    [Fact]
    public void UnverifiedSourceCannotBeCertifiedByCallerSuppliedEntries()
    {
        var unverified = Source() with { SourceRevision = "" };
        Assert.Equal("UNVERIFIED_SOURCE_CATALOG",
            V5OperationalAuditDomainRules.Evaluate(unverified,
                new[] { Reviewed("C15-05.M03.01") }).Code);
    }
}
