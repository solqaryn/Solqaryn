using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5TenancySecurityDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class V5TenancySecurityApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private sealed class AuthoritySource : IV5TenancySecurityAuthoritySource
    {
        public Rules.TrustedAuthority? Current { get; set; } =
            new(7, 11, 3, true, true, false, false);
        public int Calls { get; private set; }
        public ValueTask<Rules.TrustedAuthority?> ReadVerifiedAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return ValueTask.FromResult(Current);
        }
    }

    private sealed class ResourceSource : IV5TenancySecurityResourceSource
    {
        public Rules.PersistedResource? Current { get; set; } =
            new(7, 7, 3, true, false, false);
        public int Calls { get; private set; }
        public int? SeenTenantId { get; private set; }
        public ValueTask<Rules.PersistedResource?> ReadScopedAsync(
            int resourceId, int verifiedTenantId, CancellationToken cancellationToken)
        {
            Calls++;
            SeenTenantId = verifiedTenantId;
            return ValueTask.FromResult(Current);
        }
    }

    private static V5TenancySecurityApplicationService.Request Valid(
        Rules.Surface surface = Rules.Surface.BusinessRead) =>
        new(101, surface, Sha, Sha, "git-evidence:source-sha", false, false, null);

    private static (V5TenancySecurityApplicationService Service,
        AuthoritySource Authority, ResourceSource Resource) Setup()
    {
        var authority = new AuthoritySource();
        var resource = new ResourceSource();
        return (new V5TenancySecurityApplicationService(authority, resource),
            authority, resource);
    }

    [Fact]
    public async Task ScopedBusinessReadRequiresAuthenticatedIntegrationAndGrantsNothing()
    {
        var (service, _, resource) = Setup();
        var result = await service.PrepareAsync(Valid());
        Assert.Equal("AUTHENTICATED_SECURITY_TENANCY_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsAuthenticatedAdapter);
        Assert.Equal(7, resource.SeenTenantId);
        Assert.False(result.GrantsResourceAccess);
        Assert.False(result.AllowsFileDownload);
        Assert.False(result.AllowsBackgroundJob);
        Assert.False(result.AllowsAdministrativeAction);
        Assert.False(result.AllowsTenantFilterBypass);
    }

    [Fact]
    public async Task UntrustedOrMismatchedSourceNeverCallsAnyReader()
    {
        var (service, authority, resource) = Setup();
        Assert.Equal("SECURITY_TENANCY_APPLICATION_REQUEST_REQUIRED",
            (await service.PrepareAsync(null)).Code);
        Assert.Equal("SECURITY_TENANCY_SOURCE_SHA_MISMATCH",
            (await service.PrepareAsync(Valid() with { ObservedSourceSha = OtherSha })).Code);
        Assert.Equal("SECURITY_TENANCY_SOURCE_SHA_MISMATCH",
            (await service.PrepareAsync(Valid() with { ExpectedSourceSha = null })).Code);
        Assert.Equal(0, authority.Calls);
        Assert.Equal(0, resource.Calls);
    }

    [Fact]
    public async Task UnsafeProofInvalidResourceAndUnknownSurfaceFailBeforePersistence()
    {
        var (service, authority, resource) = Setup();
        Assert.Equal("SECURITY_TENANCY_RESOURCE_OR_SURFACE_INVALID",
            (await service.PrepareAsync(Valid() with { ResourceId = 0 })).Code);
        Assert.Equal("SECURITY_TENANCY_RESOURCE_OR_SURFACE_INVALID",
            (await service.PrepareAsync(Valid((Rules.Surface)999))).Code);
        Assert.Equal("SECURITY_TENANCY_PROOF_REFERENCE_REQUIRED",
            (await service.PrepareAsync(Valid() with { SanitizedProofReference = "https://private.example/key" })).Code);
        Assert.Equal("SECURITY_TENANCY_PROOF_REFERENCE_REQUIRED",
            (await service.PrepareAsync(Valid() with { SanitizedProofReference = "a@private" })).Code);
        Assert.Equal(0, authority.Calls);
        Assert.Equal(0, resource.Calls);
    }

    [Fact]
    public async Task TenantBypassAndEnumerationNeverReachReaders()
    {
        var (service, authority, resource) = Setup();
        Assert.Equal("TENANT_QUERY_BYPASS_DENIED",
            (await service.PrepareAsync(Valid() with { RequestsTenantFilterBypass = true })).Code);
        Assert.Equal("TENANT_QUERY_BYPASS_DENIED",
            (await service.PrepareAsync(Valid() with { AttemptsCrossTenantEnumeration = true })).Code);
        Assert.Equal(0, authority.Calls);
        Assert.Equal(0, resource.Calls);
    }

    [Fact]
    public async Task UnverifiedIdentityMembershipOrRoleNeverReadsResource()
    {
        var (service, authority, resource) = Setup();
        authority.Current = authority.Current! with { ActiveMembershipVerified = false };
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED", (await service.PrepareAsync(Valid())).Code);
        authority.Current = authority.Current! with { ActiveMembershipVerified = true, RequiredRoleVerified = false };
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED", (await service.PrepareAsync(Valid())).Code);
        authority.Current = authority.Current! with { RequiredRoleVerified = true, AuthenticatedTenantId = null };
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED", (await service.PrepareAsync(Valid())).Code);
        Assert.Equal(0, resource.Calls);
    }

    [Fact]
    public async Task ForeignTenantParentOrUnverifiedPersistenceCannotPass()
    {
        var (service, _, resource) = Setup();
        resource.Current = resource.Current! with { OwnerTenantId = 8 };
        Assert.Equal("RESOURCE_OWNER_MISMATCH", (await service.PrepareAsync(Valid())).Code);
        resource.Current = resource.Current! with { OwnerTenantId = 7, ParentTenantId = 8 };
        Assert.Equal("RESOURCE_OWNER_MISMATCH", (await service.PrepareAsync(Valid())).Code);
        resource.Current = resource.Current! with { ParentTenantId = 7, OwnershipReadFromPersistence = false };
        Assert.Equal("RESOURCE_OWNER_MISMATCH", (await service.PrepareAsync(Valid())).Code);
        resource.Current = null;
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED", (await service.PrepareAsync(Valid())).Code);
    }

    [Fact]
    public async Task BranchFileAndServiceJobSurfacesRemainDeniedWithoutProof()
    {
        var (service, authority, resource) = Setup();
        resource.Current = resource.Current! with { OwnerBranchId = 4 };
        Assert.Equal("BRANCH_SCOPE_MISMATCH",
            (await service.PrepareAsync(Valid(Rules.Surface.BranchRead))).Code);
        resource.Current = resource.Current! with { OwnerBranchId = 3, SignedLinkExpired = true };
        Assert.Equal("FILE_ACCESS_REVOKED_OR_EXPIRED",
            (await service.PrepareAsync(Valid(Rules.Surface.FileDownload))).Code);
        resource.Current = resource.Current! with { SignedLinkExpired = false };
        Assert.Equal("SERVICE_JOB_AUTHORITY_REQUIRED",
            (await service.PrepareAsync(Valid(Rules.Surface.BackgroundJob))).Code);
        authority.Current = authority.Current! with
        { PrivilegedServiceGrantVerified = true, AuditedServiceContextVerified = true };
        Assert.Equal("SERVICE_JOB_AUTHORITY_REQUIRED",
            (await service.PrepareAsync(Valid(Rules.Surface.BackgroundJob))).Code);
        Assert.Equal("AUTHENTICATED_SECURITY_TENANCY_ADAPTER_REQUIRED",
            (await service.PrepareAsync(Valid(Rules.Surface.BackgroundJob) with
            { IdempotencyKey = "job-101" })).Code);
    }

    [Fact]
    public async Task ControlPlaneRequiresIndependentAuditedServiceAuthority()
    {
        var (service, authority, _) = Setup();
        Assert.Equal("CONTROL_PLANE_AUTHORITY_REQUIRED",
            (await service.PrepareAsync(Valid(Rules.Surface.ControlPlane))).Code);
        authority.Current = authority.Current! with
        { PrivilegedServiceGrantVerified = true, AuditedServiceContextVerified = true };
        var result = await service.PrepareAsync(Valid(Rules.Surface.ControlPlane));
        Assert.True(result.NeedsAuthenticatedAdapter);
        Assert.False(result.AllowsAdministrativeAction);
    }

    [Fact]
    public async Task MissingAdaptersAreRejectedAtConstruction()
    {
        var authority = new AuthoritySource();
        var resources = new ResourceSource();
        Assert.Throws<ArgumentNullException>(() => new V5TenancySecurityApplicationService(null!, resources));
        Assert.Throws<ArgumentNullException>(() => new V5TenancySecurityApplicationService(authority, null!));
        await Task.CompletedTask;
    }
}
