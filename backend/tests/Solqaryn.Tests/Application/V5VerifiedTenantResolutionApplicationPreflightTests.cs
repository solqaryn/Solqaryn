using Solqaryn.Application.AutomationV5;
using Xunit;
using Guard = Solqaryn.Domain.AutomationV5.V5VerifiedTenantResolutionDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class V5VerifiedTenantResolutionApplicationPreflightTests
{
    private readonly V5VerifiedTenantResolutionApplicationPreflight _sut = new();

    private static Guard.ServerEvidence Evidence() => new()
    {
        AuthenticatedActorId = "actor-17",
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

    private static V5VerifiedTenantResolutionApplicationPreflight.RequestIntent Request(
        Guard.Surface surface = Guard.Surface.SessionSelection) =>
        new(surface, "tenant-A", false, false, false, false);

    private static V5VerifiedTenantResolutionApplicationPreflight.VerifiedResourceScope Scope() =>
        new("tenant-A", true, true, "tenant-A", true);

    private string Code(
        Guard.ServerEvidence? evidence = null,
        V5VerifiedTenantResolutionApplicationPreflight.RequestIntent? request = null,
        V5VerifiedTenantResolutionApplicationPreflight.VerifiedResourceScope? scope = null) =>
        _sut.ReviewOnly(evidence ?? Evidence(), scope ?? Scope(), request ?? Request()).Code;

    [Fact]
    public void ValidServerSnapshotNeverBecomesRuntimeAuthorization()
    {
        var result = _sut.ReviewOnly(Evidence(), Scope(), Request());
        Assert.Equal("TRUSTED_BACKEND_ENFORCEMENT_REQUIRED", result.Code);
        Assert.True(result.RequiresLiveBackendEnforcement);
        Assert.False(result.AuthorizesTenantAccess);
        Assert.False(result.ReadsOrWritesResource);
        Assert.False(result.InstallsTenantContext);
        Assert.False(result.IssuesJwtOrGrants);
    }

    [Fact]
    public void MissingIntentOrTrustedResourceCannotProceed()
    {
        Assert.Equal("TENANT_REQUEST_INTENT_REQUIRED",
            _sut.ReviewOnly(Evidence(), Scope(), null).Code);
        Assert.Equal("TRUSTED_BACKEND_SNAPSHOT_REQUIRED",
            _sut.ReviewOnly(null, Scope(), Request()).Code);
        Assert.Equal("TRUSTED_BACKEND_SNAPSHOT_REQUIRED",
            _sut.ReviewOnly(Evidence(), null, Request()).Code);
    }

    [Fact]
    public void RequestedTenantIsNotAuthorityOrDefault()
    {
        Assert.Equal("CLIENT_TENANT_CONSISTENCY_MISMATCH",
            Code(request: Request() with { RequestedTenantId = "tenant-B" }));
        Assert.Equal("CLIENT_TENANT_CONSISTENCY_MISMATCH",
            Code(request: Request() with { RequestedTenantId = null }));
        Assert.Equal("CLIENT_TENANT_CONSISTENCY_MISMATCH",
            Code(request: Request() with { RequestedTenantId = "" }));
    }

    [Fact]
    public void ClientTenantHostAndLegacyRoleAuthorityIsDenied()
    {
        Assert.Equal("CLIENT_TENANT_AUTHORITY_OVERRIDE_DENIED",
            Code(request: Request() with { ClientClaimsTenantOrHostAuthority = true }));
        Assert.Equal("CLIENT_TENANT_AUTHORITY_OVERRIDE_DENIED",
            Code(request: Request() with { ClientClaimsLegacyRoleAuthority = true }));
        Assert.Equal("CLIENT_TENANT_AUTHORITY_OVERRIDE_DENIED",
            Code(request: Request() with { RequestsDefaultFallback = true }));
    }

    [Fact]
    public void WriteExportAndGrantAreNotPossibleInPreflight()
    {
        var result = _sut.ReviewOnly(Evidence(), Scope(),
            Request() with { AttemptsWriteExportOrGrant = true });
        Assert.Equal("TENANT_PREFLIGHT_CANNOT_MUTATE_EXPORT_OR_GRANT", result.Code);
        Assert.False(result.ReadsOrWritesResource);
        Assert.False(result.IssuesJwtOrGrants);
    }

    [Fact]
    public void MissingPersistedResourceOwnerFailsClosed()
    {
        Assert.Equal("PERSISTED_RESOURCE_OWNER_REQUIRED",
            Code(scope: Scope() with { OwnerTenantId = null }));
        Assert.Equal("PERSISTED_RESOURCE_OWNER_REQUIRED",
            Code(scope: Scope() with { OwnershipReadFromPersistence = false }));
    }

    [Fact]
    public void ResourceOwnerTenantBIsDeniedForVerifiedTenantA()
    {
        Assert.Equal("RESOURCE_TENANT_OWNER_MISMATCH",
            Code(scope: Scope() with { OwnerTenantId = "tenant-B" }));
    }

    [Fact]
    public void BranchTenantBOrUnverifiedBranchOwnershipIsDenied()
    {
        Assert.Equal("BRANCH_TENANT_OWNER_MISMATCH",
            Code(scope: Scope() with { BranchOwnerTenantId = "tenant-B" }));
        Assert.Equal("BRANCH_TENANT_OWNER_MISMATCH",
            Code(scope: Scope() with { BranchOwnershipReadFromPersistence = false }));
    }

    [Fact]
    public void RevokedMembershipAndUnavailableStoreFailClosed()
    {
        Assert.Equal("FRESH_TENANT_MEMBERSHIP_REQUIRED",
            Code(evidence: Evidence() with { MembershipOrGrantRevoked = true }));
        Assert.Equal("FRESH_TENANT_MEMBERSHIP_REQUIRED",
            Code(evidence: Evidence() with { AuthorityStoreAvailable = false }));
        Assert.Equal("FRESH_TENANT_MEMBERSHIP_REQUIRED",
            Code(evidence: Evidence() with { CurrentRoleGrantVerified = false }));
    }

    [Fact]
    public void MissingJwtOrInactiveActorIsNotAuthorized()
    {
        Assert.Equal("AUTHENTICATED_SERVER_IDENTITY_REQUIRED",
            Code(evidence: Evidence() with { ServerJwtValidated = false }));
        Assert.Equal("AUTHENTICATED_SERVER_IDENTITY_REQUIRED",
            Code(evidence: Evidence() with { CurrentUserActive = false }));
    }

    [Fact]
    public void HostMappingNeedsTrustedUniqueFreshRegistry()
    {
        var host = Request(Guard.Surface.VerifiedHostMapping);
        Assert.True(_sut.ReviewOnly(Evidence(), Scope(), host).RequiresLiveBackendEnforcement);
        Assert.Equal("TRUSTED_HOST_MAPPING_REQUIRED",
            Code(request: host, evidence: Evidence() with { HostMappingUnique = false }));
        Assert.Equal("TRUSTED_HOST_MAPPING_REQUIRED",
            Code(request: host, evidence: Evidence() with
            {
                ForwardedHostUsed = true,
                ForwardedProxyTrusted = false
            }));
    }

    [Fact]
    public void SwitchingTenantMustHaveIndependentFreshTenantBMembershipAndOwnership()
    {
        var bEvidence = Evidence() with
        {
            VerifiedTenantId = "tenant-B",
            RegisteredHostTenantId = "tenant-B"
        };
        var bScope = Scope() with
        {
            OwnerTenantId = "tenant-B",
            BranchOwnerTenantId = "tenant-B"
        };
        var bRequest = Request() with { RequestedTenantId = "tenant-B" };
        Assert.True(_sut.ReviewOnly(bEvidence, bScope, bRequest)
            .RequiresLiveBackendEnforcement);
        Assert.Equal("RESOURCE_TENANT_OWNER_MISMATCH",
            _sut.ReviewOnly(bEvidence,
                bScope with { OwnerTenantId = "tenant-A" }, bRequest).Code);
        Assert.Equal("FRESH_TENANT_MEMBERSHIP_REQUIRED",
            _sut.ReviewOnly(bEvidence with { CurrentMembershipActive = false },
                bScope, bRequest).Code);
    }
}
