using Solqaryn.Domain.AutomationV5;
using Xunit;
using Guard = Solqaryn.Domain.AutomationV5.V5VerifiedTenantResolutionDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class V5VerifiedTenantResolutionDomainRulesTests
{
    private static Guard.ServerEvidence Evidence() => new()
    {
        AuthenticatedActorId = "actor-A",
        VerifiedTenantId = "tenant-A",
        ServerJwtValidated = true,
        CurrentUserActive = true,
        CurrentTenantActive = true,
        MembershipReadFromPersistence = true,
        CurrentMembershipActive = true,
        CurrentRoleGrantVerified = true,
        AuthorityStoreAvailable = true,
        RegisteredHostTenantId = "tenant-A",
        HostMappingFromTrustedRegistry = true,
        HostMappingUnique = true,
        HostMappingFresh = true,
        ForwardedProxyTrusted = true
    };

    private static Guard.ProposedResolution Request(
        Guard.Surface surface = Guard.Surface.SessionSelection) => new()
    {
        Surface = surface,
        ClientRequestedTenantId = "tenant-A",
        ResourceOwnerTenantId = "tenant-A",
        ResourceOwnershipReadFromPersistence = true,
        IncludesBranch = true,
        BranchOwnerTenantId = "tenant-A",
        BranchOwnershipReadFromPersistence = true
    };

    private static string Code(Guard.ServerEvidence? evidence = null,
        Guard.ProposedResolution? request = null) =>
        Guard.Evaluate(evidence ?? Evidence(), request ?? Request()).Code;

    [Fact]
    public void CurrentTenantAWithFreshStoreEvidenceOnlyRequestsRuntimeEnforcement()
    {
        var result = Guard.Evaluate(Evidence(), Request());
        Assert.Equal("TRUSTED_BACKEND_ENFORCEMENT_REQUIRED", result.Code);
        Assert.True(result.RequiresRuntimeEnforcement);
        Assert.False(result.AuthorizesTenantAccess);
    }

    [Fact]
    public void VerifiedHostMappingNeedsTrustedUniqueFreshRegistry()
    {
        var host = Request(Guard.Surface.VerifiedHostMapping);
        Assert.True(Guard.Evaluate(Evidence(), host).RequiresRuntimeEnforcement);
        Assert.Equal("TRUSTED_HOST_MAPPING_REQUIRED",
            Guard.Evaluate(Evidence() with { HostMappingUnique = false }, host).Code);
        Assert.Equal("TRUSTED_HOST_MAPPING_REQUIRED",
            Guard.Evaluate(Evidence() with { HostMappingFresh = false }, host).Code);
        Assert.Equal("TRUSTED_HOST_MAPPING_REQUIRED",
            Guard.Evaluate(Evidence() with { HostMappingFromTrustedRegistry = false }, host).Code);
    }

    [Fact]
    public void SpoofedForwardedHostNeverSetsTenantAuthority()
    {
        var host = Request(Guard.Surface.VerifiedHostMapping);
        Assert.Equal("TRUSTED_HOST_MAPPING_REQUIRED",
            Guard.Evaluate(Evidence() with
            {
                ForwardedHostUsed = true,
                ForwardedProxyTrusted = false
            }, host).Code);
        Assert.Equal("TRUSTED_HOST_MAPPING_REQUIRED",
            Guard.Evaluate(Evidence() with { RegisteredHostTenantId = "tenant-B" }, host).Code);
    }

    [Fact]
    public void UnauthenticatedOrInactiveActorsAreDenied()
    {
        Assert.Equal("AUTHENTICATED_SERVER_IDENTITY_REQUIRED",
            Guard.Evaluate(null, Request()).Code);
        Assert.Equal("AUTHENTICATED_SERVER_IDENTITY_REQUIRED",
            Guard.Evaluate(Evidence(), null).Code);
        Assert.Equal("AUTHENTICATED_SERVER_IDENTITY_REQUIRED",
            Code(Evidence() with { ServerJwtValidated = false }));
        Assert.Equal("AUTHENTICATED_SERVER_IDENTITY_REQUIRED",
            Code(Evidence() with { CurrentUserActive = false }));
        Assert.Equal("AUTHENTICATED_SERVER_IDENTITY_REQUIRED",
            Code(Evidence() with { AuthenticatedActorId = " " }));
    }

    [Fact]
    public void TenantRevocationAndSuspensionOverrideFormerJwt()
    {
        Assert.Equal("FRESH_TENANT_MEMBERSHIP_REQUIRED",
            Code(Evidence() with { MembershipOrGrantRevoked = true }));
        Assert.Equal("FRESH_TENANT_MEMBERSHIP_REQUIRED",
            Code(Evidence() with { CurrentMembershipActive = false }));
        Assert.Equal("FRESH_TENANT_MEMBERSHIP_REQUIRED",
            Code(Evidence() with { CurrentTenantActive = false }));
        Assert.Equal("FRESH_TENANT_MEMBERSHIP_REQUIRED",
            Code(Evidence() with { CurrentRoleGrantVerified = false }));
    }

    [Fact]
    public void StaleMembershipOrUnavailableAuthorityStoreFailsClosed()
    {
        Assert.Equal("FRESH_TENANT_MEMBERSHIP_REQUIRED",
            Code(Evidence() with { MembershipReadFromPersistence = false }));
        Assert.Equal("FRESH_TENANT_MEMBERSHIP_REQUIRED",
            Code(Evidence() with { AuthorityStoreAvailable = false }));
    }

    [Fact]
    public void ClientTenantHostLegacyRoleAndFallbackNeverConferAuthority()
    {
        Assert.Equal("CLIENT_OR_LEGACY_AUTHORITY_DENIED",
            Code(request: Request() with { TreatsClientTenantAsAuthority = true }));
        Assert.Equal("CLIENT_OR_LEGACY_AUTHORITY_DENIED",
            Code(request: Request() with { TreatsClientHostAsAuthority = true }));
        Assert.Equal("CLIENT_OR_LEGACY_AUTHORITY_DENIED",
            Code(request: Request() with { TreatsLegacyGlobalRoleAsAuthority = true }));
        Assert.Equal("CLIENT_OR_LEGACY_AUTHORITY_DENIED",
            Code(request: Request() with { AttemptsTenantDefaultFallback = true }));
    }

    [Fact]
    public void TenantBCannotBorrowTenantAMembershipOrResourceOwnership()
    {
        Assert.Equal("VERIFIED_TENANT_MISMATCH",
            Code(request: Request() with { ClientRequestedTenantId = "tenant-B" }));
        Assert.Equal("RESOURCE_TENANT_OWNER_MISMATCH",
            Code(request: Request() with { ResourceOwnerTenantId = "tenant-B" }));
        Assert.Equal("RESOURCE_TENANT_OWNER_MISMATCH",
            Code(request: Request() with { ResourceOwnershipReadFromPersistence = false }));
    }

    [Fact]
    public void BranchFromTenantBOrUnverifiedOwnershipIsDenied()
    {
        Assert.Equal("BRANCH_TENANT_OWNER_MISMATCH",
            Code(request: Request() with { BranchOwnerTenantId = "tenant-B" }));
        Assert.Equal("BRANCH_TENANT_OWNER_MISMATCH",
            Code(request: Request() with { BranchOwnershipReadFromPersistence = false }));
    }

    [Fact]
    public void UnknownSurfaceAndMalformedIdentifiersAreDenied()
    {
        Assert.Equal("UNKNOWN_TENANT_RESOLUTION_SURFACE",
            Code(request: Request((Guard.Surface)999)));
        Assert.Equal("AUTHENTICATED_SERVER_IDENTITY_REQUIRED",
            Code(Evidence() with { VerifiedTenantId = "tenant A" }));
        Assert.Equal("VERIFIED_TENANT_MISMATCH",
            Code(request: Request() with { ClientRequestedTenantId = new string('x', 129) }));
    }

    [Fact]
    public void SwitchingContextFromAToBRequiresFreshBMembershipAndBResource()
    {
        var tenantB = Evidence() with
        {
            VerifiedTenantId = "tenant-B",
            RegisteredHostTenantId = "tenant-B"
        };
        var requestB = Request() with
        {
            ClientRequestedTenantId = "tenant-B",
            ResourceOwnerTenantId = "tenant-B",
            BranchOwnerTenantId = "tenant-B"
        };
        Assert.True(Guard.Evaluate(tenantB, requestB).RequiresRuntimeEnforcement);
        Assert.Equal("RESOURCE_TENANT_OWNER_MISMATCH",
            Guard.Evaluate(tenantB, requestB with { ResourceOwnerTenantId = "tenant-A" }).Code);
        Assert.Equal("FRESH_TENANT_MEMBERSHIP_REQUIRED",
            Guard.Evaluate(tenantB with { CurrentMembershipActive = false }, requestB).Code);
    }

    [Fact]
    public void DomainVerdictNeverAuthorizesHttpOrDatabaseAccess()
    {
        Assert.False(Guard.Evaluate(Evidence(), Request()).AuthorizesTenantAccess);
        Assert.False(Guard.Evaluate(Evidence(), Request() with
        {
            ResourceOwnerTenantId = "tenant-B"
        }).AuthorizesTenantAccess);
    }
}
