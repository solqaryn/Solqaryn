using Rules = Solqaryn.Domain.AutomationV5.V5TenancySecurityDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Authenticated server-only authority source. Implementations MUST derive tenant,
/// actor, branch, membership, permission and service grant from verified backend
/// state, never from untrusted URL/header/body/cookie parameters.
/// </summary>
public interface IV5TenancySecurityAuthoritySource
{
    ValueTask<Rules.TrustedAuthority?> ReadVerifiedAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Read-only scoped persistence boundary. Implementations MUST retain tenant
/// filters and verify ownership in persistence; they MUST NOT use an unscoped
/// fallback when no matching resource is found.
/// </summary>
public interface IV5TenancySecurityResourceSource
{
    ValueTask<Rules.PersistedResource?> ReadScopedAsync(
        int resourceId, int verifiedTenantId, CancellationToken cancellationToken);
}

/// <summary>
/// C15-03.M03.02: fail-closed Application preflight composing Domain M03.01.
/// This service neither authenticates HTTP requests nor grants resource access,
/// file links, jobs, administration, database writes or tenant-filter bypasses.
/// Runtime integration requires separately verified trusted adapters.
/// </summary>
public sealed class V5TenancySecurityApplicationService
{
    private readonly IV5TenancySecurityAuthoritySource _authority;
    private readonly IV5TenancySecurityResourceSource _resources;

    public V5TenancySecurityApplicationService(
        IV5TenancySecurityAuthoritySource authority,
        IV5TenancySecurityResourceSource resources)
    {
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    }

    public sealed record Request(
        int ResourceId,
        Rules.Surface Surface,
        string? ExpectedSourceSha,
        string? ObservedSourceSha,
        string? SanitizedProofReference,
        bool RequestsTenantFilterBypass,
        bool AttemptsCrossTenantEnumeration,
        string? IdempotencyKey);

    public sealed record Result(string Code, bool NeedsAuthenticatedAdapter)
    {
        public bool GrantsResourceAccess => false;
        public bool AllowsFileDownload => false;
        public bool AllowsBackgroundJob => false;
        public bool AllowsAdministrativeAction => false;
        public bool AllowsTenantFilterBypass => false;
    }

    public async Task<Result> PrepareAsync(Request? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return Deny("SECURITY_TENANCY_APPLICATION_REQUEST_REQUIRED");

        if (request.ResourceId <= 0 || !Enum.IsDefined(request.Surface))
            return Deny("SECURITY_TENANCY_RESOURCE_OR_SURFACE_INVALID");

        if (!FullSha(request.ExpectedSourceSha) ||
            !FullSha(request.ObservedSourceSha) ||
            !string.Equals(request.ExpectedSourceSha, request.ObservedSourceSha,
                StringComparison.Ordinal))
            return Deny("SECURITY_TENANCY_SOURCE_SHA_MISMATCH");

        if (!SafeProof(request.SanitizedProofReference))
            return Deny("SECURITY_TENANCY_PROOF_REFERENCE_REQUIRED");

        // Never reach persistence with an explicit bypass or enumeration intent.
        if (request.RequestsTenantFilterBypass ||
            request.AttemptsCrossTenantEnumeration)
            return Deny("TENANT_QUERY_BYPASS_DENIED");

        cancellationToken.ThrowIfCancellationRequested();
        var verified = await _authority.ReadVerifiedAsync(cancellationToken);
        if (verified?.AuthenticatedTenantId is not > 0 ||
            verified.AuthenticatedActorId is not > 0 ||
            !verified.ActiveMembershipVerified ||
            !verified.RequiredRoleVerified)
            return Deny("VERIFIED_SERVER_AUTHORITY_REQUIRED");

        // Tenant scope comes only from the trusted authority source.
        // A null, foreign or unverified persistence response fails closed in Domain.
        var stored = await _resources.ReadScopedAsync(
            request.ResourceId, verified.AuthenticatedTenantId.Value,
            cancellationToken);

        var attempt = new Rules.Request(
            request.Surface,
            request.RequestsTenantFilterBypass,
            request.IdempotencyKey,
            request.AttemptsCrossTenantEnumeration);

        var verdict = Rules.Evaluate(verified, stored, attempt);
        if (!verdict.RequiresServerEnforcement)
            return Deny(verdict.Code);

        // A domain preflight is never final authorization or A0-A5 certification.
        return new Result("AUTHENTICATED_SECURITY_TENANCY_ADAPTER_REQUIRED", true);
    }

    private static Result Deny(string code) => new(code, false);

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    private static bool SafeProof(string? proof) =>
        !string.IsNullOrWhiteSpace(proof) &&
        proof.Length <= 160 &&
        proof.All(c => c >= '!' && c <= '~') &&
        !proof.Contains("://", StringComparison.Ordinal) &&
        !proof.Contains('@');
}
