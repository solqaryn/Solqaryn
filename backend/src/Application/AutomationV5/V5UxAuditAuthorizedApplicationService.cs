using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C15-04.M03.03. Fail-closed server-authorized read-only UX audit preflight.
/// This adapter does not authenticate JWTs itself and must never be fed raw
/// browser assertions as trusted authority. No access or publication is granted.
/// </summary>
public sealed class V5UxAuditAuthorizedApplicationService
{
    public sealed record TrustedAuthority(
        int? ServerActorId,
        string? ServerTenantScope,
        string? VerifiedSourceSha,
        bool JwtValidatedByServer,
        bool CurrentMembershipVerified,
        bool RequiredReviewPermissionVerified,
        bool TenantResolvedFromServer,
        bool ExplicitDevScopeVerified,
        IReadOnlySet<string>? ApprovedSurfaces,
        IReadOnlySet<string>? ApprovedCriteria);

    public readonly record struct Result(string Code, int ItemsReviewed)
    {
        public bool GrantsAccess => false;
        public bool AllowsInstitutionalWrite => false;
        public bool CertifiesExternalMeasurement => false;
        public bool AuthorizesProduction => false;
    }

    public Result Prepare(
        TrustedAuthority? authority,
        V5UxAuditApplicationService.Request? request)
    {
        if (authority is null || request is null ||
            authority.ServerActorId is not > 0 ||
            string.IsNullOrWhiteSpace(authority.ServerTenantScope) ||
            !authority.JwtValidatedByServer ||
            !authority.CurrentMembershipVerified ||
            !authority.RequiredReviewPermissionVerified ||
            !authority.TenantResolvedFromServer ||
            !authority.ExplicitDevScopeVerified)
            return new Result("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED", 0);

        if (!ValidSha(authority.VerifiedSourceSha) ||
            !string.Equals(authority.VerifiedSourceSha,
                request.ExpectedSourceSha, StringComparison.Ordinal) ||
            !string.Equals(authority.VerifiedSourceSha,
                request.ObservedSourceSha, StringComparison.Ordinal))
            return new Result("EXACT_TRUSTED_SOURCE_SHA_REQUIRED", 0);

        if (!string.Equals(authority.ServerTenantScope,
                request.ServerResolvedTenantScope, StringComparison.Ordinal) ||
            !request.TenantContextVerifiedByServer)
            return new Result("TRUSTED_TENANT_CONTEXT_MISMATCH", 0);

        if (authority.ApprovedSurfaces is null ||
            authority.ApprovedCriteria is null ||
            request.AuthorizedSurfaces is null ||
            request.AuthorizedCriteria is null ||
            authority.ApprovedSurfaces.Count == 0 ||
            authority.ApprovedCriteria.Count == 0 ||
            !request.AuthorizedSurfaces.IsSubsetOf(authority.ApprovedSurfaces) ||
            !request.AuthorizedCriteria.IsSubsetOf(authority.ApprovedCriteria))
            return new Result("SERVER_APPROVED_CATALOG_REQUIRED", 0);

        // Never substitute client-selected permissions or a tenant for the
        // verified server snapshot, even in a read-only candidate review.
        var trusted = request with
        {
            ExpectedSourceSha = authority.VerifiedSourceSha,
            ObservedSourceSha = authority.VerifiedSourceSha,
            ServerResolvedTenantScope = authority.ServerTenantScope,
            TenantContextVerifiedByServer = true,
            AuthorizedSurfaces = authority.ApprovedSurfaces,
            AuthorizedCriteria = authority.ApprovedCriteria
        };

        var result = new V5UxAuditApplicationService().Prepare(trusted);
        return new Result(result.Code, result.ItemsReviewed);
    }

    private static bool ValidSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
}
