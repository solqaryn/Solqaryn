using DomainRules = Solqaryn.Domain.AutomationV5.V5TenancySecurityDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Trusted server-side integration boundary. The implementation must resolve
/// current membership, explicit permission and persisted ownership independently
/// of browser headers/body/route, and must never disable EF tenant filters.
/// </summary>
public interface IV5TenancySecurityVerifiedAuthorizationEvidenceReader
{
    ValueTask<V5TenancySecurityAuthorizationGate.VerifiedEvidence?> ReadVerifiedAsync(
        int resourceId, CancellationToken cancellationToken);
}

/// <summary>
/// C15-03.M03.03: an additional fail-closed authorization/tenancy gate.
/// It verifies the identity and freshness of a trusted evidence snapshot
/// before Domain rules. A positive preflight is NOT an API access grant;
/// no HTTP, EF, file, job or control-plane operation occurs here.
/// </summary>
public sealed class V5TenancySecurityAuthorizationGate
{
    private readonly IV5TenancySecurityVerifiedAuthorizationEvidenceReader _reader;

    public V5TenancySecurityAuthorizationGate(
        IV5TenancySecurityVerifiedAuthorizationEvidenceReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public sealed record Request(
        int ResourceId,
        DomainRules.Surface Surface,
        string? ExpectedSourceSha,
        bool RequestsTenantFilterBypass,
        bool AttemptsCrossTenantEnumeration,
        bool RequestsInstitutionalWrite,
        string? IdempotencyKey);

    public sealed record VerifiedEvidence(
        string? ObservedSourceSha,
        int? VerifiedResourceId,
        int? VerifiedTenantId,
        int? VerifiedActorId,
        bool ServerAuthenticationVerified,
        bool CurrentMembershipVerified,
        bool ExplicitPermissionVerified,
        bool AuthorizationReadFromTrustedBackend,
        bool PermissionRevoked,
        DomainRules.TrustedAuthority? Authority,
        DomainRules.PersistedResource? Resource);

    public readonly record struct Result(string Code)
    {
        // This permits only the next separately authenticated adapter check.
        // Never treat this result as a resource/API/EF authorization.
        public bool NeedsAuthenticatedExecutionAdapter =>
            Code == "AUTHENTICATED_EXECUTION_ADAPTER_REQUIRED";
        public bool GrantsResourceRead => false;
        public bool GrantsFileDownload => false;
        public bool GrantsBackgroundJob => false;
        public bool GrantsControlPlane => false;
        public bool GrantsInstitutionalWrite => false;
        public bool AllowsTenantFilterBypass => false;
    }

    public async ValueTask<Result> EvaluateAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request is null)
            return Deny("AUTHORIZATION_REQUEST_REQUIRED");
        if (request.ResourceId <= 0 || !Enum.IsDefined(request.Surface))
            return Deny("RESOURCE_OR_SECURITY_SURFACE_INVALID");
        if (!FullSha(request.ExpectedSourceSha))
            return Deny("EXACT_SOURCE_SHA_REQUIRED");

        // No untrusted bypass, enumeration or institutional mutation may
        // invoke the evidence provider. A review-only gate never performs I/O.
        if (request.RequestsTenantFilterBypass ||
            request.AttemptsCrossTenantEnumeration)
            return Deny("TENANT_QUERY_BYPASS_DENIED");
        if (request.RequestsInstitutionalWrite)
            return Deny("INSTITUTIONAL_WRITE_OUT_OF_SCOPE");

        var evidence = await _reader.ReadVerifiedAsync(
            request.ResourceId, cancellationToken);

        if (evidence is null ||
            !string.Equals(evidence.ObservedSourceSha,
                request.ExpectedSourceSha, StringComparison.Ordinal))
            return Deny("TRUSTED_EVIDENCE_SOURCE_MISSING_OR_STALE");

        if (!evidence.ServerAuthenticationVerified ||
            !evidence.CurrentMembershipVerified ||
            !evidence.AuthorizationReadFromTrustedBackend ||
            !evidence.ExplicitPermissionVerified ||
            evidence.PermissionRevoked)
            return Deny("CURRENT_VERIFIED_AUTHORIZATION_REQUIRED");

        if (evidence.VerifiedResourceId != request.ResourceId ||
            evidence.VerifiedTenantId is not > 0 ||
            evidence.VerifiedActorId is not > 0 ||
            evidence.Authority?.AuthenticatedTenantId != evidence.VerifiedTenantId ||
            evidence.Authority?.AuthenticatedActorId != evidence.VerifiedActorId ||
            evidence.Resource?.OwnerTenantId != evidence.VerifiedTenantId ||
            evidence.Resource?.ParentTenantId != evidence.VerifiedTenantId ||
            evidence.Resource.OwnershipReadFromPersistence != true)
            return Deny("RESOURCE_SCOPE_OR_IDENTITY_MISMATCH");

        var domainRequest = new DomainRules.Request(
            request.Surface, request.RequestsTenantFilterBypass,
            request.IdempotencyKey, request.AttemptsCrossTenantEnumeration);
        var verdict = DomainRules.Evaluate(
            evidence.Authority, evidence.Resource, domainRequest);
        if (!verdict.RequiresServerEnforcement)
            return Deny(verdict.Code);

        // Separate authenticated API/EF adapters must still verify and
        // execute the requested operation. This gate grants no capability.
        return new Result("AUTHENTICATED_EXECUTION_ADAPTER_REQUIRED");
    }

    private static Result Deny(string code) => new(code);

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
}
