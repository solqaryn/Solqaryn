using System.Text.RegularExpressions;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// Validates a caller-supplied, versioned audit snapshot without granting authority.
/// This is a pure domain preflight; it does NOT verify CI with GitHub, authenticate
/// a reviewer, lock Google Sheets, certify the source catalog or dispatch work.
/// </summary>
public static class V5OperationalAuditDomainRules
{
    private static readonly Regex WorkItemIdPattern = new(
        @"^C[0-9]{2}-[0-9]{2}\.M[0-9]{2}\.[0-9]{2}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public sealed record SourceSnapshot(
        string SourceRevision,
        IReadOnlySet<string> AuthorizedWorkItemIds,
        IReadOnlySet<string> AuthorizedTaskIds);

    public sealed record WorkItem(
        string Id,
        string State,
        string? OwnerTaskId,
        string? PublishedSha,
        string? ReviewerTaskId,
        string? CheckEvidenceReference,
        string? ReviewEvidenceReference);

    public readonly record struct Assessment(string Code, int ItemsExamined)
    {
        public bool ReadyForExternalVerification =>
            Code == "READY_FOR_EXTERNAL_VERIFICATION";
    }

    public static Assessment Evaluate(
        SourceSnapshot source,
        IReadOnlyCollection<WorkItem> items)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(source.AuthorizedWorkItemIds);
        ArgumentNullException.ThrowIfNull(source.AuthorizedTaskIds);

        if (string.IsNullOrWhiteSpace(source.SourceRevision) ||
            source.AuthorizedWorkItemIds.Count == 0 ||
            source.AuthorizedTaskIds.Count < 2)
            return new Assessment("UNVERIFIED_SOURCE_CATALOG", 0);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var incomplete = items.Count != source.AuthorizedWorkItemIds.Count;
        var examined = 0;

        foreach (var item in items)
        {
            if (item is null)
                return new Assessment("NULL_WORK_ITEM", examined);
            examined++;

            if (string.IsNullOrWhiteSpace(item.Id) ||
                !WorkItemIdPattern.IsMatch(item.Id) ||
                !source.AuthorizedWorkItemIds.Contains(item.Id))
                return new Assessment("UNKNOWN_WORK_ITEM", examined);
            if (!seen.Add(item.Id))
                return new Assessment("DUPLICATE_WORK_ITEM", examined);

            if (item.State == "PENDIENTE")
            {
                if (!string.IsNullOrWhiteSpace(item.OwnerTaskId) ||
                    !string.IsNullOrWhiteSpace(item.ReviewerTaskId))
                    return new Assessment("PENDING_ITEM_HAS_OWNER_OR_REVIEW", examined);
                incomplete = true;
                continue;
            }

            if (!AuthorizedTask(item.OwnerTaskId, source.AuthorizedTaskIds))
                return new Assessment("UNAUTHORIZED_OWNER", examined);

            if (item.State is "EN_PROGRESO" or "VALIDANDO")
            {
                if (!string.IsNullOrWhiteSpace(item.ReviewerTaskId))
                    return new Assessment("PREMATURE_INDEPENDENT_REVIEW", examined);
                incomplete = true;
                continue;
            }

            if (item.State != "LISTO")
                return new Assessment("UNKNOWN_STATE", examined);

            if (!FullSha(item.PublishedSha))
                return new Assessment("INVALID_PUBLICATION_SHA", examined);
            if (!SafeEvidence(item.CheckEvidenceReference))
                return new Assessment("CHECK_EVIDENCE_REQUIRED", examined);

            // A5 can only be attributed to a DIFFERENT allowlisted task.
            if (string.IsNullOrWhiteSpace(item.ReviewerTaskId))
            {
                incomplete = true;
                continue;
            }
            if (!AuthorizedTask(item.ReviewerTaskId, source.AuthorizedTaskIds) ||
                string.Equals(item.OwnerTaskId, item.ReviewerTaskId,
                    StringComparison.Ordinal))
                return new Assessment("INDEPENDENT_REVIEW_REQUIRED", examined);
            if (!SafeEvidence(item.ReviewEvidenceReference))
                return new Assessment("REVIEW_EVIDENCE_REQUIRED", examined);
        }

        if (incomplete || seen.Count != source.AuthorizedWorkItemIds.Count)
            return new Assessment("SOURCE_OR_A5_INCOMPLETE", examined);

        // Presence of references never equals authenticated PASS. Callers must
        // verify every source row, reviewer, original commit SHA and check run.
        return new Assessment("READY_FOR_EXTERNAL_VERIFICATION", examined);
    }

    private static bool AuthorizedTask(string? id, IReadOnlySet<string> allowlist) =>
        !string.IsNullOrWhiteSpace(id) && allowlist.Contains(id);

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    private static bool SafeEvidence(string? reference) =>
        !string.IsNullOrWhiteSpace(reference) && reference.Length <= 512 &&
        reference.All(c => !char.IsControl(c));
}
