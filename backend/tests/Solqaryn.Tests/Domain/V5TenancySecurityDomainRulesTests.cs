using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5TenancySecurityDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class V5TenancySecurityDomainRulesTests
{
    private static Rules.TrustedAuthority Authority() => new(7, 11, 3, true, true, false, false);
    private static Rules.PersistedResource Resource() => new(7, 7, 3, true, false, false);
    private static Rules.Request Request(Rules.Surface surface = Rules.Surface.BusinessRead) =>
        new(surface, false, null, false);
    private static string Code(Rules.TrustedAuthority? a = null, Rules.PersistedResource? r = null,
        Rules.Request? q = null) => Rules.Evaluate(a ?? Authority(), r ?? Resource(), q ?? Request()).Code;

    [Fact]
    public void ScopedTrustedReadOnlySignalsFurtherBackendEnforcement()
    {
        var outcome = Rules.Evaluate(Authority(), Resource(), Request());
        Assert.True(outcome.RequiresServerEnforcement);
        Assert.Equal("READY_FOR_SERVER_SECURITY_ENFORCEMENT", outcome.Code);
    }

    [Fact]
    public void MissingServerIdentityMembershipAndPermissionFailClosed()
    {
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED", Rules.Evaluate(null, Resource(), Request()).Code);
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED", Code(Authority() with { AuthenticatedTenantId = null }));
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED", Code(Authority() with { AuthenticatedActorId = 0 }));
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED", Code(Authority() with { ActiveMembershipVerified = false }));
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED", Code(Authority() with { RequiredRoleVerified = false }));
    }

    [Fact]
    public void QueryBypassAndEnumerationNeverAuthorizeRead()
    {
        Assert.Equal("TENANT_QUERY_BYPASS_DENIED", Code(q: Request() with { BypassesTenantFilters = true }));
        Assert.Equal("TENANT_QUERY_BYPASS_DENIED", Code(q: Request() with { AttemptsCrossTenantEnumeration = true }));
    }

    [Fact]
    public void TenantBAndLegacyNullRemainInaccessibleToTenantA()
    {
        Assert.Equal("RESOURCE_OWNER_MISMATCH", Code(r: Resource() with { OwnerTenantId = 8 }));
        Assert.Equal("RESOURCE_OWNER_MISMATCH", Code(r: Resource() with { ParentTenantId = 8 }));
        Assert.Equal("RESOURCE_OWNER_MISMATCH", Code(r: Resource() with { OwnerTenantId = null }));
        Assert.Equal("RESOURCE_OWNER_MISMATCH", Code(r: Resource() with { ParentTenantId = null }));
        Assert.Equal("RESOURCE_OWNER_MISMATCH", Code(r: Resource() with { OwnershipReadFromPersistence = false }));
    }

    [Fact]
    public void BranchOwnershipRequiresMatchingAuthenticatedBranch()
    {
        Assert.Equal("BRANCH_SCOPE_MISMATCH",
            Code(r: Resource() with { OwnerBranchId = 4 }, q: Request(Rules.Surface.BranchRead)));
        Assert.Equal("BRANCH_SCOPE_MISMATCH",
            Code(a: Authority() with { AuthenticatedBranchId = null }, q: Request(Rules.Surface.BranchRead)));
        Assert.Equal("READY_FOR_SERVER_SECURITY_ENFORCEMENT", Code(q: Request(Rules.Surface.BranchRead)));
    }

    [Fact]
    public void RevokedAndExpiredFileLinksNeverPass()
    {
        Assert.Equal("FILE_ACCESS_REVOKED_OR_EXPIRED",
            Code(r: Resource() with { AccessRevoked = true }, q: Request(Rules.Surface.FileDownload)));
        Assert.Equal("FILE_ACCESS_REVOKED_OR_EXPIRED",
            Code(r: Resource() with { SignedLinkExpired = true }, q: Request(Rules.Surface.FileDownload)));
    }

    [Fact]
    public void JobsRequireVerifiedServiceGrantAuditAndIdempotency()
    {
        var job = Request(Rules.Surface.BackgroundJob) with { IdempotencyKey = "job-7" };
        Assert.Equal("SERVICE_JOB_AUTHORITY_REQUIRED", Code(q: job));
        var service = Authority() with { PrivilegedServiceGrantVerified = true, AuditedServiceContextVerified = true };
        Assert.Equal("READY_FOR_SERVER_SECURITY_ENFORCEMENT", Code(a: service, q: job));
        Assert.Equal("SERVICE_JOB_AUTHORITY_REQUIRED", Code(a: service, q: job with { IdempotencyKey = "bad key" }));
    }

    [Fact]
    public void ControlPlaneRequiresVerifiedIndependentServiceAuthority()
    {
        Assert.Equal("CONTROL_PLANE_AUTHORITY_REQUIRED", Code(q: Request(Rules.Surface.ControlPlane)));
        Assert.Equal("CONTROL_PLANE_AUTHORITY_REQUIRED", Code(a: Authority() with
        { PrivilegedServiceGrantVerified = true }, q: Request(Rules.Surface.ControlPlane)));
        Assert.Equal("READY_FOR_SERVER_SECURITY_ENFORCEMENT", Code(a: Authority() with
        { PrivilegedServiceGrantVerified = true, AuditedServiceContextVerified = true },
            q: Request(Rules.Surface.ControlPlane)));
    }

    [Fact]
    public void UnknownSurfaceCannotBorrowNormalRole()
    {
        Assert.Equal("UNKNOWN_SECURITY_SURFACE", Code(q: Request((Rules.Surface)999)));
    }
}
