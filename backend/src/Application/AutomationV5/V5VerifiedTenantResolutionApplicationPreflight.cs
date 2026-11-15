using Guard = Solqaryn.Domain.AutomationV5.V5VerifiedTenantResolutionDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C02-01.M03.02: non-authorizing Application tenant-resolution preflight.
/// The trusted backend must obtain all ServerEvidence and resource ownership
/// from authenticated persistence/registry, never client DTO, forwarded headers
/// or an old cached role. No HTTP tenant context is installed here.
/// </summary>
public sealed class V5VerifiedTenantResolutionApplicationPreflight
{
    public sealed record RequestIntent(
        Guard.Surface Surface,
        string? RequestedTenantId,
        bool ClientClaimsTenantOrHostAuthority,
        bool ClientClaimsLegacyRoleAuthority,
        bool RequestsDefaultFallback,
        bool AttemptsWriteExportOrGrant);

    public sealed record VerifiedResourceScope(
        string? OwnerTenantId,
        bool OwnershipReadFromPersistence,
        bool IncludesBranch,
        string? BranchOwnerTenantId,
        bool BranchOwnershipReadFromPersistence);

    public sealed record ReviewResult(string Code, bool RequiresLiveBackendEnforcement)
    {
        public bool AuthorizesTenantAccess => false;
        public bool ReadsOrWritesResource => false;
        public bool InstallsTenantContext => false;
        public bool IssuesJwtOrGrants => false;
    }

    public ReviewResult ReviewOnly(
        Guard.ServerEvidence? trustedBackendEvidence,
        VerifiedResourceScope? verifiedResource,
        RequestIntent? request)
    {
        if (request is null)
            return new("TENANT_REQUEST_INTENT_REQUIRED", false);

        if (trustedBackendEvidence is null || verifiedResource is null)
            return new("TRUSTED_BACKEND_SNAPSHOT_REQUIRED", false);

        if (request.AttemptsWriteExportOrGrant)
            return new("TENANT_PREFLIGHT_CANNOT_MUTATE_EXPORT_OR_GRANT", false);

        if (request.ClientClaimsTenantOrHostAuthority ||
            request.ClientClaimsLegacyRoleAuthority ||
            request.RequestsDefaultFallback)
            return new("CLIENT_TENANT_AUTHORITY_OVERRIDE_DENIED", false);

        // A requested tenant is only a consistency check, not a selector
        // or a fallback that can establish another enterprise membership.
        if (string.IsNullOrWhiteSpace(request.RequestedTenantId) ||
            !string.Equals(request.RequestedTenantId,
                trustedBackendEvidence.VerifiedTenantId, StringComparison.Ordinal))
            return new("CLIENT_TENANT_CONSISTENCY_MISMATCH", false);

        // Server-side resource ownership is mandatory for this scoped intent.
        if (!verifiedResource.OwnershipReadFromPersistence ||
            string.IsNullOrWhiteSpace(verifiedResource.OwnerTenantId))
            return new("PERSISTED_RESOURCE_OWNER_REQUIRED", false);

        Guard.ProposedResolution proposal = new()
        {
            Surface = request.Surface,
            ClientRequestedTenantId = request.RequestedTenantId,
            // No client authority or fallback is carried into the Domain.
            TreatsClientTenantAsAuthority = false,
            TreatsClientHostAsAuthority = false,
            TreatsLegacyGlobalRoleAsAuthority = false,
            AttemptsTenantDefaultFallback = false,
            ResourceOwnerTenantId = verifiedResource.OwnerTenantId,
            ResourceOwnershipReadFromPersistence = verifiedResource.OwnershipReadFromPersistence,
            IncludesBranch = verifiedResource.IncludesBranch,
            BranchOwnerTenantId = verifiedResource.BranchOwnerTenantId,
            BranchOwnershipReadFromPersistence = verifiedResource.BranchOwnershipReadFromPersistence
        };

        Guard.Verdict verified = Guard.Evaluate(trustedBackendEvidence, proposal);
        return new(verified.Code, verified.RequiresRuntimeEnforcement);
    }
}
