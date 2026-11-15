using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C15-01.M03.04: isolated read-only adapter for a trusted V5 requirements snapshot.
/// Does not query Google Sheets, authenticate user claims, grant A5 or certify coverage.
/// A future concrete source must authenticate/revalidate each upstream read.
/// </summary>
public interface IC1501TrustedRequirementSnapshotSource
{
    ValueTask<C1501RequirementCoverageSnapshotAdapter.VerifiedSnapshot?> ReadAsync(
        string requirementKey, string workItemId, CancellationToken cancellationToken);
}

public sealed class C1501RequirementCoverageSnapshotAdapter
{
    private readonly IC1501TrustedRequirementSnapshotSource _source;
    private readonly V5RequirementCoverageProjectionService _projection = new();

    public C1501RequirementCoverageSnapshotAdapter(
        IC1501TrustedRequirementSnapshotSource source) =>
        _source = source ?? throw new ArgumentNullException(nameof(source));

    public sealed record Request(
        string? RequirementKey, string? WorkItemId, string? ExpectedHeadSha,
        bool ClientControlsCatalog = false, bool RequestsA5OrWrite = false);

    public sealed record VerifiedSnapshot(
        string? ObservedHeadSha,
        bool SourceAuthenticatedByBackend,
        bool ReviewerScopeCurrent,
        bool ReadbackAndAuditAvailable,
        IReadOnlySet<string>? AuthorizedRequirementKeys,
        IReadOnlySet<string>? ExistingCatalogWorkItemIds,
        IReadOnlyCollection<V5RequirementCoverageRules.Entry>? Mappings,
        IReadOnlyDictionary<string,V5RequirementCoverageProjectionService.WorkItemReadModel>? ObservedPlan);

    public sealed record Result(string Code, int RowsExamined)
    {
        public bool GrantsA5 => false;
        public bool GrantsInstitutionalWrites => false;
        public bool CertifiesCoverage => false;
        public bool AuthorizesTenantAccess => false;
        public bool RequiresExternalCausalEvidence =>
            Code == "EXTERNAL_REQUIREMENT_EVIDENCE_AND_A5_REQUIRED";
    }

    public async ValueTask<Result> InspectAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || !SafeKey(request.RequirementKey) ||
            !SafeKey(request.WorkItemId) || !FullSha(request.ExpectedHeadSha))
            return new("INVALID_REQUIREMENT_OR_SHA", 0);

        if (request.ClientControlsCatalog || request.RequestsA5OrWrite)
            return new("UNTRUSTED_CATALOG_OR_WRITE_DENIED", 0);

        VerifiedSnapshot? snapshot;
        try
        {
            snapshot = await _source.ReadAsync(
                request.RequirementKey!, request.WorkItemId!, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("TRUSTED_SOURCE_UNAVAILABLE", 0);
        }

        if (snapshot is null || !snapshot.SourceAuthenticatedByBackend ||
            !snapshot.ReviewerScopeCurrent || !snapshot.ReadbackAndAuditAvailable ||
            !string.Equals(snapshot.ObservedHeadSha, request.ExpectedHeadSha,
                StringComparison.Ordinal) ||
            snapshot.AuthorizedRequirementKeys is null ||
            snapshot.ExistingCatalogWorkItemIds is null ||
            snapshot.ObservedPlan is null || snapshot.Mappings is null)
            return new("CURRENT_INSTITUTIONAL_SNAPSHOT_REQUIRED", 0);

        if (snapshot.Mappings.Count is < 1 or > 2340 ||
            snapshot.ObservedPlan.Count is < 1 or > 2340)
            return new("COVERAGE_CAPACITY_OR_EMPTY", 0);

        // Never trust the comparer of a caller-provided IReadOnlySet.
        // An OrdinalIgnoreCase set must not expand the canonical catalog.
        if (!snapshot.AuthorizedRequirementKeys.Any(k =>
                string.Equals(k, request.RequirementKey, StringComparison.Ordinal)) ||
            !snapshot.ExistingCatalogWorkItemIds.Any(k =>
                string.Equals(k, request.WorkItemId, StringComparison.Ordinal)))
            return new("EXACT_CATALOG_MEMBERSHIP_REQUIRED", 0);

        var selected = snapshot.Mappings.Where(m =>
            string.Equals(m.RequirementKey, request.RequirementKey, StringComparison.Ordinal) &&
            string.Equals(m.WorkItemId, request.WorkItemId, StringComparison.Ordinal)).ToArray();
        if (selected.Length != 1 || snapshot.Mappings.Count != 1)
            return new("EXACT_UNAMBIGUOUS_MAPPING_REQUIRED", selected.Length);

        var canonicalRequirements = new HashSet<string>(
            snapshot.AuthorizedRequirementKeys, StringComparer.Ordinal);
        var canonicalWorkItems = new HashSet<string>(
            snapshot.ExistingCatalogWorkItemIds, StringComparer.Ordinal);
        var projected = _projection.Project(
            selected, snapshot.ObservedPlan, canonicalRequirements,
            canonicalWorkItems, request.ExpectedHeadSha!);

        if (!projected.Verdict.Accepted ||
            projected.Rows.Count != 1 ||
            projected.Rows[0].Outstanding.Count == 0)
            return new(projected.Verdict.Accepted ?
                "UNVERIFIED_PROJECTION_REJECTED" : projected.Verdict.Code, 0);

        // Even LISTO is never a proof of tests, SHA checks or an independent A5.
        return new("EXTERNAL_REQUIREMENT_EVIDENCE_AND_A5_REQUIRED", 1);
    }

    private static bool FullSha(string? s) =>
        s is { Length: 40 } && s.All(Uri.IsHexDigit);

    private static bool SafeKey(string? s) =>
        s is { Length: >= 3 and <= 128 } &&
        s.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
            (c >= '0' && c <= '9') || c is '.' or '-' or '_');
}
