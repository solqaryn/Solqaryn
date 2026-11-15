using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// DEV-only, read-only application preflight for the universal case-file use case.
/// It composes the existing Domain rules but does not resolve JWT, permissions,
/// persistence ownership, attachments or A5 from untrusted caller assertions.
/// A future authenticated adapter must obtain those facts from trusted services.
/// </summary>
public sealed class V5UniversalCaseFileApplicationService
{
    public sealed record Request(
        string? ExpectedSourceSha,
        string? ObservedSourceSha,
        string? EvidenceReference,
        V5UniversalCaseFileDomainRules.ServerAuthority? Authority,
        V5UniversalCaseFileDomainRules.StoredCase? StoredCase,
        V5UniversalCaseFileDomainRules.Command? Command);

    public sealed record Result(string Code, bool NeedsTrustedAdapter)
    {
        public bool AllowsPersistence => false;
        public bool AllowsExternalMediaAccess => false;
        public bool IsFinalAuthorization => false;
    }

    public Result Prepare(Request? request)
    {
        if (request is null)
            return Deny("CASEFILE_REQUEST_REQUIRED");

        if (!FullSha(request.ExpectedSourceSha) ||
            !FullSha(request.ObservedSourceSha) ||
            !string.Equals(request.ExpectedSourceSha, request.ObservedSourceSha,
                StringComparison.Ordinal))
            return Deny("CASEFILE_SOURCE_SHA_UNVERIFIED");

        if (!SafeEvidenceReference(request.EvidenceReference))
            return Deny("CASEFILE_EVIDENCE_REFERENCE_REQUIRED");

        var verdict = V5UniversalCaseFileDomainRules.Evaluate(
            request.Authority, request.StoredCase, request.Command);

        if (!verdict.RequiresTrustedApplicationEnforcement)
            return Deny(verdict.Code);

        // These are caller-supplied preflight facts, not authenticated evidence.
        // No code path here grants writes or direct media access.
        return new Result("AUTHENTICATED_CASEFILE_ADAPTER_REQUIRED", true);
    }

    private static Result Deny(string code) => new(code, false);

    private static bool FullSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);

    private static bool SafeEvidenceReference(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 160 &&
        value.All(c => c >= '!' && c <= '~') &&
        !value.Contains("://", StringComparison.Ordinal) &&
        !value.Contains('@');
}
