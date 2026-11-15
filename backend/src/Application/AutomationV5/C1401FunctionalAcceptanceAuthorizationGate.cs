using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// A future authenticated backend adapter must retrieve CURRENT session, membership,
/// relational RBAC and persisted ownership. No client-supplied actor is trusted.
/// </summary>
public interface IC1401FunctionalAcceptanceAuthorityReader
{
    ValueTask<C1401FunctionalAcceptanceAuthorizationGate.VerifiedContext?> ReadAsync(
        string resourceKey, CancellationToken cancellationToken);
}

/// <summary>
/// C14-01.M03.03: isolated fail-closed authorization and tenant boundary for
/// functional acceptance. This is read-only and DOES NOT authenticate HTTP,
/// grant privileges, certify E2E/QA, or bind to DI, SQL, payments or deployment.
/// </summary>
public sealed class C1401FunctionalAcceptanceAuthorizationGate
{
    private readonly IC1401FunctionalAcceptanceAuthorityReader _reader;
    private readonly V5FunctionalAcceptanceApplicationService _application = new();

    public C1401FunctionalAcceptanceAuthorizationGate(
        IC1401FunctionalAcceptanceAuthorityReader reader) =>
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public sealed record Request(
        string? ResourceKey, string? ExpectedSourceSha, int ClientTenantId,
        IReadOnlyCollection<V5FunctionalAcceptanceDomainRules.AcceptanceEvidence?>? Items,
        bool ClientClaimsMembership = false,
        bool ClientClaimsPermission = false,
        bool RequestsProductionOrBypass = false);

    public sealed record VerifiedContext(
        string? PersistedResourceKey, string? SourceCommitSha,
        int ActorId, int TenantId, bool CurrentBackendJwtVerified,
        bool CurrentMembershipVerified, bool CurrentRelationalRbacVerified,
        bool PersistedOwnershipVerified, bool SessionOrRoleRevoked,
        IReadOnlySet<string>? ApprovedCaseKeys,
        IReadOnlySet<string>? ApprovedActionKeys);

    public readonly record struct Decision(string Code, int ItemsChecked)
    {
        public bool RequiresFutureAuthenticatedAdapter =>
            Code == "AUTHENTICATED_FUNCTIONAL_ACCEPTANCE_ADAPTER_REQUIRED";
        public bool GrantsTenantOrResourceAccess => false;
        public bool AllowsInstitutionalWrite => false;
        public bool CertifiesFunctionalAcceptance => false;
        public bool AuthorizesQaOrProduction => false;
    }

    public async ValueTask<Decision> InspectAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request is null || !SafeResourceKey(request.ResourceKey) ||
            !ValidSha(request.ExpectedSourceSha) || request.ClientTenantId <= 0)
            return new("VALID_RESOURCE_TENANT_AND_SOURCE_REQUIRED", 0);

        if (request.ClientClaimsMembership || request.ClientClaimsPermission ||
            request.RequestsProductionOrBypass)
            return new("CLIENT_PRIVILEGE_OR_PRODUCTION_OVERRIDE_DENIED", 0);

        VerifiedContext? trusted;
        try
        {
            trusted = await _reader.ReadAsync(request.ResourceKey!, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("CURRENT_BACKEND_AUTHORITY_SOURCE_UNAVAILABLE", 0);
        }

        if (trusted is null ||
            !string.Equals(trusted.PersistedResourceKey, request.ResourceKey,
                StringComparison.Ordinal) ||
            !string.Equals(trusted.SourceCommitSha, request.ExpectedSourceSha,
                StringComparison.Ordinal))
            return new("PERSISTED_RESOURCE_AND_SOURCE_REQUIRED", 0);

        if (trusted.ActorId <= 0 || trusted.TenantId <= 0 ||
            !trusted.CurrentBackendJwtVerified || !trusted.CurrentMembershipVerified ||
            !trusted.CurrentRelationalRbacVerified ||
            !trusted.PersistedOwnershipVerified || trusted.SessionOrRoleRevoked)
            return new("CURRENT_BACKEND_JWT_RBAC_MEMBERSHIP_OWNERSHIP_REQUIRED", 0);

        if (request.ClientTenantId != trusted.TenantId)
            return new("CROSS_TENANT_CLIENT_ASSERTION_DENIED", 0);

        if (trusted.ApprovedCaseKeys is null || trusted.ApprovedCaseKeys.Count == 0 ||
            trusted.ApprovedActionKeys is null || trusted.ApprovedActionKeys.Count == 0)
            return new("TRUSTED_SERVER_CASE_ACTION_CATALOG_REQUIRED", 0);

        var actor = new V5FunctionalAcceptanceDomainRules.VerifiedActor(
            trusted.ActorId, trusted.TenantId, true, true, true, trusted.SourceCommitSha!);
        var requestForApplication = new V5FunctionalAcceptanceApplicationService.Request(
            request.ClientTenantId, request.Items);
        var result = _application.Prepare(
            actor, requestForApplication, trusted.ApprovedCaseKeys, trusted.ApprovedActionKeys);

        // Even a positive preflight requires separate live authenticated adapter,
        // E2E evidence, independent A5, release permissions and current DB checks.
        return result.DomainPreflightAccepted
            ? new("AUTHENTICATED_FUNCTIONAL_ACCEPTANCE_ADAPTER_REQUIRED",
                result.ItemsChecked)
            : new(result.Code, result.ItemsChecked);
    }

    private static bool ValidSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    private static bool SafeResourceKey(string? key) =>
        key is { Length: >= 1 and <= 96 } &&
        key.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-' or '_');
}
