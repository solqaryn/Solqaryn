namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// Pure validation rules for the V5 commercial work-item lifecycle.
/// Callers must obtain a real exclusive claim/CAS before persisting a transition.
/// No Google Sheets writes, dispatch, authorization grant or tenant access is performed here.
/// </summary>
public static class V5WorkItemRules
{
    public sealed record Snapshot(string Id, string State, string? OwnerTaskId);
    public readonly record struct Decision(bool Allowed, string Code)
    {
        public static Decision Permit() => new(true, "OK");
        public static Decision Deny(string code) => new(false, code);
    }

    public static Decision CanClaim(
        Snapshot item,
        string actorTaskId,
        IReadOnlySet<string> catalogIds,
        IReadOnlySet<string> authorizedTaskIds,
        bool queueDependenciesOk,
        bool queueDevelopmentEnabled,
        bool dagValidated,
        bool dagGateOpen)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(catalogIds);
        ArgumentNullException.ThrowIfNull(authorizedTaskIds);

        if (string.IsNullOrWhiteSpace(item.Id) || !catalogIds.Contains(item.Id))
            return Decision.Deny("INVALID_ID");
        if (string.IsNullOrWhiteSpace(actorTaskId) || !authorizedTaskIds.Contains(actorTaskId))
            return Decision.Deny("UNAUTHORIZED_ACTOR");
        if (item.State != "PENDIENTE" || !string.IsNullOrWhiteSpace(item.OwnerTaskId))
            return Decision.Deny("CLAIM_CONFLICT");
        if (!queueDependenciesOk || !queueDevelopmentEnabled || !dagValidated || !dagGateOpen)
            return Decision.Deny("DEPENDENCY_BLOCKED");

        return Decision.Permit();
    }

    public static Decision CanMarkValidating(Snapshot item, string actorTaskId, string candidateFingerprint)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.State != "EN_PROGRESO" || string.IsNullOrWhiteSpace(item.OwnerTaskId) ||
            !string.Equals(item.OwnerTaskId, actorTaskId, StringComparison.Ordinal))
            return Decision.Deny("OWNER_OR_STATE_MISMATCH");
        if (string.IsNullOrWhiteSpace(candidateFingerprint))
            return Decision.Deny("CANDIDATE_INCOMPLETE");
        return Decision.Permit();
    }

    public static Decision CanCloseDev(
        Snapshot item,
        string actorTaskId,
        string publishedCommitSha,
        bool a0Passed,
        bool a1Passed,
        bool a2Passed,
        bool a3Passed,
        bool a4Passed,
        bool a5PendingRecorded)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.State != "VALIDANDO" || string.IsNullOrWhiteSpace(item.OwnerTaskId) ||
            !string.Equals(item.OwnerTaskId, actorTaskId, StringComparison.Ordinal))
            return Decision.Deny("OWNER_OR_STATE_MISMATCH");
        if (!IsFullCommitSha(publishedCommitSha))
            return Decision.Deny("CHECK_SHA_MISMATCH");
        if (!a0Passed || !a1Passed || !a2Passed || !a3Passed || !a4Passed || !a5PendingRecorded)
            return Decision.Deny("AUDIT_REQUIRED");
        return Decision.Permit();
    }

    public static Decision CanReviewA5(
        Snapshot item,
        string authorTaskId,
        string reviewerTaskId,
        IReadOnlySet<string> authorizedTaskIds)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(authorizedTaskIds);
        if (item.State != "LISTO" || !string.Equals(item.OwnerTaskId, authorTaskId, StringComparison.Ordinal))
            return Decision.Deny("OWNER_OR_STATE_MISMATCH");
        if (string.IsNullOrWhiteSpace(authorTaskId) || !authorizedTaskIds.Contains(authorTaskId) ||
            string.IsNullOrWhiteSpace(reviewerTaskId) || !authorizedTaskIds.Contains(reviewerTaskId) ||
            string.Equals(authorTaskId, reviewerTaskId, StringComparison.Ordinal))
            return Decision.Deny("REVIEW_NOT_INDEPENDENT");
        return Decision.Permit();
    }

    private static bool IsFullCommitSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
}
