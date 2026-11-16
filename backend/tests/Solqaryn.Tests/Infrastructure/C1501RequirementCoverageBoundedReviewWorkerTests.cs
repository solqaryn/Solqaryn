using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1501RequirementCoverageBoundedReviewWorkerTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private sealed class Port : IC1501BoundedRequirementReviewPort
    {
        public int Calls;
        public Exception? Error;
        public C1501RequirementCoverageSnapshotAdapter.Result Result =
            new("EXTERNAL_REQUIREMENT_EVIDENCE_AND_A5_REQUIRED", 1);
        public ValueTask<C1501RequirementCoverageSnapshotAdapter.Result> InspectAsync(
            C1501RequirementCoverageSnapshotAdapter.Request request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Result);
        }
    }

    private static C1501RequirementCoverageSnapshotAdapter.Request Req(
        string key = "req.a", string work = "C15-01.M03.01", string sha = Sha) =>
        new(key, work, sha);

    private static C1501RequirementCoverageBoundedReviewWorker Worker(Port? port = null) =>
        new(port ?? new());

    private static void NoGrant(C1501RequirementCoverageBoundedReviewWorker.Review value)
    {
        Assert.False(value.GrantsInstitutionalWrite);
        Assert.False(value.GrantsA5);
        Assert.False(value.CertifiesGlobalCoverage);
        Assert.False(value.AuthorizesTenantRead);
        Assert.False(value.AuthorizesQaOrProduction);
    }

    [Fact]
    public async Task PositiveBoundedPreviewStillRequiresExternalIndependentEvidence()
    {
        var port = new Port();
        var result = await Worker(port).InspectBatchAsync(new[] {
            Req("req.a", "C15-01.M03.01"), Req("req.b", "C15-01.M03.02")
        });
        Assert.Equal("REVIEW_PREVIEW_REQUIRES_INDEPENDENT_EVIDENCE", result.Code);
        Assert.True(result.RequiresExternalCausalEvidence);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, r =>
            Assert.Equal("EXTERNAL_CAUSAL_EVIDENCE_REQUIRED", r.Code));
        Assert.Equal(2, port.Calls);
        NoGrant(result);
    }

    [Fact]
    public async Task EmptyNullAndOverCapacityNeverReadTrustedPort()
    {
        var port = new Port();
        Assert.Equal("REVIEW_BATCH_REQUIRED_OR_TOO_LARGE",
            (await Worker(port).InspectBatchAsync(null)).Code);
        Assert.Equal("REVIEW_BATCH_REQUIRED_OR_TOO_LARGE",
            (await Worker(port).InspectBatchAsync(Array.Empty<C1501RequirementCoverageSnapshotAdapter.Request>())).Code);
        var many = Enumerable.Range(0, 17).Select(i =>
            Req("req." + i, "C15-01.M03." + i.ToString("D2"))).ToArray();
        Assert.Equal("REVIEW_BATCH_REQUIRED_OR_TOO_LARGE",
            (await Worker(port).InspectBatchAsync(many)).Code);
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public async Task BatchValidationIsAllOrNothingBeforeFirstSourceRead()
    {
        var port = new Port();
        var batch = new[] { Req(), Req("req.b", "C15-01.M03.02") with { RequestsA5OrWrite = true } };
        var result = await Worker(port).InspectBatchAsync(batch);
        Assert.Equal("REVIEW_REQUEST_UNTRUSTED_OR_INVALID", result.Code);
        Assert.Empty(result.Items);
        Assert.Equal(0, port.Calls);
        NoGrant(result);
    }

    [Fact]
    public async Task ClientCatalogControlAndInvalidIdentifiersFailClosed()
    {
        var port = new Port();
        var requests = new[] {
            Req() with { ClientControlsCatalog = true },
            Req("bad space", "C15-01.M03.01"),
            Req("req.a", "token=SECRET"),
            Req("req.a", "C15-01.M03.01", "short")
        };
        foreach (var request in requests)
            Assert.Equal("REVIEW_REQUEST_UNTRUSTED_OR_INVALID",
                (await Worker(port).InspectBatchAsync(new[] { request })).Code);
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public async Task MixedHeadShaCannotMixDifferentInstitutionalSnapshots()
    {
        var port = new Port();
        var result = await Worker(port).InspectBatchAsync(new[] {
            Req(), Req("req.b", "C15-01.M03.02", OtherSha)
        });
        Assert.Equal("MIXED_OR_STALE_SHA_DENIED", result.Code);
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public async Task DuplicateRequirementsOrWorkItemsAreRejectedOrdinally()
    {
        var port = new Port();
        Assert.Equal("DUPLICATE_REQUIREMENT_OR_WORK_ITEM_DENIED",
            (await Worker(port).InspectBatchAsync(new[] {
                Req("req.a", "C15-01.M03.01"), Req("req.a", "C15-01.M03.02")
            })).Code);
        Assert.Equal("DUPLICATE_REQUIREMENT_OR_WORK_ITEM_DENIED",
            (await Worker(port).InspectBatchAsync(new[] {
                Req("req.a", "C15-01.M03.01"), Req("req.b", "C15-01.M03.01")
            })).Code);
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public async Task CaseDistinctKeysDoNotUsePermissiveComparers()
    {
        var port = new Port();
        var result = await Worker(port).InspectBatchAsync(new[] {
            Req("req.a", "C15-01.M03.01"), Req("REQ.A", "C15-01.M03.02")
        });
        Assert.True(result.RequiresExternalCausalEvidence);
        Assert.Equal(2, port.Calls);
    }

    [Fact]
    public async Task ProviderDenialDoesNotReturnPartialAcceptedRows()
    {
        var port = new Port { Result = new("UNVERIFIED_PROJECTION_REJECTED", 0) };
        var result = await Worker(port).InspectBatchAsync(new[] { Req() });
        Assert.Equal("REVIEW_SOURCE_REJECTED", result.Code);
        Assert.Empty(result.Items);
        NoGrant(result);
    }

    [Fact]
    public async Task ProviderExceptionIsOpaqueAndFailClosed()
    {
        var port = new Port { Error = new InvalidOperationException("private tenant information") };
        var result = await Worker(port).InspectBatchAsync(new[] { Req() });
        Assert.Equal("REVIEW_SOURCE_UNAVAILABLE", result.Code);
        Assert.Empty(result.Items);
        NoGrant(result);
    }

    [Fact]
    public async Task CancellationDoesNotReachProvider()
    {
        var port = new Port();
        using var token = new CancellationTokenSource();
        token.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Worker(port).InspectBatchAsync(new[] { Req() }, token.Token));
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public void NullPortIsRejectedAtConstruction() =>
        Assert.Throws<ArgumentNullException>(() =>
            new C1501RequirementCoverageBoundedReviewWorker(null!));
}
