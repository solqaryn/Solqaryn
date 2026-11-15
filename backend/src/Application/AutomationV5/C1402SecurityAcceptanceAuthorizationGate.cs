using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C14-02.M03.03: server-evidence-only authorization preflight for security acceptance.
/// Does not authenticate HTTP requests, grant access or certify a pentest/production.
/// </summary>
public interface IC1402SecurityAcceptanceEvidenceReader
{
    ValueTask<C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence?> ReadAsync(
        string resourceKey, CancellationToken cancellationToken);
}

public sealed class C1402SecurityAcceptanceAuthorizationGate
{
    private readonly IC1402SecurityAcceptanceEvidenceReader _reader;
    private readonly V5SecurityAcceptanceApplicationService _application = new();

    public C1402SecurityAcceptanceAuthorizationGate(IC1402SecurityAcceptanceEvidenceReader reader) =>
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public sealed record Request(
        string? ResourceKey, string? ExpectedCommitSha,
        V5SecurityAcceptanceDomainRules.Surface Surface,
        bool ClientControlsTenantOrRole = false,
        bool RequestsBypassOrRestore = false);

    public sealed record VerifiedEvidence(
        string? PersistedResourceKey,
        string? ObservedSourceSha,
        bool ServerBackendEvidenceVerified,
        bool CurrentJwtVerified,
        bool CurrentMembershipVerified,
        bool CurrentExplicitRbacVerified,
        bool CurrentAuditFenceVerified,
        string? SanitizedProofReference,
        V5SecurityAcceptanceDomainRules.VerifiedAuthority? Authority,
        V5SecurityAcceptanceDomainRules.PersistedTarget? Target,
        V5SecurityAcceptanceDomainRules.Attempt? Attempt);

    public readonly record struct Decision(string Code)
    {
        public bool RequiresSeparateAuthenticatedAdapter =>
            Code == "AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED";
        public bool GrantsTenantOrResourceAccess => false;
        public bool AllowsInstitutionalWrite => false;
        public bool AllowsRestoreOrDeployment => false;
        public bool CertifiesSecurityAcceptance => false;
    }

    public async ValueTask<Decision> CheckAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request is null || !SafeResourceKey(request.ResourceKey) ||
            !ValidSha(request.ExpectedCommitSha) ||
            !Enum.IsDefined(request.Surface))
            return new("INVALID_REQUEST_OR_SHA");

        if (request.ClientControlsTenantOrRole || request.RequestsBypassOrRestore ||
            request.Surface == V5SecurityAcceptanceDomainRules.Surface.BackupRestore)
            return new("CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED");

        VerifiedEvidence? evidence;
        try
        {
            evidence = await _reader.ReadAsync(request.ResourceKey!, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("TRUSTED_EVIDENCE_SOURCE_UNAVAILABLE");
        }

        if (evidence is null || !evidence.ServerBackendEvidenceVerified ||
            !string.Equals(evidence.PersistedResourceKey, request.ResourceKey,
                StringComparison.Ordinal) ||
            !string.Equals(evidence.ObservedSourceSha, request.ExpectedCommitSha,
                StringComparison.Ordinal))
            return new("PERSISTED_RESOURCE_AND_SOURCE_REQUIRED");

        if (!evidence.CurrentJwtVerified || !evidence.CurrentMembershipVerified ||
            !evidence.CurrentExplicitRbacVerified || !evidence.CurrentAuditFenceVerified ||
            evidence.Authority is null || evidence.Target is null || evidence.Attempt is null ||
            !evidence.Authority.JwtVerified ||
            !evidence.Authority.ActiveMembershipVerified ||
            !evidence.Authority.RequiredPermissionVerified ||
            evidence.Attempt.Surface != request.Surface ||
            evidence.Target.OwnerTenantId != evidence.Authority.ServerTenantId ||
            evidence.Target.AccessRevoked)
            return new("CURRENT_AUTHORIZATION_TENANT_AND_AUDIT_REQUIRED");

        var application = _application.Prepare(
            new V5SecurityAcceptanceApplicationService.Request(
                request.ExpectedCommitSha, evidence.ObservedSourceSha,
                evidence.SanitizedProofReference,
                evidence.Authority, evidence.Target, evidence.Attempt));

        if (!application.NeedsTrustedAdapter)
            return new(application.Code);

        // This is only a preflight. The real authenticated adapter must re-read
        // membership, persisted ownership, RBAC and audit policy before access.
        return new("AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED");
    }

    private static bool ValidSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);

    private static bool SafeResourceKey(string? value) =>
        value is { Length: >= 1 and <= 96 } &&
        value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_');
}
