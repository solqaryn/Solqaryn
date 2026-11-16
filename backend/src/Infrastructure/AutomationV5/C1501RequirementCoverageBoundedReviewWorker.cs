namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// A future authenticated provider may implement this port. The built-in
/// adapter composes the existing C15-01.M03.04 authority/coverage checks.
/// This interface does not itself grant source authenticity or write access.
/// </summary>
public interface IC1501BoundedRequirementReviewPort
{
    ValueTask<C1501RequirementCoverageSnapshotAdapter.Result> InspectAsync(
        C1501RequirementCoverageSnapshotAdapter.Request request,
        CancellationToken cancellationToken);
}

/// <summary>Delegate to the existing single-entry authenticated-snapshot fence.</summary>
public sealed class C1501RequirementCoverageAdapterPort : IC1501BoundedRequirementReviewPort
{
    private readonly C1501RequirementCoverageSnapshotAdapter _adapter;
    public C1501RequirementCoverageAdapterPort(C1501RequirementCoverageSnapshotAdapter adapter) =>
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

    public ValueTask<C1501RequirementCoverageSnapshotAdapter.Result> InspectAsync(
        C1501RequirementCoverageSnapshotAdapter.Request request, CancellationToken cancellationToken) =>
        _adapter.InspectAsync(request, cancellationToken);
}

/// <summary>
/// C15-01.M03.05: bounded sequential coverage preview. NOT a hosted worker,
/// live Sheets connection, coverage certificate, A5 grant or institutional writer.
/// </summary>
public sealed class C1501RequirementCoverageBoundedReviewWorker
{
    public const int MaximumBatch = 16;
    private readonly IC1501BoundedRequirementReviewPort _port;

    public C1501RequirementCoverageBoundedReviewWorker(IC1501BoundedRequirementReviewPort port) =>
        _port = port ?? throw new ArgumentNullException(nameof(port));

    public sealed record Item(string RequirementKey, string WorkItemId, string Code);
    public sealed record Review(string Code, IReadOnlyList<Item> Items)
    {
        public bool GrantsInstitutionalWrite => false;
        public bool GrantsA5 => false;
        public bool CertifiesGlobalCoverage => false;
        public bool AuthorizesTenantRead => false;
        public bool AuthorizesQaOrProduction => false;
        public bool RequiresExternalCausalEvidence =>
            Code == "REVIEW_PREVIEW_REQUIRES_INDEPENDENT_EVIDENCE";
    }

    public async ValueTask<Review> InspectBatchAsync(
        IReadOnlyList<C1501RequirementCoverageSnapshotAdapter.Request>? requests,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (requests is null || requests.Count is < 1 or > MaximumBatch)
            return Deny("REVIEW_BATCH_REQUIRED_OR_TOO_LARGE");

        var workItems = new HashSet<string>(StringComparer.Ordinal);
        var requirements = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<Item>(requests.Count);
        string? expectedSha = null;

        // Validate the ENTIRE batch before touching an external/read-only port.
        // This prevents a malformed second entry from causing a partial read.
        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request is null ||
                !SafeKey(request.RequirementKey) ||
                !SafeKey(request.WorkItemId) ||
                !FullSha(request.ExpectedHeadSha) ||
                request.ClientControlsCatalog || request.RequestsA5OrWrite)
                return Deny("REVIEW_REQUEST_UNTRUSTED_OR_INVALID");

            if (expectedSha is null) expectedSha = request.ExpectedHeadSha;
            if (!string.Equals(expectedSha, request.ExpectedHeadSha, StringComparison.Ordinal))
                return Deny("MIXED_OR_STALE_SHA_DENIED");

            // Exact/ordinal uniqueness: a caller may not substitute its own comparer.
            if (!workItems.Add(request.WorkItemId!) ||
                !requirements.Add(request.RequirementKey!))
                return Deny("DUPLICATE_REQUIREMENT_OR_WORK_ITEM_DENIED");
        }

        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            C1501RequirementCoverageSnapshotAdapter.Result result;
            try { result = await _port.InspectAsync(request, cancellationToken); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            { return Deny("REVIEW_SOURCE_UNAVAILABLE"); }

            if (!result.RequiresExternalCausalEvidence || result.RowsExamined != 1)
                return Deny("REVIEW_SOURCE_REJECTED");

            rows.Add(new Item(request.RequirementKey!, request.WorkItemId!,
                "EXTERNAL_CAUSAL_EVIDENCE_REQUIRED"));
        }

        return new("REVIEW_PREVIEW_REQUIRES_INDEPENDENT_EVIDENCE", rows.AsReadOnly());
    }

    private static Review Deny(string code) => new(code, Array.Empty<Item>());
    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(c =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
    private static bool SafeKey(string? key) =>
        key is { Length: >= 3 and <= 128 } && key.All(c =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
            (c >= '0' && c <= '9') || c is '-' or '_' or '.');
}
