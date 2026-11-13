using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Bounded, deterministic selection of a V5 claim intent from trusted sources.
/// Does not run Tasks, send events, write Sheets, or grant a distributed lease.
/// A caller must separately enforce JWT/RBAC/tenant, CAS and institutional readback.
/// </summary>
public sealed class V5WorkItemQueueSelector
{
    public sealed record QueueEntry(int PlanRow, V5WorkItemRules.Snapshot WorkItem,
        bool DependenciesOk, bool DevelopmentEnabled, bool DagValidated,
        bool DagGateOpen, string SourceHeadSha, string ConflictArea);

    public sealed record Selection(bool Allowed, string Code, int? PlanRow,
        string? ConflictArea, V5WorkItemTransitionPlanner.TransitionIntent? ClaimIntent);

    private readonly V5WorkItemTransitionPlanner _planner = new();

    public Selection PrepareNext(IEnumerable<QueueEntry> institutionalSnapshots,
        string actorTaskId, IReadOnlySet<string> catalogIds,
        IReadOnlySet<string> authorizedTaskIds, IReadOnlySet<string> trustedConflictAreas,
        IReadOnlySet<string> activelyWrittenAreas, string expectedHeadSha,
        int maxCandidates = 1000)
    {
        ArgumentNullException.ThrowIfNull(institutionalSnapshots);
        ArgumentNullException.ThrowIfNull(catalogIds);
        ArgumentNullException.ThrowIfNull(authorizedTaskIds);
        ArgumentNullException.ThrowIfNull(trustedConflictAreas);
        ArgumentNullException.ThrowIfNull(activelyWrittenAreas);
        if (string.IsNullOrWhiteSpace(actorTaskId) || !authorizedTaskIds.Contains(actorTaskId))
            return Deny("UNAUTHORIZED_ACTOR");
        if (!IsSha(expectedHeadSha))
            return Deny("CHECK_SHA_MISMATCH");
        if (maxCandidates < 1 || maxCandidates > 1000)
            return Deny("INVALID_CAPACITY");

        var snapshots = institutionalSnapshots.Take(maxCandidates + 1).ToArray();
        if (snapshots.Length > maxCandidates)
            return Deny("CAPACITY_REACHED");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var rows = new HashSet<int>();
        foreach (var entry in snapshots)
        {
            if (entry is null || entry.WorkItem is null || entry.PlanRow <= 1 ||
                string.IsNullOrWhiteSpace(entry.WorkItem.Id) ||
                !catalogIds.Contains(entry.WorkItem.Id) ||
                !ids.Add(entry.WorkItem.Id) || !rows.Add(entry.PlanRow) ||
                !IsSha(entry.SourceHeadSha) ||
                !string.Equals(entry.SourceHeadSha, expectedHeadSha, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(entry.ConflictArea) ||
                !trustedConflictAreas.Contains(entry.ConflictArea))
                return Deny("UNTRUSTED_QUEUE_SNAPSHOT");
        }

        foreach (var entry in snapshots.OrderBy(x => x.PlanRow))
        {
            if (activelyWrittenAreas.Contains(entry.ConflictArea))
                continue;
            var prepared = _planner.PrepareClaim(entry.WorkItem, actorTaskId,
                catalogIds, authorizedTaskIds, entry.DependenciesOk,
                entry.DevelopmentEnabled, entry.DagValidated, entry.DagGateOpen);
            if (prepared.Allowed && prepared.Intent is not null)
                return new(true, "CLAIM_INTENT_ONLY", entry.PlanRow,
                    entry.ConflictArea, prepared.Intent);
        }
        return Deny("NO_ELIGIBLE_CANDIDATE");
    }

    private static Selection Deny(string code) => new(false, code, null, null, null);
    private static bool IsSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
}
