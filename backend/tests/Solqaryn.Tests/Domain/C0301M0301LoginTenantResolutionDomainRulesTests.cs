using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class C0301M0301LoginTenantResolutionDomainRulesTests
{
    private static C0301LoginTenantResolutionDomainRules.Membership Active(int tenant) =>
        new(11, tenant, true, true, true, true, true);

    private static C0301LoginTenantResolutionDomainRules.Request Valid(
        params C0301LoginTenantResolutionDomainRules.Membership[] memberships) =>
        new(11, memberships, null, null, true, false, false, false, false);

    private static string Code(C0301LoginTenantResolutionDomainRules.Request? request) =>
        C0301LoginTenantResolutionDomainRules.Resolve(request).Code;

    [Fact]
    public void ZeroMembershipsRejectEvenWithStalePreferenceOrLegacyRole()
    {
        var result = C0301LoginTenantResolutionDomainRules.Resolve(Valid() with
        {
            LastPreferredTenantId = 20, PreferenceOwnerUserId = 11
        });
        Assert.Equal("NO_VALID_ENTERPRISE_MEMBERSHIP", result.Code);
        Assert.Null(result.CandidateTenantId);
        Assert.Empty(result.ValidTenantIds);
        Assert.False(result.GrantsTenantAccess);
    }

    [Fact]
    public void OneValidTenantIgnoresRevokedPreferenceToOtherTenant()
    {
        var r = C0301LoginTenantResolutionDomainRules.Resolve(Valid(Active(10),
            Active(20) with { MembershipActive = false }) with
        {
            LastPreferredTenantId = 20, PreferenceOwnerUserId = 11
        });
        Assert.Equal("SINGLE_TENANT_REQUIRES_SERVER_ENFORCEMENT", r.Code);
        Assert.Equal(10, r.CandidateTenantId);
        Assert.Equal(new[] { 10 }, r.ValidTenantIds);
        Assert.False(r.GrantsTenantAccess);
    }

    [Fact]
    public void MultipleTenantsPreferOnlyOwnStillActivePreference()
    {
        var r = C0301LoginTenantResolutionDomainRules.Resolve(
            Valid(Active(20), Active(10)) with { LastPreferredTenantId = 20, PreferenceOwnerUserId = 11 });
        Assert.Equal("VALID_PREFERENCE_REQUIRES_SERVER_ENFORCEMENT", r.Code);
        Assert.Equal(20, r.CandidateTenantId);
        Assert.Equal(new[] { 10, 20 }, r.ValidTenantIds);
    }

    [Fact]
    public void AmbiguousOrForeignUserPreferenceRequiresExplicitSelection()
    {
        var own = Valid(Active(10), Active(20)) with {
            LastPreferredTenantId = 30, PreferenceOwnerUserId = 11 };
        var foreign = own with { LastPreferredTenantId = 10, PreferenceOwnerUserId = 99 };
        Assert.Equal("EXPLICIT_TENANT_SELECTION_REQUIRED", Code(own));
        var r = C0301LoginTenantResolutionDomainRules.Resolve(foreign);
        Assert.Null(r.CandidateTenantId);
        Assert.Equal(new[] { 10, 20 }, r.ValidTenantIds);
    }

    [Fact]
    public void AToBMustRejectRevokedAndStaleMembership()
    {
        var r = Valid(Active(10), Active(20) with {
            CurrentServerRevalidationCompleted = false }) with {
                LastPreferredTenantId = 20, PreferenceOwnerUserId = 11 };
        Assert.Equal("SINGLE_TENANT_REQUIRES_SERVER_ENFORCEMENT", Code(r));
        Assert.Equal(10, C0301LoginTenantResolutionDomainRules.Resolve(r).CandidateTenantId);
        Assert.Equal("NO_VALID_ENTERPRISE_MEMBERSHIP",
            Code(Valid(Active(10) with { RoleActiveAndAssignableToTenant = false })));
    }

    [Fact]
    public void RejectsForgedUserSnapshotOrDuplicateTenantRows()
    {
        Assert.Equal("FOREIGN_OR_INVALID_MEMBERSHIP_SNAPSHOT",
            Code(Valid(Active(10), Active(20) with { UserId = 99 })));
        Assert.Equal("DUPLICATE_TENANT_MEMBERSHIP_DENIED",
            Code(Valid(Active(10), Active(10))));
        Assert.Equal("FOREIGN_OR_INVALID_MEMBERSHIP_SNAPSHOT",
            Code(Valid(Active(0))));
    }

    [Fact]
    public void IdentityAndClientOverrideAreAlwaysFailClosed()
    {
        Assert.Equal("TRUSTED_ACTIVE_IDENTITY_REQUIRED", Code(null));
        Assert.Equal("TRUSTED_ACTIVE_IDENTITY_REQUIRED",
            Code(Valid(Active(10)) with { IdentityVerifiedByServer = false }));
        Assert.Equal("TRUSTED_ACTIVE_IDENTITY_REQUIRED",
            Code(Valid(Active(10)) with { AccountLockedOrRevoked = true }));
        Assert.Equal("CLIENT_TENANT_OVERRIDE_DENIED",
            Code(Valid(Active(10)) with { ClientTenantOverrideAttempted = true }));
        Assert.Equal("FRESH_SERVER_MEMBERSHIP_EVIDENCE_REQUIRED",
            Code(Valid() with { Memberships = null }));
    }

    [Fact]
    public void EnterpriseRoleNeverImpliesGlobalConsoleGrant()
    {
        var noGlobal = Valid() with { PlatformAuthorityVerified = true };
        Assert.Equal("NO_VALID_ENTERPRISE_MEMBERSHIP", Code(noGlobal));
        var explicitGlobal = C0301LoginTenantResolutionDomainRules.Resolve(noGlobal with {
            ExplicitPlatformConsoleGrantVerified = true
        });
        Assert.Equal("SEPARATE_PLATFORM_LOGIN_REQUIRES_SERVER_GRANT", explicitGlobal.Code);
        Assert.Null(explicitGlobal.CandidateTenantId);
        Assert.False(explicitGlobal.GrantsPlatformAccess);
        Assert.False(explicitGlobal.GrantsTenantAccess);
    }
}
