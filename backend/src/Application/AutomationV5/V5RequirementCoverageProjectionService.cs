using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Pure read-model projection for V5 requirement coverage.
/// Caller-supplied snapshots are evidence references, never authorization or proof
/// that a runtime test or an independent review has passed.
/// </summary>
public sealed class V5RequirementCoverageProjectionService
{
    public sealed record WorkItemReadModel(
        string MicrotaskId,
        string State,
        string? OwnerTaskId,
        string ExpectedResult,
        string ObservedHeadSha);

    public sealed record CoverageRow(
        string RequirementKey,
        string WorkItemId,
        string SourceVersion,
        string ContractReference,
        string TestReference,
        string EvidenceReference,
        string State,
        string? OwnerTaskId,
        string ExpectedResult,
        string ObservedHeadSha,
        IReadOnlyList<string> Outstanding);

    public sealed record ProjectionResult(
        V5RequirementCoverageRules.Verdict Verdict,
        IReadOnlyList<CoverageRow> Rows);

    private static readonly HashSet<string> KnownStates = new(StringComparer.Ordinal)
    {
        "PENDIENTE", "EN_PROGRESO", "VALIDANDO", "LISTO"
    };

    public ProjectionResult Project(
        IReadOnlyCollection<V5RequirementCoverageRules.Entry> mappings,
        IReadOnlyDictionary<string, WorkItemReadModel> observedPlan,
        IReadOnlySet<string> authorizedRequirementKeys,
        IReadOnlySet<string> existingCatalogIds,
        string observedHeadSha)
    {
        ArgumentNullException.ThrowIfNull(mappings);
        ArgumentNullException.ThrowIfNull(observedPlan);
        ArgumentNullException.ThrowIfNull(authorizedRequirementKeys);
        ArgumentNullException.ThrowIfNull(existingCatalogIds);

        if (mappings.Count == 0)
            return Reject("COVERAGE_EMPTY");
        if (!IsCommitSha(observedHeadSha))
            return Reject("CHECK_SHA_MISMATCH");

        var domain = V5RequirementCoverageRules.ValidateBatch(
            mappings, authorizedRequirementKeys, existingCatalogIds);
        if (!domain.Accepted)
            return new ProjectionResult(domain, Array.Empty<CoverageRow>());

        var rows = new List<CoverageRow>(mappings.Count);
        foreach (var entry in mappings)
        {
            if (!observedPlan.TryGetValue(entry.WorkItemId, out var current) ||
                current is null || !string.Equals(current.MicrotaskId, entry.WorkItemId, StringComparison.Ordinal))
                return Reject("SNAPSHOT_MISSING");
            if (!KnownStates.Contains(current.State))
                return Reject("INVALID_STATE");
            if (!string.Equals(current.ObservedHeadSha, observedHeadSha, StringComparison.Ordinal))
                return Reject("SOURCE_REF_MOVED");
            if (string.IsNullOrWhiteSpace(current.ExpectedResult))
                return Reject("EXPECTED_RESULT_REQUIRED");

            // A reference alone is not causal proof: even LISTO requires external
            // exact-SHA check-runs, test artifacts and A5 readback when applicable.
            var outstanding = current.State == "LISTO"
                ? new[] { "EVIDENCE_NOT_VERIFIED" }
                : new[] { "WORK_ITEM_NOT_LISTO", "EVIDENCE_NOT_VERIFIED" };

            rows.Add(new CoverageRow(
                entry.RequirementKey, entry.WorkItemId, entry.SourceVersion,
                entry.ContractReference, entry.TestReference, entry.EvidenceReference,
                current.State, current.OwnerTaskId, current.ExpectedResult,
                observedHeadSha, Array.AsReadOnly(outstanding)));
        }

        return new ProjectionResult(
            V5RequirementCoverageRules.Verdict.Pass(), rows.AsReadOnly());
    }

    private static ProjectionResult Reject(string code) =>
        new(V5RequirementCoverageRules.Verdict.Reject(code), Array.Empty<CoverageRow>());

    private static bool IsCommitSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
}
