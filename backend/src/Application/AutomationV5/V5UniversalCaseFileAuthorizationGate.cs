using Rules = Solqaryn.Domain.AutomationV5.V5UniversalCaseFileDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C01-04.M03.03. Trusted-evidence, fail-closed case-file authorization preflight.
/// No caller boolean is accepted as proof of authentication, tenancy, membership,
/// persisted ownership, review, attachment entitlement, or JWT/RBAC authorization.
/// This class grants no HTTP, file or persistence permissions.
/// </summary>
public interface IV5UniversalCaseFileVerifiedEvidenceReader
{
    ValueTask<V5UniversalCaseFileAuthorizationGate.VerifiedEvidence?> ReadAsync(
        int caseFileId, CancellationToken cancellationToken);

    // A legacy reader cannot attest a grant for a particular operation.
    ValueTask<V5UniversalCaseFileAuthorizationGate.VerifiedEvidence?> ReadForActionAsync(
        int caseFileId, Rules.CaseAction action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<V5UniversalCaseFileAuthorizationGate.VerifiedEvidence?>(null);
    }
}

public sealed class V5UniversalCaseFileAuthorizationGate
{
    private readonly IV5UniversalCaseFileVerifiedEvidenceReader _reader;

    public V5UniversalCaseFileAuthorizationGate(
        IV5UniversalCaseFileVerifiedEvidenceReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public sealed record Request(
        int CaseFileId,
        Rules.CaseAction Action,
        string? ExpectedSourceSha,
        int? ClientSelectedTenantId,
        bool AttemptsTenantFilterBypass,
        bool AttemptsInstitutionalWrite,
        long? ExpectedVersion,
        string? IdempotencyKey,
        string? ActorReason);

    /// <summary>
    /// Must be created by a trusted backend adapter, not from route/body claims.
    /// Even the flags here are insufficient to grant an operation.
    /// </summary>
    public sealed record VerifiedEvidence(
        string? ObservedSourceSha,
        int? VerifiedCaseFileId,
        int? VerifiedActorId,
        int? VerifiedTenantId,
        bool ServerAuthenticationVerified,
        bool MembershipReadFromTrustedBackend,
        bool PermissionReadFromTrustedBackend,
        bool PermissionRevoked,
        bool CaseOwnershipReadFromPersistence,
        Rules.ServerAuthority? Authority,
        Rules.StoredCase? Stored,
        bool IndependentDecisionReviewVerified,
        bool AttachmentOwnershipVerified,
        bool AttachmentLinkExpired)
    {
        public Rules.CaseAction? VerifiedAction { get; init; }
    }

    public readonly record struct Result(string Code)
    {
        public bool RequiresSeparateAuthenticatedAdapter =>
            Code == "TRUSTED_CASEFILE_EXECUTION_REQUIRED";
        public bool GrantsTimelineRead => false;
        public bool GrantsCaseWrite => false;
        public bool GrantsAttachment => false;
        public bool AllowsTenantFilterBypass => false;
        public bool GrantsInstitutionalWrite => false;
    }

    public async ValueTask<Result> EvaluateAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request is null)
            return Deny("CASEFILE_AUTHORIZATION_REQUEST_REQUIRED");
        if (request.CaseFileId <= 0 || !Enum.IsDefined(request.Action))
            return Deny("CASEFILE_ID_OR_ACTION_INVALID");
        if (!FullSha(request.ExpectedSourceSha))
            return Deny("CASEFILE_EXACT_SOURCE_SHA_REQUIRED");

        // Unsafe requests must be rejected BEFORE invoking a provider.
        if (request.AttemptsTenantFilterBypass)
            return Deny("CASEFILE_TENANT_FILTER_BYPASS_DENIED");
        if (request.AttemptsInstitutionalWrite)
            return Deny("CASEFILE_INSTITUTIONAL_WRITE_DENIED");
        if (request.ClientSelectedTenantId is <= 0)
            return Deny("CASEFILE_CLIENT_TENANT_INVALID");

        var evidence = await _reader.ReadForActionAsync(request.CaseFileId, request.Action, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (evidence is null ||
            !string.Equals(evidence.ObservedSourceSha,
                request.ExpectedSourceSha, StringComparison.Ordinal))
            return Deny("CASEFILE_TRUSTED_EVIDENCE_MISSING_OR_STALE");

        if (evidence.VerifiedAction != request.Action)
            return Deny("CASEFILE_ACTION_GRANT_MISMATCH");

        if (!evidence.ServerAuthenticationVerified ||
            !evidence.MembershipReadFromTrustedBackend ||
            !evidence.PermissionReadFromTrustedBackend ||
            evidence.PermissionRevoked)
            return Deny("CASEFILE_CURRENT_AUTHORIZATION_REQUIRED");

        if (evidence.VerifiedCaseFileId != request.CaseFileId ||
            evidence.VerifiedActorId is not > 0 ||
            evidence.VerifiedTenantId is not > 0 ||
            !evidence.CaseOwnershipReadFromPersistence ||
            evidence.Authority?.ActorId != evidence.VerifiedActorId ||
            evidence.Authority?.TenantId != evidence.VerifiedTenantId ||
            evidence.Stored?.OwnerTenantId != evidence.VerifiedTenantId ||
            evidence.Stored?.OwnershipVerifiedFromPersistence != true)
            return Deny("CASEFILE_PERSISTED_OWNER_OR_IDENTITY_MISMATCH");

        if (request.ClientSelectedTenantId is not null &&
            request.ClientSelectedTenantId != evidence.VerifiedTenantId)
            return Deny("CASEFILE_CLIENT_TENANT_OVERRIDE_DENIED");

        // API12 is branch-bound even when a different trusted reader is used.
        // Nullable branch is valid for legacy timeline preflight, not for an
        // authenticated enterprise audit correlation.
        if (request.Action == Rules.CaseAction.ReadAuditCorrelation &&
            (evidence.Authority?.BranchId is not > 0 ||
             evidence.Stored?.OwnerBranchId is not > 0 ||
             evidence.Authority.BranchId != evidence.Stored.OwnerBranchId))
            return Deny("CASEFILE_BRANCH_MISMATCH");

        // The dangerous flags are derived solely from trusted evidence.
        var command = new Rules.Command(
            request.Action,
            request.ClientSelectedTenantId,
            request.ExpectedVersion,
            request.IdempotencyKey,
            request.ActorReason,
            evidence.AttachmentOwnershipVerified,
            evidence.AttachmentLinkExpired,
            evidence.IndependentDecisionReviewVerified);

        var domainVerdict = Rules.Evaluate(evidence.Authority, evidence.Stored, command);
        if (!domainVerdict.RequiresTrustedApplicationEnforcement)
            return Deny(domainVerdict.Code);

        // A positive preflight is never a JWT/HTTP/EF/MySQL authorization.
        // The future runtime adapter must revalidate ownership, current grants,
        // idempotency, audit and row-version inside its own transaction.
        return new Result("TRUSTED_CASEFILE_EXECUTION_REQUIRED");
    }

    private static Result Deny(string code) => new(code);

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
}
