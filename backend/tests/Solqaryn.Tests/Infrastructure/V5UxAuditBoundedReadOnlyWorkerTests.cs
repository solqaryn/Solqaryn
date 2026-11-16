using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using System.Collections;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5UxAuditBoundedReadOnlyWorkerTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static V5UxAuditAuthorizedApplicationService.TrustedAuthority Authority() =>
        new(31, "tenant-A", Sha, true, true, true, true, true,
            new HashSet<string>(StringComparer.Ordinal) { "portal" },
            new HashSet<string>(StringComparer.Ordinal) { "contrast" });

    private static V5UxAuditApplicationService.Request Request() =>
        new(Sha, Sha, "tenant-A", true,
            new HashSet<string>(StringComparer.Ordinal) { "portal" },
            new HashSet<string>(StringComparer.Ordinal) { "contrast" },
            new[] { new V5UxAuditRules.Observation(
                "portal", "contrast", "tenant-A", Sha, "ci:synthetic-only",
                5m, 4.5m, "MIN") });

    private static V5UxAuditBoundedReadOnlyWorker.Candidate Item(
        string key = "ux-001", string tenant = "tenant-A", string sha = Sha) =>
        new(key, Request(), Authority(), sha, tenant);

    private static V5UxAuditBoundedReadOnlyWorker Worker() => new();

    [Fact]
    public void CoherentBatchRemainsStructuralAndNeverCertifiesExternalEvidence()
    {
        var result = Worker().EvaluateReadOnly(new[] { Item(), Item("ux-002") });
        Assert.Equal("STRUCTURAL_REVIEW_ONLY", result.Code);
        Assert.Equal(2, result.ItemsEvaluated);
        Assert.All(result.Items, x =>
        {
            Assert.Equal("EXTERNAL_EVIDENCE_AND_A5_REQUIRED", x.Code);
            Assert.Equal(1, x.ObservationsReviewed);
        });
        Assert.False(result.GrantsAccess);
        Assert.False(result.AllowsPersistence);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.CertifiesExternalMeasurement);
        Assert.False(result.AuthorizesProduction);
        Assert.False(result.CertifiesA5);
    }

    [Fact]
    public void EmptyOversizedAndNullBatchFailClosed()
    {
        Assert.Equal("BATCH_CAPACITY_DENIED", Worker().EvaluateReadOnly(null).Code);
        Assert.Equal("BATCH_CAPACITY_DENIED", Worker().EvaluateReadOnly(
            Array.Empty<V5UxAuditBoundedReadOnlyWorker.Candidate>()).Code);
        var tooMany = Enumerable.Range(0, 33).Select(i => Item("ux-" + i)).ToArray();
        Assert.Equal("BATCH_CAPACITY_DENIED", Worker().EvaluateReadOnly(tooMany).Code);
    }

    [Fact]
    public void DuplicateAndUnsafeIdempotencyKeysFailBeforePartialEvaluation()
    {
        Assert.Equal("INVALID_OR_DUPLICATE_WORK_KEY",
            Worker().EvaluateReadOnly(new[] { Item(), Item() }).Code);
        Assert.Equal("INVALID_OR_DUPLICATE_WORK_KEY",
            Worker().EvaluateReadOnly(new[] { Item("tenant-A/secret") }).Code);
    }

    [Fact]
    public void MixedTenantAndMixedSourceShaAreRejectedAtomically()
    {
        var crossTenant = Worker().EvaluateReadOnly(new[] { Item(), Item("two", "tenant-B") });
        Assert.Equal("MIXED_TENANT_OR_SOURCE_BATCH", crossTenant.Code);
        Assert.Empty(crossTenant.Items);
        var crossSha = Worker().EvaluateReadOnly(new[] { Item(), Item("two", sha: OtherSha) });
        Assert.Equal("MIXED_TENANT_OR_SOURCE_BATCH", crossSha.Code);
        Assert.Equal(0, crossSha.ItemsEvaluated);
    }

    [Fact]
    public void UnknownShaAndMissingTenantCannotEstablishBatchContext()
    {
        Assert.Equal("VERIFIED_BATCH_CONTEXT_REQUIRED",
            Worker().EvaluateReadOnly(new[] { Item(sha: "short") }).Code);
        Assert.Equal("VERIFIED_BATCH_CONTEXT_REQUIRED",
            Worker().EvaluateReadOnly(new[] { Item(tenant: "") }).Code);
    }

    [Fact]
    public void RevocationAndUnverifiedJwtNeverProduceStructuralSuccess()
    {
        var revoked = Item() with
        {
            Authority = Authority() with { CurrentMembershipVerified = false }
        };
        var badJwt = Item("second") with
        {
            Authority = Authority() with { JwtValidatedByServer = false }
        };
        var result = Worker().EvaluateReadOnly(new[] { revoked, badJwt });
        Assert.Equal("SOME_ITEMS_DENIED", result.Code);
        Assert.All(result.Items,
            x => Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED", x.Code));
    }

    [Fact]
    public void InvalidObservationAndCrossTenantAuthorityDenyWithoutGrants()
    {
        var invalid = Item() with
        {
            Request = Request() with
            {
                Observations = new[]
                {
                    new V5UxAuditRules.Observation("portal", "contrast",
                        "tenant-A", Sha, "ci:synthetic", 2m, 4.5m, "MIN")
                }
            }
        };
        var crossed = Item("second") with
        {
            Authority = Authority() with { ServerTenantScope = "tenant-B" }
        };
        var result = Worker().EvaluateReadOnly(new[] { invalid, crossed });
        Assert.Equal("SOME_ITEMS_DENIED", result.Code);
        Assert.Equal("BUDGET_EXCEEDED", result.Items[0].Code);
        Assert.Equal("TRUSTED_TENANT_CONTEXT_MISMATCH", result.Items[1].Code);
        Assert.False(result.GrantsAccess);
    }

    [Fact]
    public void CancelledBatchReturnsNoPartialResultsOrWrites()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = Worker().EvaluateReadOnly(new[] { Item() }, cts.Token);
        Assert.Equal("CANCELLED_NO_WRITE", result.Code);
        Assert.Empty(result.Items);
        Assert.False(result.AllowsInstitutionalWrite);
    }

    [Fact]
    public void NullSnapshotCollectionIsRejectedBeforePartialReview()
    {
        var malformed = Item() with
        {
            Request = Request() with { Observations = null! }
        };
        var result = Worker().EvaluateReadOnly(new[] { malformed });
        Assert.Equal("INVALID_WORK_SNAPSHOT", result.Code);
        Assert.Empty(result.Items);
    }

    // Adversarial IReadOnlyCollection.Count is not a security boundary.
    private sealed class MisreportedCollection : IReadOnlyCollection<V5UxAuditBoundedReadOnlyWorker.Candidate>
    {
        private readonly int _reportedCount;
        private readonly IEnumerable<V5UxAuditBoundedReadOnlyWorker.Candidate> _items;

        public MisreportedCollection(int reportedCount,
            IEnumerable<V5UxAuditBoundedReadOnlyWorker.Candidate> items)
        {
            _reportedCount = reportedCount;
            _items = items;
        }

        public int Count => _reportedCount;

        public IEnumerator<V5UxAuditBoundedReadOnlyWorker.Candidate> GetEnumerator() =>
            _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static IEnumerable<V5UxAuditBoundedReadOnlyWorker.Candidate> ThrowingSource()
    {
        yield return Item();
        throw new InvalidOperationException("synthetic-private-enumerator-message");
    }

    private static IEnumerable<V5UxAuditBoundedReadOnlyWorker.Candidate> CancellingSource(
        CancellationTokenSource source)
    {
        yield return Item();
        source.Cancel();
        yield return Item("ux-002");
    }

    [Fact]
    public void LyingCountCannotBypassThirtyTwoItemHardLimit()
    {
        var exceeds = new MisreportedCollection(1,
            Enumerable.Range(0, 33).Select(i => Item("ux-" + i)));
        var result = Worker().EvaluateReadOnly(exceeds);
        Assert.Equal("BATCH_CAPACITY_DENIED", result.Code);
        Assert.Equal(0, result.ItemsEvaluated);
        Assert.Empty(result.Items);
        Assert.False(result.AllowsInstitutionalWrite);
    }

    [Fact]
    public void CountIsNotTrustedAndActualSafeSizeRemainsValid()
    {
        var one = new MisreportedCollection(999, new[] { Item() });
        var result = Worker().EvaluateReadOnly(one);
        Assert.Equal("STRUCTURAL_REVIEW_ONLY", result.Code);
        Assert.Single(result.Items);
        Assert.False(result.CertifiesExternalMeasurement);
    }

    [Fact]
    public void EnumeratorExceptionDeniesWithoutPartialResultsOrLeakingMessage()
    {
        var source = new MisreportedCollection(2, ThrowingSource());
        var result = Worker().EvaluateReadOnly(source);
        Assert.Equal("INVALID_WORK_SNAPSHOT", result.Code);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.ItemsEvaluated);
        Assert.DoesNotContain("synthetic-private", result.Code);
    }

    [Fact]
    public void CancellationDuringEnumerationDeniesWithoutPartialResults()
    {
        using var cts = new CancellationTokenSource();
        var source = new MisreportedCollection(2, CancellingSource(cts));
        var result = Worker().EvaluateReadOnly(source, cts.Token);
        Assert.Equal("CANCELLED_NO_WRITE", result.Code);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.ItemsEvaluated);
    }
}
