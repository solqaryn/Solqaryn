using Rules = Solqaryn.Domain.AutomationV5.V5ReleaseReadinessDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Trusted backend integration boundary for DEV-only release evidence.
/// Implementations must authenticate grants, source, CI and tenant server-side.
/// No untrusted browser assertion may populate these records as authority.
/// </summary>
public interface IV5ReleaseReadinessVerifiedEvidenceSource
{
    ValueTask<Rules.VerificationContext?> GetAuthorityAsync(CancellationToken cancellationToken);
    ValueTask<Rules.Candidate?> GetCandidateAsync(string releaseId, CancellationToken cancellationToken);
}

/// <summary>
/// C14-04.M03.02: read-only Application preflight over M03.01 Domain rules.
/// Never deploys, grants access, approves A5, changes environment or certifies CI.
/// </summary>
public sealed class V5ReleaseReadinessApplicationService
{
    private readonly IV5ReleaseReadinessVerifiedEvidenceSource _source;

    public V5ReleaseReadinessApplicationService(IV5ReleaseReadinessVerifiedEvidenceSource source) =>
        _source = source ?? throw new ArgumentNullException(nameof(source));

    public sealed record Request(string? ReleaseId, string? ExpectedSha, string? ObservedSha);
    public sealed record Result(string Code, bool NeedsIndependentAuthenticatedReview)
    {
        public bool AllowsRelease => false;
        public bool AllowsProductionDeployment => false;
        public bool AllowsInstitutionalWrite => false;
        public bool CertifiesTenantIsolation => false;
    }

    public async Task<Result> PrepareAsync(Request? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || !Safe(request.ReleaseId))
            return Deny("RELEASE_REQUEST_REQUIRED");

        if (!FullSha(request.ExpectedSha) ||
            !FullSha(request.ObservedSha) ||
            !string.Equals(request.ExpectedSha, request.ObservedSha, StringComparison.Ordinal))
            return Deny("RELEASE_SOURCE_SHA_MISMATCH");

        cancellationToken.ThrowIfCancellationRequested();
        var authority = await _source.GetAuthorityAsync(cancellationToken);
        if (authority is null || !string.Equals(authority.ExpectedCommitSha,
            request.ExpectedSha, StringComparison.Ordinal))
            return Deny("VERIFIED_DEV_AUTHORITY_REQUIRED");

        var candidate = await _source.GetCandidateAsync(request.ReleaseId!, cancellationToken);
        if (candidate is null ||
            !string.Equals(candidate.ReleaseId, request.ReleaseId, StringComparison.Ordinal))
            return Deny("VERIFIED_RELEASE_CANDIDATE_REQUIRED");

        var verdict = Rules.Evaluate(authority, candidate);
        if (!verdict.ReadyForExternalEvidenceReview)
            return Deny(verdict.Code);

        // Domain positive preflight is not proof that sources, checks or A5 are authentic.
        return new Result("INDEPENDENT_AUTHENTICATED_RELEASE_REVIEW_REQUIRED", true);
    }

    private static Result Deny(string code) => new(code, false);
    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
    private static bool Safe(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.Length <= 128 &&
        text.All(c => c >= '!' && c <= '~') &&
        !text.Contains("://", StringComparison.Ordinal) &&
        !text.Contains('@');
}
