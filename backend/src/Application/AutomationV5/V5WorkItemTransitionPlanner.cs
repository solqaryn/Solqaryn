using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Builds conditional V5 lifecycle transition intents from verified snapshots.
/// This is a pure application use case: callers must independently authorize access,
/// execute an atomic compare-and-swap in their storage adapter, append A:Z evidence,
/// and verify the institutional readback before reporting a transition.
/// </summary>
public sealed class V5WorkItemTransitionPlanner
{
    public sealed record TransitionIntent(
        string MicrotaskId,
        string ExpectedState,
        string? ExpectedOwnerTaskId,
        string RequestedState,
        string RequestedOwnerTaskId,
        string? CandidateFingerprint);

    public sealed record Preparation(
        V5WorkItemRules.Decision Decision,
        TransitionIntent? Intent)
    {
        public bool Allowed => Decision.Allowed;
    }

    public Preparation PrepareClaim(
        V5WorkItemRules.Snapshot item,
        string actorTaskId,
        IReadOnlySet<string> catalogIds,
        IReadOnlySet<string> authorizedTaskIds,
        bool queueDependenciesOk,
        bool queueDevelopmentEnabled,
        bool dagValidated,
        bool dagGateOpen)
    {
        var decision = V5WorkItemRules.CanClaim(
            item, actorTaskId, catalogIds, authorizedTaskIds,
            queueDependenciesOk, queueDevelopmentEnabled, dagValidated, dagGateOpen);

        return Build(decision, item, "EN_PROGRESO", actorTaskId, null);
    }

    public Preparation PrepareValidating(
        V5WorkItemRules.Snapshot item,
        string actorTaskId,
        IReadOnlySet<string> authorizedTaskIds,
        string candidateFingerprint)
    {
        ArgumentNullException.ThrowIfNull(authorizedTaskIds);
        if (!authorizedTaskIds.Contains(actorTaskId))
            return Deny("UNAUTHORIZED_ACTOR");

        var decision = V5WorkItemRules.CanMarkValidating(item, actorTaskId, candidateFingerprint);
        return Build(decision, item, "VALIDANDO", actorTaskId, candidateFingerprint);
    }

    public Preparation PrepareCloseDev(
        V5WorkItemRules.Snapshot item,
        string actorTaskId,
        IReadOnlySet<string> authorizedTaskIds,
        string publishedCommitSha,
        bool a0Passed,
        bool a1Passed,
        bool a2Passed,
        bool a3Passed,
        bool a4Passed,
        bool a5PendingRecorded)
    {
        ArgumentNullException.ThrowIfNull(authorizedTaskIds);
        if (!authorizedTaskIds.Contains(actorTaskId))
            return Deny("UNAUTHORIZED_ACTOR");

        var decision = V5WorkItemRules.CanCloseDev(
            item, actorTaskId, publishedCommitSha, a0Passed, a1Passed,
            a2Passed, a3Passed, a4Passed, a5PendingRecorded);
        return Build(decision, item, "LISTO", actorTaskId, publishedCommitSha);
    }

    public V5WorkItemRules.Decision CheckIndependentA5(
        V5WorkItemRules.Snapshot item,
        string authorTaskId,
        string reviewerTaskId,
        IReadOnlySet<string> authorizedTaskIds) =>
        V5WorkItemRules.CanReviewA5(item, authorTaskId, reviewerTaskId, authorizedTaskIds);

    private static Preparation Deny(string code) =>
        new(V5WorkItemRules.Decision.Deny(code), null);

    private static Preparation Build(
        V5WorkItemRules.Decision decision,
        V5WorkItemRules.Snapshot item,
        string state,
        string actorTaskId,
        string? fingerprint)
    {
        // No intent is emitted for a denied operation. No state is mutated here.
        return decision.Allowed
            ? new Preparation(decision, new TransitionIntent(
                item.Id, item.State, item.OwnerTaskId,
                state, actorTaskId, fingerprint))
            : new Preparation(decision, null);
    }
}
