using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Read-only application preflight for a caller-supplied V5 operational audit.
/// Never treats a reference as authenticated GitHub/Sheets/CI evidence and never
/// grants a lease, records A5, changes a row, dispatches Tasks, or emits events.
/// </summary>
public sealed class V5OperationalAuditApplicationService
{
    public sealed record Request(
        string ExpectedHeadSha,
        string ObservedHeadSha,
        string ExpectedSourceRevision,
        V5OperationalAuditDomainRules.SourceSnapshot Source,
        IReadOnlyCollection<V5OperationalAuditDomainRules.WorkItem> Items,
        IReadOnlySet<string> ExpectedAuthorizedTaskIds,
        int MaxWorkItems = 2340);

    public sealed record Result(string Code, int ItemsExamined)
    {
        // Only an authenticated external adapter may decide whether a state
        // transition is allowed, after exact-SHA CI, CAS, A5 and readback.
        public bool AllowsInstitutionalWrite => false;
    }

    public Result Prepare(Request request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Source);
        ArgumentNullException.ThrowIfNull(request.Items);
        ArgumentNullException.ThrowIfNull(request.ExpectedAuthorizedTaskIds);

        if (!FullSha(request.ExpectedHeadSha) ||
            !FullSha(request.ObservedHeadSha) ||
            !string.Equals(request.ExpectedHeadSha, request.ObservedHeadSha,
                StringComparison.Ordinal))
            return Deny("CHECK_SHA_MISMATCH");

        if (string.IsNullOrWhiteSpace(request.ExpectedSourceRevision) ||
            !string.Equals(request.ExpectedSourceRevision,
                request.Source.SourceRevision, StringComparison.Ordinal))
            return Deny("SOURCE_REVISION_MISMATCH");

        if (request.MaxWorkItems < 1 || request.MaxWorkItems > 2340 ||
            request.Items.Count > request.MaxWorkItems ||
            request.Source.AuthorizedWorkItemIds is null ||
            request.Source.AuthorizedWorkItemIds.Count > request.MaxWorkItems)
            return Deny("CAPACITY_EXCEEDED");

        if (request.Source.AuthorizedTaskIds is null ||
            request.ExpectedAuthorizedTaskIds.Count < 2 ||
            request.Source.AuthorizedTaskIds.Count !=
                request.ExpectedAuthorizedTaskIds.Count ||
            !request.ExpectedAuthorizedTaskIds.All(
                id => request.Source.AuthorizedTaskIds.Contains(id)))
            return Deny("TASK_CATALOG_MISMATCH");

        var domain = V5OperationalAuditDomainRules.Evaluate(
            request.Source, request.Items);
        if (!domain.ReadyForExternalVerification)
            return new Result(domain.Code, domain.ItemsExamined);

        // A complete caller-supplied snapshot is NOT a verification of the
        // institutional source, reviewer identity, check-runs, or CAS outcome.
        return new Result("AUTHENTICATED_EVIDENCE_REQUIRED", domain.ItemsExamined);
    }

    /// <summary>
    /// Adds tenant/rbac denial before the existing read-only source preflight.
    /// Caller-supplied facts still require real authentication by a future
    /// adapter. Neither branch ever enables institutional writes.
    /// </summary>
    public Result PrepareWithTenantSecurityPreflight(
        Request request, V5OperationalAuditSecurityGate.Context context)
    {
        ArgumentNullException.ThrowIfNull(request);
        var security = V5OperationalAuditSecurityGate.Evaluate(context);
        if (!security.NeedsExternalAdapterVerification)
            return Deny(security.Code);
        return Prepare(request);
    }

    private static Result Deny(string code) => new(code, 0);

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
}
