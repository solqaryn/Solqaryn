using System;
using System.Linq;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C02-01.M03.01: deterministic, non-authorizing tenant resolution preflight.
/// Every evidence field must originate from trusted backend services, never from
/// a header, route, cookie, client DTO, cached role or unverified JWT claim.
/// API/Application still enforce authenticated membership and resource access.
/// </summary>
public static class V5VerifiedTenantResolutionDomainRules
{
    public enum Surface
    {
        SessionSelection = 1,
        VerifiedHostMapping = 2
    }

    public sealed record ServerEvidence
    {
        public string? AuthenticatedActorId { get; init; }
        public string? VerifiedTenantId { get; init; }
        public bool ServerJwtValidated { get; init; }
        public bool CurrentUserActive { get; init; }
        public bool CurrentTenantActive { get; init; }
        public bool MembershipReadFromPersistence { get; init; }
        public bool CurrentMembershipActive { get; init; }
        public bool CurrentRoleGrantVerified { get; init; }
        public bool MembershipOrGrantRevoked { get; init; }
        public bool AuthorityStoreAvailable { get; init; }
        public string? RegisteredHostTenantId { get; init; }
        public bool HostMappingFromTrustedRegistry { get; init; }
        public bool HostMappingUnique { get; init; }
        public bool HostMappingFresh { get; init; }
        public bool ForwardedHostUsed { get; init; }
        public bool ForwardedProxyTrusted { get; init; }
    }

    public sealed record ProposedResolution
    {
        public Surface Surface { get; init; }
        public string? ClientRequestedTenantId { get; init; }
        public bool TreatsClientTenantAsAuthority { get; init; }
        public bool TreatsClientHostAsAuthority { get; init; }
        public bool TreatsLegacyGlobalRoleAsAuthority { get; init; }
        public bool AttemptsTenantDefaultFallback { get; init; }
        public string? ResourceOwnerTenantId { get; init; }
        public bool ResourceOwnershipReadFromPersistence { get; init; }
        public bool IncludesBranch { get; init; }
        public string? BranchOwnerTenantId { get; init; }
        public bool BranchOwnershipReadFromPersistence { get; init; }
    }

    public readonly record struct Verdict(string Code)
    {
        // A validated precondition never grants access or installs an HTTP tenant context.
        public bool AuthorizesTenantAccess => false;
        public bool RequiresRuntimeEnforcement =>
            Code == "TRUSTED_BACKEND_ENFORCEMENT_REQUIRED";
    }

    private static bool ValidId(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id.Length <= 128 &&
        !id.Any(char.IsWhiteSpace) && !id.Any(char.IsControl);

    public static Verdict Evaluate(ServerEvidence? evidence, ProposedResolution? request)
    {
        if (evidence is null || request is null ||
            !ValidId(evidence.AuthenticatedActorId) ||
            !ValidId(evidence.VerifiedTenantId) ||
            !evidence.ServerJwtValidated || !evidence.CurrentUserActive)
            return new("AUTHENTICATED_SERVER_IDENTITY_REQUIRED");

        if (!Enum.IsDefined(request.Surface))
            return new("UNKNOWN_TENANT_RESOLUTION_SURFACE");

        if (!evidence.AuthorityStoreAvailable ||
            !evidence.CurrentTenantActive ||
            !evidence.MembershipReadFromPersistence ||
            !evidence.CurrentMembershipActive ||
            !evidence.CurrentRoleGrantVerified ||
            evidence.MembershipOrGrantRevoked)
            return new("FRESH_TENANT_MEMBERSHIP_REQUIRED");

        if (request.TreatsClientTenantAsAuthority ||
            request.TreatsClientHostAsAuthority ||
            request.TreatsLegacyGlobalRoleAsAuthority ||
            request.AttemptsTenantDefaultFallback)
            return new("CLIENT_OR_LEGACY_AUTHORITY_DENIED");

        // A route/DTO tenant ID is a requested target, not a grant. The backend
        // must already have independently selected the same verified tenant.
        if (request.ClientRequestedTenantId is not null &&
            (!ValidId(request.ClientRequestedTenantId) ||
             !string.Equals(request.ClientRequestedTenantId,
                 evidence.VerifiedTenantId, StringComparison.Ordinal)))
            return new("VERIFIED_TENANT_MISMATCH");

        if (request.Surface == Surface.VerifiedHostMapping &&
            (!evidence.HostMappingFromTrustedRegistry ||
             !evidence.HostMappingUnique ||
             !evidence.HostMappingFresh ||
             !ValidId(evidence.RegisteredHostTenantId) ||
             !string.Equals(evidence.RegisteredHostTenantId,
                 evidence.VerifiedTenantId, StringComparison.Ordinal) ||
             (evidence.ForwardedHostUsed && !evidence.ForwardedProxyTrusted)))
            return new("TRUSTED_HOST_MAPPING_REQUIRED");

        // Resource access needs a fresh persisted owner, never client-provided
        // EmpresaId or ownership inherited from a previous session.
        if (request.ResourceOwnerTenantId is not null &&
            (!request.ResourceOwnershipReadFromPersistence ||
             !ValidId(request.ResourceOwnerTenantId) ||
             !string.Equals(request.ResourceOwnerTenantId,
                 evidence.VerifiedTenantId, StringComparison.Ordinal)))
            return new("RESOURCE_TENANT_OWNER_MISMATCH");

        if (request.IncludesBranch &&
            (!request.BranchOwnershipReadFromPersistence ||
             !ValidId(request.BranchOwnerTenantId) ||
             !string.Equals(request.BranchOwnerTenantId,
                 evidence.VerifiedTenantId, StringComparison.Ordinal)))
            return new("BRANCH_TENANT_OWNER_MISMATCH");

        // This does not verify an incoming JWT or query the membership store:
        // the trusted API/Application adapter must supply fresh evidence, enforce
        // RBAC and ownership, audit safe denials, and apply EF tenant filters.
        return new("TRUSTED_BACKEND_ENFORCEMENT_REQUIRED");
    }
}
