using Solqaryn.Domain.AutomationV5;
using Xunit;
using Guard = Solqaryn.Domain.AutomationV5.V5GlobalControlPlaneDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class V5GlobalControlPlaneDomainRulesTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

    private static Guard.TrustedSnapshot Authority() => new(
        "actor-17", "tenant-A", "tenant-A",
        true, true, true, false, false,
        true, true, true, "trace-17",
        false, false, null, null, null, false, null);

    private static Guard.ProposedAction Action(
        Guard.Operation operation = Guard.Operation.OwnTenantMetadataRead) =>
        new(operation, null, false, false, false);

    private static string Code(Guard.TrustedSnapshot? authority = null,
        Guard.ProposedAction? request = null) =>
        Guard.Evaluate(authority ?? Authority(), request ?? Action(), Now).Code;

    private static Guard.TrustedSnapshot PlatformAuthority() =>
        Authority() with
        {
            VerifiedTargetTenantId = "tenant-B",
            PlatformIdentityVerified = true,
            PlatformGrantVerified = true
        };

    private static Guard.TrustedSnapshot ExceptionalAuthority() =>
        PlatformAuthority() with
        {
            ExceptionGrantActive = true,
            ExceptionTenantId = "tenant-B",
            ExceptionIssuedUtc = Now.AddMinutes(-2),
            ExceptionExpiresUtc = Now.AddMinutes(5),
            IndependentApprovalVerified = true,
            IndependentApproverActorId = "reviewer-33"
        };

    private static Guard.ProposedAction ExceptionAction() =>
        Action(Guard.Operation.ExceptionalTenantMetadataRead) with
        {
            Justification = "Review a scoped incident without data export"
        };

    [Fact]
    public void ScopedTenantReadIsOnlyANonAuthorizingPrecondition()
    {
        var verdict = Guard.Evaluate(Authority(), Action(), Now);
        Assert.Equal("TRUSTED_RUNTIME_ENFORCEMENT_REQUIRED", verdict.Code);
        Assert.True(verdict.RequiresBackendEnforcement);
    }

    [Fact]
    public void MissingIdentityRoleMfaAndReauthAreDenied()
    {
        Assert.Equal("SERVER_IDENTITY_AND_GRANT_REQUIRED",
            Guard.Evaluate(null, Action(), Now).Code);
        Assert.Equal("SERVER_IDENTITY_AND_GRANT_REQUIRED",
            Code(Authority() with { CurrentRoleGrantVerified = false }));
        Assert.Equal("MFA_AND_REAUTHENTICATION_REQUIRED",
            Code(Authority() with { RecentMfaVerified = false }));
        Assert.Equal("MFA_AND_REAUTHENTICATION_REQUIRED",
            Code(Authority() with { SensitiveReauthenticationVerified = false }));
    }

    [Fact]
    public void TenantBCannotBorrowTenantARoleOrMembership()
    {
        Assert.Equal("TENANT_OWNERSHIP_REQUIRED",
            Code(Authority() with { VerifiedTargetTenantId = "tenant-B" }));
        Assert.Equal("TENANT_OWNERSHIP_REQUIRED",
            Code(Authority() with { CurrentMembershipVerified = false }));
        Assert.Equal("VERIFIED_TARGET_TENANT_REQUIRED",
            Code(Authority() with { VerifiedTargetTenantId = null }));
    }

    [Fact]
    public void ClientFlagsMutationAndSensitiveReadsNeverAuthorize()
    {
        Assert.Equal("CLIENT_AUTHORITY_OVERRIDE_DENIED",
            Code(request: Action() with { ClientSuppliedAuthority = true }));
        Assert.Equal("UNSAFE_CONTROL_PLANE_OPERATION_DENIED",
            Code(request: Action() with { AttemptsWriteOrExport = true }));
        Assert.Equal("UNSAFE_CONTROL_PLANE_OPERATION_DENIED",
            Code(request: Action() with { IncludesSensitiveData = true }));
        Assert.Equal("UNKNOWN_CONTROL_PLANE_OPERATION",
            Code(request: Action((Guard.Operation)999)));
    }

    [Fact]
    public void PlatformIdentityMustNotBeInferredFromTenantRole()
    {
        var directory = Action(Guard.Operation.PlatformTenantDirectoryRead);
        Assert.Equal("INDEPENDENT_PLATFORM_GRANT_REQUIRED",
            Code(request: directory));
        Assert.Equal("INDEPENDENT_PLATFORM_GRANT_REQUIRED",
            Code(PlatformAuthority() with { PlatformGrantVerified = false }, directory));
        Assert.Equal("TRUSTED_RUNTIME_ENFORCEMENT_REQUIRED",
            Code(PlatformAuthority(), directory));
    }

    [Fact]
    public void ExceptionalReadRequiresLimitedTenantScopedLeaseAndReason()
    {
        Assert.Equal("TRUSTED_RUNTIME_ENFORCEMENT_REQUIRED",
            Code(ExceptionalAuthority(), ExceptionAction()));
        Assert.Equal("EXCEPTION_GRANT_AND_REASON_REQUIRED",
            Code(ExceptionalAuthority() with { ExceptionGrantRevoked = true }, ExceptionAction()));
        Assert.Equal("EXCEPTION_GRANT_AND_REASON_REQUIRED",
            Code(ExceptionalAuthority() with { ExceptionTenantId = "tenant-A" }, ExceptionAction()));
        Assert.Equal("EXCEPTION_GRANT_AND_REASON_REQUIRED",
            Code(ExceptionalAuthority(), ExceptionAction() with { Justification = "short" }));
    }

    [Fact]
    public void StaleFutureOrOverlongExceptionalLeasesAreDenied()
    {
        Assert.Equal("EXCEPTION_EXPIRED_OR_OVERBROAD",
            Code(ExceptionalAuthority() with { ExceptionExpiresUtc = Now }, ExceptionAction()));
        Assert.Equal("EXCEPTION_EXPIRED_OR_OVERBROAD",
            Code(ExceptionalAuthority() with { ExceptionIssuedUtc = Now.AddSeconds(1) }, ExceptionAction()));
        Assert.Equal("EXCEPTION_EXPIRED_OR_OVERBROAD",
            Code(ExceptionalAuthority() with { ExceptionExpiresUtc = Now.AddMinutes(16) }, ExceptionAction()));
    }

    [Fact]
    public void SelfApprovalAndMissingAuditDoNotPass()
    {
        Assert.Equal("INDEPENDENT_APPROVAL_REQUIRED",
            Code(ExceptionalAuthority() with { IndependentApproverActorId = "actor-17" }, ExceptionAction()));
        Assert.Equal("INDEPENDENT_APPROVAL_REQUIRED",
            Code(ExceptionalAuthority() with { IndependentApprovalVerified = false }, ExceptionAction()));
        Assert.Equal("AUDIT_INTEGRITY_REQUIRED",
            Code(Authority() with { AuditSinkVerified = false }));
        Assert.Equal("AUDIT_INTEGRITY_REQUIRED",
            Code(Authority() with { AuditCorrelationId = "" }));
    }
}
