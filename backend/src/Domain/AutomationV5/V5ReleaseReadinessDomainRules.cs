using System.Text.RegularExpressions;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// Pure, fail-closed preflight for a prospective DEV release evidence packet.
/// All authorities and observations must be supplied by trusted application code.
/// This rule does not authenticate callers, query CI, authorize deployment,
/// execute rollback, grant A5, or certify real service availability.
/// </summary>
public static class V5ReleaseReadinessDomainRules
{
    private static readonly Regex FullShaPattern = new(
        "^[0-9a-fA-F]{40}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DigestPattern = new(
        "^sha256:[0-9a-fA-F]{64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public sealed record VerificationContext(
        string SourceRevision,
        string ExpectedCommitSha,
        string AuthenticatedTenantScope,
        IReadOnlySet<string> AuthorizedTaskIds,
        IReadOnlySet<string> RequiredCheckNames,
        IReadOnlySet<string> AuthorizedDevGrantIds);

    public sealed record CheckResult(
        string Name, string HeadSha, string Status, string Conclusion);

    public sealed record ObservedCase(
        string CaseId, string EvidenceReference, bool Executed);

    public sealed record Candidate(
        string ReleaseId,
        string CommitSha,
        string ManifestCommitSha,
        string ArtifactDigest,
        string Environment,
        string DevGrantId,
        string TenantScope,
        string AuthorTaskId,
        string ReviewerTaskId,
        string RollbackReference,
        string StopCondition,
        IReadOnlyCollection<CheckResult> Checks,
        IReadOnlyCollection<ObservedCase> AcceptanceCases);

    public readonly record struct Verdict(string Code)
    {
        public bool ReadyForExternalEvidenceReview =>
            Code == "READY_FOR_EXTERNAL_EVIDENCE_REVIEW";
    }

    public static Verdict Evaluate(VerificationContext authority, Candidate candidate)
    {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(authority.AuthorizedTaskIds);
        ArgumentNullException.ThrowIfNull(authority.RequiredCheckNames);
        ArgumentNullException.ThrowIfNull(authority.AuthorizedDevGrantIds);
        ArgumentNullException.ThrowIfNull(candidate.Checks);
        ArgumentNullException.ThrowIfNull(candidate.AcceptanceCases);

        if (!Safe(authority.SourceRevision) ||
            !FullSha(authority.ExpectedCommitSha) ||
            !Safe(authority.AuthenticatedTenantScope) ||
            authority.AuthorizedTaskIds.Count < 2 ||
            authority.RequiredCheckNames.Count == 0 ||
            authority.AuthorizedDevGrantIds.Count == 0)
            return new Verdict("UNVERIFIED_AUTHORITY");

        // This implementation is deliberately dev-only. PROD/QA need a separate
        // explicit authorization and integration, never inferred from A5/CI.
        if (!string.Equals(candidate.Environment, "dev", StringComparison.Ordinal) ||
            !Safe(candidate.DevGrantId) ||
            !authority.AuthorizedDevGrantIds.Contains(candidate.DevGrantId))
            return new Verdict("DEV_GRANT_REQUIRED");

        if (!Safe(candidate.ReleaseId) ||
            !FullSha(candidate.CommitSha) ||
            !string.Equals(candidate.CommitSha, authority.ExpectedCommitSha, StringComparison.Ordinal) ||
            !string.Equals(candidate.ManifestCommitSha, candidate.CommitSha, StringComparison.Ordinal) ||
            !DigestPattern.IsMatch(candidate.ArtifactDigest ?? ""))
            return new Verdict("RELEASE_MANIFEST_MISMATCH");

        if (!string.Equals(candidate.TenantScope, authority.AuthenticatedTenantScope,
                StringComparison.Ordinal))
            return new Verdict("TENANT_SCOPE_MISMATCH");

        if (!Safe(candidate.AuthorTaskId) ||
            !Safe(candidate.ReviewerTaskId) ||
            !authority.AuthorizedTaskIds.Contains(candidate.AuthorTaskId) ||
            !authority.AuthorizedTaskIds.Contains(candidate.ReviewerTaskId) ||
            string.Equals(candidate.AuthorTaskId, candidate.ReviewerTaskId, StringComparison.Ordinal))
            return new Verdict("INDEPENDENT_REVIEW_REQUIRED");

        if (!Safe(candidate.RollbackReference) || !Safe(candidate.StopCondition))
            return new Verdict("ROLLBACK_AND_STOP_CONDITION_REQUIRED");

        var seenChecks = new HashSet<string>(StringComparer.Ordinal);
        foreach (var check in candidate.Checks)
        {
            if (check is null || !Safe(check.Name) || !seenChecks.Add(check.Name))
                return new Verdict("DUPLICATE_OR_INVALID_CHECK");
            if (!string.Equals(check.HeadSha, candidate.CommitSha, StringComparison.Ordinal) ||
                !string.Equals(check.Status, "completed", StringComparison.Ordinal) ||
                !string.Equals(check.Conclusion, "success", StringComparison.Ordinal))
                return new Verdict("CHECK_NOT_SUCCESS_EXACT_SHA");
        }
        if (!authority.RequiredCheckNames.IsSubsetOf(seenChecks))
            return new Verdict("REQUIRED_CHECK_MISSING");

        if (candidate.AcceptanceCases.Count == 0)
            return new Verdict("OBSERVED_ACCEPTANCE_REQUIRED");

        var seenCases = new HashSet<string>(StringComparer.Ordinal);
        foreach (var observation in candidate.AcceptanceCases)
        {
            if (observation is null || !Safe(observation.CaseId) ||
                !seenCases.Add(observation.CaseId) ||
                !observation.Executed || !Safe(observation.EvidenceReference))
                return new Verdict("ACCEPTANCE_NOT_OBSERVED");
        }

        // Even success here is NOT release approval; external authentication,
        // provenance, review, and deployment gates remain mandatory.
        return new Verdict("READY_FOR_EXTERNAL_EVIDENCE_REVIEW");
    }

    private static bool FullSha(string? value) =>
        value is not null && FullShaPattern.IsMatch(value);

    private static bool Safe(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 512 &&
        value.All(c => !char.IsControl(c));
}
