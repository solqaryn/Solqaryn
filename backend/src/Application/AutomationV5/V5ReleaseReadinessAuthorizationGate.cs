using Rules = Solqaryn.Domain.AutomationV5.V5ReleaseReadinessDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// A principal obtained from an authenticated backend context, never a request DTO.
/// No implementation is registered by this microtask.
/// </summary>
public sealed record V5ReadinessVerifiedCaller(
    string TaskId, string TenantScope, string DevGrantId, string Environment,
    bool IsAuthenticated, bool IsMembershipActive, bool HasReadinessReviewGrant);

public interface IV5ReadinessAuthenticatedCallerSource
{
    ValueTask<V5ReadinessVerifiedCaller?> GetAuthenticatedCallerAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// DEV-only trusted-principal gate for the M03.02 readiness preflight.
/// This policy never authorizes release, records A5 or writes to institutional state.
/// A future authenticated backend adapter must supply the identity and evidence.
/// </summary>
public sealed class V5ReleaseReadinessAuthorizationGate
{
    private readonly IV5ReadinessAuthenticatedCallerSource _callers;
    private readonly IV5ReleaseReadinessVerifiedEvidenceSource _evidence;

    public V5ReleaseReadinessAuthorizationGate(
        IV5ReadinessAuthenticatedCallerSource callers,
        IV5ReleaseReadinessVerifiedEvidenceSource evidence)
    {
        _callers = callers ?? throw new ArgumentNullException(nameof(callers));
        _evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
    }

    public sealed record Verdict(string Code, bool NeedsExternalIndependentReview)
    {
        public bool AllowsRelease => false;
        public bool AllowsDeployment => false;
        public bool AllowsInstitutionalWrite => false;
        public bool GrantsTenantAccess => false;
    }

    public async Task<Verdict> EvaluateAsync(
        V5ReleaseReadinessApplicationService.Request? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || !Safe(request.ReleaseId))
            return Deny("RELEASE_REQUEST_REQUIRED");
        if (!FullSha(request.ExpectedSha) || !FullSha(request.ObservedSha) ||
            !string.Equals(request.ExpectedSha, request.ObservedSha,
                StringComparison.Ordinal))
            return Deny("RELEASE_SOURCE_SHA_MISMATCH");

        cancellationToken.ThrowIfCancellationRequested();
        var caller = await _callers.GetAuthenticatedCallerAsync(cancellationToken);
        if (caller is null || !caller.IsAuthenticated ||
            !caller.IsMembershipActive || !caller.HasReadinessReviewGrant ||
            !Safe(caller.TaskId) || !Safe(caller.DevGrantId) ||
            !Safe(caller.TenantScope) ||
            !string.Equals(caller.Environment, "dev", StringComparison.Ordinal))
            return Deny("AUTHENTICATED_DEV_REVIEWER_REQUIRED");

        var authority = await _evidence.GetAuthorityAsync(cancellationToken);
        if (authority is null || authority.AuthorizedTaskIds is null ||
            authority.AuthorizedDevGrantIds is null ||
            !FullSha(authority.ExpectedCommitSha) ||
            !string.Equals(authority.ExpectedCommitSha, request.ExpectedSha,
                StringComparison.Ordinal) ||
            !string.Equals(authority.AuthenticatedTenantScope, caller.TenantScope,
                StringComparison.Ordinal) ||
            !authority.AuthorizedTaskIds.Any(id =>
                string.Equals(id, caller.TaskId, StringComparison.Ordinal)) ||
            !authority.AuthorizedDevGrantIds.Any(id =>
                string.Equals(id, caller.DevGrantId, StringComparison.Ordinal)))
            return Deny("VERIFIED_DEV_AUTHORITY_REQUIRED");

        var candidate = await _evidence.GetCandidateAsync(
            request.ReleaseId!, cancellationToken);
        if (candidate is null ||
            !string.Equals(candidate.ReleaseId, request.ReleaseId,
                StringComparison.Ordinal))
            return Deny("VERIFIED_RELEASE_CANDIDATE_REQUIRED");

        // The authenticated principal, not the candidate DTO, is the reviewer.
        if (!string.Equals(candidate.ReviewerTaskId, caller.TaskId,
                StringComparison.Ordinal) ||
            string.Equals(candidate.AuthorTaskId, caller.TaskId,
                StringComparison.Ordinal) ||
            !string.Equals(candidate.TenantScope, caller.TenantScope,
                StringComparison.Ordinal) ||
            !string.Equals(candidate.DevGrantId, caller.DevGrantId,
                StringComparison.Ordinal) ||
            !string.Equals(candidate.Environment, "dev", StringComparison.Ordinal))
            return Deny("CALLER_SCOPE_OR_INDEPENDENCE_MISMATCH");

        var domain = Rules.Evaluate(authority, candidate);
        if (!domain.ReadyForExternalEvidenceReview)
            return Deny(domain.Code);

        // Even this positive result is NOT a release or A5 approval.
        return new Verdict("INDEPENDENT_AUTHENTICATED_RELEASE_REVIEW_REQUIRED", true);
    }

    private static Verdict Deny(string code) => new(code, false);
    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
    private static bool Safe(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 128 &&
        value.All(c => c >= '!' && c <= '~') &&
        !value.Contains("://", StringComparison.Ordinal) && !value.Contains('@');
}
