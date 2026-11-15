using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C07-04.M03.02: read-only commercial-document application preflight.
/// Caller-provided facts are NOT an authenticated backend session or a grant.
/// A future authenticated adapter must fetch ownership and grants from trusted
/// services, enforce version/idempotency atomically and handle fiscal/PSP gates.
/// No method here can persist, charge, invoice, export or dispatch an event.
/// </summary>
public sealed class V5CommercialDocumentApplicationService
{
    public sealed record Request(
        string? ExpectedSourceSha,
        string? ObservedSourceSha,
        string? SanitizedEvidenceReference,
        V5CommercialDocumentDomainRules.TrustedContext? Authority,
        V5CommercialDocumentDomainRules.PersistedSnapshot? Document,
        V5CommercialDocumentDomainRules.Command? Command);

    public sealed record Result(string Code, bool NeedsAuthenticatedAdapter)
    {
        public bool GrantsCommercialMutation => false;
        public bool GrantsPaymentOrFiscalIssuance => false;
        public bool GrantsDocumentExport => false;
        public bool GrantsCrossTenantRead => false;
    }

    public Result Prepare(Request? request)
    {
        if (request is null)
            return Deny("COMMERCIAL_REQUEST_REQUIRED");

        if (!FullSha(request.ExpectedSourceSha) ||
            !FullSha(request.ObservedSourceSha) ||
            !string.Equals(request.ExpectedSourceSha, request.ObservedSourceSha,
                StringComparison.Ordinal))
            return Deny("COMMERCIAL_SOURCE_SHA_UNVERIFIED");

        if (!SafeReference(request.SanitizedEvidenceReference))
            return Deny("COMMERCIAL_EVIDENCE_REFERENCE_REQUIRED");

        var verdict = V5CommercialDocumentDomainRules.Evaluate(
            request.Authority, request.Document, request.Command);

        if (!verdict.NeedsApplicationVerification)
            return Deny(verdict.Code);

        // This class does not verify JWT signatures, membership, grants,
        // fiscal issuer, provider tokens or a durable idempotency ledger.
        // A positive domain preflight MUST NOT release a commercial operation.
        return new Result("AUTHENTICATED_COMMERCIAL_ADAPTER_REQUIRED", true);
    }

    private static Result Deny(string code) => new(code, false);

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    private static bool SafeReference(string? reference) =>
        !string.IsNullOrWhiteSpace(reference) &&
        reference.Length <= 160 &&
        reference.All(c => c >= '!' && c <= '~') &&
        !reference.Contains("://", StringComparison.Ordinal) &&
        !reference.Contains('@');
}
