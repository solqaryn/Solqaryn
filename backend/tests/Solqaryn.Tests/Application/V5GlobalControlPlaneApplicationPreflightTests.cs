using Solqaryn.Application.AutomationV5;
using Xunit;
using Guard = Solqaryn.Domain.AutomationV5.V5GlobalControlPlaneDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class V5GlobalControlPlaneApplicationPreflightTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 15, 13, 0, 0, TimeSpan.Zero);
    private readonly V5GlobalControlPlaneApplicationPreflight _service = new();

    private static Guard.TrustedSnapshot TenantEvidence() => new(
        "actor-17", "tenant-A", "tenant-A",
        true, true, true, false, false,
        true, true, true, "trace-17",
        false, false, null, null, null, false, null);

    private static V5GlobalControlPlaneApplicationPreflight.ReadIntent Own() =>
        new(Guard.Operation.OwnTenantMetadataRead, "tenant-A",
            null, false, false, false);

    private static Guard.TrustedSnapshot PlatformEvidence() =>
        TenantEvidence() with
        {
            VerifiedTargetTenantId = "tenant-B",
            PlatformIdentityVerified = true,
            PlatformGrantVerified = true
        };

    private static Guard.TrustedSnapshot ExceptionEvidence() =>
        PlatformEvidence() with
        {
            ExceptionGrantActive = true,
            ExceptionTenantId = "tenant-B",
            ExceptionIssuedUtc = Now.AddMinutes(-1),
            ExceptionExpiresUtc = Now.AddMinutes(5),
            IndependentApprovalVerified = true,
            IndependentApproverActorId = "reviewer-33"
        };

    [Fact]
    public void OwnReadPreflightIsNotAnAuthorizationGrant()
    {
        var review = _service.ReviewOnly(TenantEvidence(), Own(), Now);
        Assert.Equal("TRUSTED_RUNTIME_ENFORCEMENT_REQUIRED", review.Code);
        Assert.True(review.RequiresLiveSecurityChecks);
        Assert.False(review.GrantsAccess);
    }

    [Fact]
    public void ForgedTenantAndMissingTrustedEvidenceDeny()
    {
        Assert.Equal("SERVER_VERIFIED_TARGET_MISMATCH",
            _service.ReviewOnly(TenantEvidence(),
                Own() with { RequestedTenantId = "tenant-B" }, Now).Code);
        Assert.Equal("SERVER_VERIFIED_TARGET_MISMATCH",
            _service.ReviewOnly(null, Own(), Now).Code);
        Assert.Equal("SERVER_VERIFIED_TARGET_MISMATCH",
            _service.ReviewOnly(TenantEvidence(),
                Own() with { RequestedTenantId = "" }, Now).Code);
    }

    [Fact]
    public void WrongClockOrMissingIntentNeverProceeds()
    {
        Assert.Equal("SERVER_UTC_CLOCK_REQUIRED",
            _service.ReviewOnly(TenantEvidence(), Own(),
                Now.ToOffset(TimeSpan.FromHours(-6))).Code);
        Assert.Equal("READ_INTENT_REQUIRED",
            _service.ReviewOnly(TenantEvidence(), null, Now).Code);
    }

    [Fact]
    public void UntrustedClientAuthorityMutationAndSensitiveReadDeny()
    {
        Assert.Equal("CLIENT_AUTHORITY_OVERRIDE_DENIED",
            _service.ReviewOnly(TenantEvidence(),
                Own() with { ContainsClientAuthorityHint = true }, Now).Code);
        Assert.Equal("UNSAFE_CONTROL_PLANE_OPERATION_DENIED",
            _service.ReviewOnly(TenantEvidence(),
                Own() with { AttemptsWriteOrExport = true }, Now).Code);
        Assert.Equal("UNSAFE_CONTROL_PLANE_OPERATION_DENIED",
            _service.ReviewOnly(TenantEvidence(),
                Own() with { IncludesSensitiveData = true }, Now).Code);
    }

    [Fact]
    public void MfaReauthenticationAndRoleRemainMandatory()
    {
        Assert.Equal("MFA_AND_REAUTHENTICATION_REQUIRED",
            _service.ReviewOnly(TenantEvidence() with { RecentMfaVerified = false },
                Own(), Now).Code);
        Assert.Equal("MFA_AND_REAUTHENTICATION_REQUIRED",
            _service.ReviewOnly(TenantEvidence() with { SensitiveReauthenticationVerified = false },
                Own(), Now).Code);
        Assert.Equal("SERVER_IDENTITY_AND_GRANT_REQUIRED",
            _service.ReviewOnly(TenantEvidence() with { CurrentRoleGrantVerified = false },
                Own(), Now).Code);
    }

    [Fact]
    public void PlatformDirectoryNeedsSeparatePlatformGrants()
    {
        var request = new V5GlobalControlPlaneApplicationPreflight.ReadIntent(
            Guard.Operation.PlatformTenantDirectoryRead,
            "tenant-B", null, false, false, false);
        Assert.Equal("INDEPENDENT_PLATFORM_GRANT_REQUIRED",
            _service.ReviewOnly(TenantEvidence() with
            {
                VerifiedTargetTenantId = "tenant-B"
            }, request, Now).Code);
        var review = _service.ReviewOnly(PlatformEvidence(), request, Now);
        Assert.True(review.RequiresLiveSecurityChecks);
        Assert.False(review.GrantsAccess);
    }

    [Fact]
    public void ExceptionalReadChecksReasonLeaseAndIndependentApproval()
    {
        var request = new V5GlobalControlPlaneApplicationPreflight.ReadIntent(
            Guard.Operation.ExceptionalTenantMetadataRead,
            "tenant-B", "Investigate a scoped incident without export",
            false, false, false);
        var reviewed = _service.ReviewOnly(ExceptionEvidence(), request, Now);
        Assert.Equal("TRUSTED_RUNTIME_ENFORCEMENT_REQUIRED", reviewed.Code);
        Assert.False(reviewed.GrantsAccess);
        Assert.Equal("EXCEPTION_EXPIRED_OR_OVERBROAD",
            _service.ReviewOnly(ExceptionEvidence() with
                { ExceptionExpiresUtc = Now }, request, Now).Code);
        Assert.Equal("INDEPENDENT_APPROVAL_REQUIRED",
            _service.ReviewOnly(ExceptionEvidence() with
                { IndependentApproverActorId = "actor-17" }, request, Now).Code);
        Assert.Equal("EXCEPTION_GRANT_AND_REASON_REQUIRED",
            _service.ReviewOnly(ExceptionEvidence(),
                request with { Justification = "short" }, Now).Code);
    }

    [Fact]
    public void RevokedExceptionAndUnverifiedAuditAreDenied()
    {
        var request = new V5GlobalControlPlaneApplicationPreflight.ReadIntent(
            Guard.Operation.ExceptionalTenantMetadataRead,
            "tenant-B", "Investigate a scoped incident without export",
            false, false, false);
        Assert.Equal("EXCEPTION_GRANT_AND_REASON_REQUIRED",
            _service.ReviewOnly(ExceptionEvidence() with
                { ExceptionGrantRevoked = true }, request, Now).Code);
        Assert.Equal("AUDIT_INTEGRITY_REQUIRED",
            _service.ReviewOnly(ExceptionEvidence() with
                { AuditSinkVerified = false }, request, Now).Code);
    }

    [Fact]
    public void UnexpectedReasonAndUnknownOperationAreDenied()
    {
        Assert.Equal("UNEXPECTED_JUSTIFICATION_REJECTED",
            _service.ReviewOnly(TenantEvidence(),
                Own() with { Justification = "Client text" }, Now).Code);
        Assert.Equal("UNKNOWN_CONTROL_PLANE_OPERATION",
            _service.ReviewOnly(TenantEvidence(),
                Own() with { Operation = (Guard.Operation)999 }, Now).Code);
    }
}
