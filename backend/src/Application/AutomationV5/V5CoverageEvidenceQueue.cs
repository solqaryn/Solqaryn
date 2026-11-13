using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Produces bounded, read-only evidence-check intents from the existing V5 coverage projection.
/// Does not dispatch Tasks, mark A5 PASS, grant access, or write institutional state.
/// </summary>
public sealed class V5CoverageEvidenceQueue
{
    public sealed record EvidenceIntent(string WorkItemId, string RequirementKey,
        string State, string ExactHeadSha, IReadOnlyList<string> MissingEvidence);

    public sealed record Result(bool Accepted, string Code, IReadOnlyList<EvidenceIntent> Intents);

    private readonly V5RequirementCoverageProjectionService _projection = new();

    public Result Build(
        string actorTaskId,
        string scopeKey,
        IReadOnlySet<string> authorizedTaskIds,
        IReadOnlySet<string> authorizedScopeKeys,
        IReadOnlyCollection<V5RequirementCoverageRules.Entry> mappings,
        IReadOnlyDictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel> observedPlan,
        IReadOnlySet<string> authorizedRequirementKeys,
        IReadOnlySet<string> existingCatalogIds,
        string exactHeadSha,
        int maxBatch = 1000)
    {
        ArgumentNullException.ThrowIfNull(authorizedTaskIds);
        ArgumentNullException.ThrowIfNull(authorizedScopeKeys);
        ArgumentNullException.ThrowIfNull(mappings);
        ArgumentNullException.ThrowIfNull(observedPlan);
        ArgumentNullException.ThrowIfNull(authorizedRequirementKeys);
        ArgumentNullException.ThrowIfNull(existingCatalogIds);

        if (string.IsNullOrWhiteSpace(actorTaskId) || !authorizedTaskIds.Contains(actorTaskId))
            return Deny("UNAUTHORIZED_ACTOR");
        if (string.IsNullOrWhiteSpace(scopeKey) || !authorizedScopeKeys.Contains(scopeKey))
            return Deny("SCOPE_DENIED");
        if (exactHeadSha is not { Length: 40 } || !exactHeadSha.All(Uri.IsHexDigit))
            return Deny("CHECK_SHA_MISMATCH");
        if (maxBatch < 1 || maxBatch > 1000)
            return Deny("INVALID_CAPACITY");
        if (mappings.Count > maxBatch)
            return Deny("CAPACITY_REACHED");

        var projected = _projection.Project(
            mappings, observedPlan, authorizedRequirementKeys, existingCatalogIds, exactHeadSha);
        if (!projected.Verdict.Accepted)
            return Deny(projected.Verdict.Code);

        var queue = new List<EvidenceIntent>();
        foreach (var row in projected.Rows
            .OrderBy(x => x.WorkItemId, StringComparer.Ordinal)
            .ThenBy(x => x.RequirementKey, StringComparer.Ordinal))
        {
            // A5 independent reviewers may only be selected by an authorized consumer;
            // never put an author's own LISTO work into this author's review queue.
            if (row.State == "LISTO" &&
                string.Equals(row.OwnerTaskId, actorTaskId, StringComparison.Ordinal))
                continue;

            // The projection always retains EVIDENCE_NOT_VERIFIED even for LISTO:
            // a document link or a source state is not causal CI/A5 evidence.
            if (!row.Outstanding.Contains("EVIDENCE_NOT_VERIFIED", StringComparer.Ordinal))
                return Deny("UNVERIFIED_PROJECTION_CONTRACT");

            queue.Add(new EvidenceIntent(row.WorkItemId, row.RequirementKey,
                row.State, row.ObservedHeadSha,
                Array.AsReadOnly(row.Outstanding.ToArray())));
        }
        return new(true, "EVIDENCE_INTENTS_ONLY", queue.AsReadOnly());
    }

    private static Result Deny(string code) =>
        new(false, code, Array.Empty<EvidenceIntent>());
}
