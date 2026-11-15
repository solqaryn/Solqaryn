using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C14-02.M03.02: Application-level, non-authorizing preflight for security
/// acceptance evidence. Caller-supplied authority is not an authenticated
/// resource-access decision. Trusted adapters must verify JWT, tenant, RBAC,
/// host/file ownership and CI independently. No release or access is granted.
/// </summary>
public sealed class V5SecurityAcceptanceApplicationService
{
    public sealed record Request(
        string? ExpectedCommitSha,
        string? ObservedCommitSha,
        string? SanitizedProofReference,
        V5SecurityAcceptanceDomainRules.VerifiedAuthority? Authority,
        V5SecurityAcceptanceDomainRules.PersistedTarget? Target,
        V5SecurityAcceptanceDomainRules.Attempt? Attempt);

    public sealed record Result(string Code, bool NeedsTrustedAdapter)
    {
        public bool AllowsResourceAccess => false;
        public bool AllowsRelease => false;
        public bool AllowsInstitutionalWrite => false;
    }

    public Result Prepare(Request? request)
    {
        if (request is null)
            return Deny("SECURITY_ACCEPTANCE_REQUEST_REQUIRED");

        if (!FullSha(request.ExpectedCommitSha) ||
            !FullSha(request.ObservedCommitSha) ||
            !string.Equals(request.ExpectedCommitSha, request.ObservedCommitSha,
                StringComparison.Ordinal))
            return Deny("SECURITY_ACCEPTANCE_SOURCE_SHA_MISMATCH");

        if (!SafeProof(request.SanitizedProofReference))
            return Deny("SECURITY_ACCEPTANCE_PROOF_REFERENCE_REQUIRED");

        if (request.Authority is null ||
            !string.Equals(request.Authority.EvidenceSha,
                request.ExpectedCommitSha, StringComparison.Ordinal) ||
            request.Attempt is null ||
            !string.Equals(request.Attempt.EvidenceSha,
                request.ExpectedCommitSha, StringComparison.Ordinal))
            return Deny("SECURITY_ACCEPTANCE_EVIDENCE_SHA_MISMATCH");

        var verdict = V5SecurityAcceptanceDomainRules.Evaluate(
            request.Authority, request.Target, request.Attempt);

        if (!verdict.RequiresTrustedIntegration)
            return Deny(verdict.Code);

        // Pure preflight cannot authenticate caller claims nor produce A0-A5,
        // tenant authorization, CI certification, media access or deployment.
        return new Result("TRUSTED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED", true);
    }

    private static Result Deny(string code) => new(code, false);

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    private static bool SafeProof(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 160 &&
        value.All(c => c >= '!' && c <= '~') &&
        !value.Contains("://", StringComparison.Ordinal) &&
        !value.Contains('@');
}
