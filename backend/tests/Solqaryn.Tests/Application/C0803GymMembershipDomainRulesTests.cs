using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0803GymMembershipDomainRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 0, 0, 0, DateTimeKind.Utc);

    private static C0803GymMembershipDomainRules.VerifiedActor Actor() =>
        new(91, 11, 111, true, true, true, false);

    private static C0803GymMembershipDomainRules.MembershipSnapshot Membership() =>
        new(11, 801, 401, 111,
            C0803GymMembershipDomainRules.MembershipState.Active,
            7, Now.AddDays(-1), Now.AddDays(1), true, false, false, true);

    private static C0803GymMembershipDomainRules.AccessRequest Request() =>
        new(11, 801, 401, 111, 7, Now, false, false);

    [Fact]
    public void AllServerVerifiedPreconditionsOnlyYieldCandidateNeverGrant()
    {
        var result = C0803GymMembershipDomainRules.Evaluate(Actor(), Membership(), Request());
        Assert.Equal("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED", result.Code);
        Assert.True(result.CandidateForBackendRevalidation);
        Assert.False(result.AuthorizesDoorOpening);
        Assert.False(result.AuthorizesJwtOrRbac);
        Assert.False(result.AuthorizesPayment);
        Assert.False(result.CertifiesHttpMysqlOrHardware);
    }

    [Fact]
    public void NullOrUnboundSnapshotsFailClosed()
    {
        Assert.False(C0803GymMembershipDomainRules.Evaluate(null, Membership(), Request()).CandidateForBackendRevalidation);
        Assert.False(C0803GymMembershipDomainRules.Evaluate(Actor(), null, Request()).CandidateForBackendRevalidation);
        Assert.False(C0803GymMembershipDomainRules.Evaluate(Actor(), Membership(), null).CandidateForBackendRevalidation);
        Assert.Equal("VERIFIED_SERVER_SNAPSHOTS_REQUIRED",
            C0803GymMembershipDomainRules.Evaluate(Actor() with { ActorId = 0 }, Membership(), Request()).Code);
    }

    [Fact]
    public void TenantBAndBranchBAndForeignMemberOrPlanCannotAccessA()
    {
        var original = Actor();
        var member = Membership();
        var request = Request();

        Assert.Equal("TENANT_BRANCH_MEMBER_PLAN_MISMATCH",
            C0803GymMembershipDomainRules.Evaluate(original with { TenantEmpresaId = 22 }, member, request).Code);
        Assert.Equal("TENANT_BRANCH_MEMBER_PLAN_MISMATCH",
            C0803GymMembershipDomainRules.Evaluate(original, member, request with { RequestedTenantId = 22 }).Code);
        Assert.Equal("TENANT_BRANCH_MEMBER_PLAN_MISMATCH",
            C0803GymMembershipDomainRules.Evaluate(original, member with { BranchId = 222 }, request).Code);
        Assert.Equal("TENANT_BRANCH_MEMBER_PLAN_MISMATCH",
            C0803GymMembershipDomainRules.Evaluate(original, member, request with { MemberId = 802 }).Code);
        Assert.Equal("TENANT_BRANCH_MEMBER_PLAN_MISMATCH",
            C0803GymMembershipDomainRules.Evaluate(original, member, request with { PlanId = 402 }).Code);
    }

    [Fact]
    public void RevokedJwtOrRelationalPermissionAlwaysDenied()
    {
        var a = Actor();
        var m = Membership();
        var q = Request();
        foreach (var denied in new[]
        {
            a with { JwtVerified = false },
            a with { SaaSMembershipActive = false },
            a with { GymPermissionGrantedByServer = false },
            a with { PermissionRevoked = true }
        })
        {
            var decision = C0803GymMembershipDomainRules.Evaluate(denied, m, q);
            Assert.Equal("JWT_MEMBERSHIP_RBAC_NOT_AUTHORIZED", decision.Code);
            Assert.False(decision.CandidateForBackendRevalidation);
        }
    }

    [Theory]
    [InlineData(C0803GymMembershipDomainRules.MembershipState.Draft)]
    [InlineData(C0803GymMembershipDomainRules.MembershipState.Pending)]
    [InlineData(C0803GymMembershipDomainRules.MembershipState.Suspended)]
    [InlineData(C0803GymMembershipDomainRules.MembershipState.Cancelled)]
    [InlineData(C0803GymMembershipDomainRules.MembershipState.Expired)]
    [InlineData((C0803GymMembershipDomainRules.MembershipState)99)]
    public void NonActiveOrUnknownStatesCannotAuthorize(
        C0803GymMembershipDomainRules.MembershipState state)
    {
        Assert.Equal("MEMBERSHIP_NOT_ACTIVE",
            C0803GymMembershipDomainRules.Evaluate(Actor(), Membership() with { State = state }, Request()).Code);
    }

    [Fact]
    public void ExpirationIsExclusiveAndStartIsInclusive()
    {
        var member = Membership();
        var beforeStart = Request() with { ServerNowUtc = member.StartsAtUtc.AddTicks(-1) };
        Assert.Equal("MEMBERSHIP_EXPIRED_OR_NOT_STARTED",
            C0803GymMembershipDomainRules.Evaluate(Actor(), member, beforeStart).Code);
        Assert.True(C0803GymMembershipDomainRules.Evaluate(
            Actor(), member, Request() with { ServerNowUtc = member.StartsAtUtc }).CandidateForBackendRevalidation);
        Assert.True(C0803GymMembershipDomainRules.Evaluate(
            Actor(), member, Request() with { ServerNowUtc = member.EndsAtUtc.AddTicks(-1) }).CandidateForBackendRevalidation);
        Assert.Equal("MEMBERSHIP_EXPIRED_OR_NOT_STARTED",
            C0803GymMembershipDomainRules.Evaluate(
                Actor(), member, Request() with { ServerNowUtc = member.EndsAtUtc }).Code);
    }

    [Fact]
    public void AmbiguousOrNonUtcDatesAndInvalidIntervalsDenied()
    {
        var member = Membership();
        Assert.Equal("UTC_PERIOD_NOT_VERIFIED",
            C0803GymMembershipDomainRules.Evaluate(Actor(), member,
                Request() with { ServerNowUtc = DateTime.SpecifyKind(Now, DateTimeKind.Unspecified) }).Code);
        Assert.Equal("UTC_PERIOD_NOT_VERIFIED",
            C0803GymMembershipDomainRules.Evaluate(Actor(),
                member with { StartsAtUtc = DateTime.SpecifyKind(member.StartsAtUtc, DateTimeKind.Local) },
                Request()).Code);
        Assert.Equal("UTC_PERIOD_NOT_VERIFIED",
            C0803GymMembershipDomainRules.Evaluate(Actor(),
                member with { EndsAtUtc = member.StartsAtUtc }, Request()).Code);
    }

    [Fact]
    public void StaleVersionAndDisabledPlanSuspensionAndRevokedCredentialDenied()
    {
        Assert.Equal("VERSION_STALE_OR_INVALID",
            C0803GymMembershipDomainRules.Evaluate(Actor(), Membership(), Request() with { ExpectedVersion = 6 }).Code);
        foreach (var member in new[]
        {
            Membership() with { PlanActive = false },
            Membership() with { SuspensionActive = true },
            Membership() with { CredentialRevoked = true }
        })
        {
            Assert.Equal("PLAN_SUSPENSION_OR_CREDENTIAL_REVOKED",
                C0803GymMembershipDomainRules.Evaluate(Actor(), member, Request()).Code);
        }
    }

    [Fact]
    public void OfflineDeviceClientPaidClaimOrMissingErpProofFailClosed()
    {
        Assert.Equal("TRUSTED_ERP_AND_ONLINE_REVALIDATION_REQUIRED",
            C0803GymMembershipDomainRules.Evaluate(
                Actor(), Membership(), Request() with { DeviceOffline = true }).Code);
        Assert.Equal("TRUSTED_ERP_AND_ONLINE_REVALIDATION_REQUIRED",
            C0803GymMembershipDomainRules.Evaluate(
                Actor(), Membership(), Request() with { ClientClaimsPaid = true }).Code);
        Assert.Equal("TRUSTED_ERP_AND_ONLINE_REVALIDATION_REQUIRED",
            C0803GymMembershipDomainRules.Evaluate(
                Actor(), Membership() with { ErpPaymentReconciled = false }, Request()).Code);
    }
}
