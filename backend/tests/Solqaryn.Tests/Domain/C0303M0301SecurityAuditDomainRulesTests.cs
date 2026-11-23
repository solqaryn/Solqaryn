using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class C0303M0301SecurityAuditDomainRulesTests
{
    private static C0303SecurityAuditDomainRules.Request TenantA() => new(
        AuthenticatedActorId: 71,
        ServerResolvedTenantId: 10,
        CurrentMembershipTenantId: 10,
        PersistedResourceTenantId: 10,
        MembershipActive: true,
        AuditWriterTrusted: true,
        CurrentAuditPermissionVerified: true,
        PermissionRevoked: false,
        PlatformAuthorityVerified: false,
        ExplicitPlatformAuditGrantVerified: false,
        TransactionalSinkVerified: false,
        ClientTenantOverrideAttempted: false,
        Scope: C0303SecurityAuditDomainRules.Scope.Enterprise,
        Action: C0303SecurityAuditDomainRules.Action.AuthorizationDenied,
        Outcome: C0303SecurityAuditDomainRules.Outcome.Denied,
        CorrelationId: "req-tenant-A.01");

    private static string Code(C0303SecurityAuditDomainRules.Request? r) =>
        C0303SecurityAuditDomainRules.Evaluate(r).Code;

    [Fact]
    public void PositiveTenantAuditCreatesOnlyMinimalCandidateNotAnAuthorizationGrant()
    {
        var before = DateTimeOffset.UtcNow;
        var result = C0303SecurityAuditDomainRules.Evaluate(TenantA());
        Assert.True(result.NeedsAuthenticatedPersistenceAdapter);
        Assert.False(result.AuthorizesAuditWrite);
        Assert.NotNull(result.Candidate);
        Assert.Equal(10, result.Candidate!.TenantId);
        Assert.Equal(71, result.Candidate.ActorId);
        Assert.Equal("req-tenant-A.01", result.Candidate.CorrelationId);
        Assert.Equal(TimeSpan.Zero, result.Candidate.OccurredAtUtc.Offset);
        Assert.True(result.Candidate.OccurredAtUtc >= before);
        Assert.DoesNotContain("Password", result.Candidate.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingActorAndUntrustedWriterFailClosed()
    {
        Assert.Equal("MISSING_AUDIT_REQUEST", Code(null));
        Assert.Equal("TRUSTED_SERVER_ACTOR_REQUIRED", Code(TenantA() with { AuthenticatedActorId = 0 }));
        Assert.Equal("TRUSTED_SERVER_ACTOR_REQUIRED", Code(TenantA() with { AuditWriterTrusted = false }));
    }

    [Fact]
    public void TenantARejectsTenantBResourcesAndStaleMembership()
    {
        Assert.Equal("CROSS_TENANT_RESOURCE_DENIED", Code(TenantA() with { PersistedResourceTenantId = 20 }));
        Assert.Equal("CURRENT_TENANT_MEMBERSHIP_REQUIRED", Code(TenantA() with { CurrentMembershipTenantId = 20 }));
        Assert.Equal("CURRENT_TENANT_MEMBERSHIP_REQUIRED", Code(TenantA() with { MembershipActive = false }));
        Assert.Equal("CURRENT_TENANT_MEMBERSHIP_REQUIRED", Code(TenantA() with { ServerResolvedTenantId = null }));
        Assert.Equal("CROSS_TENANT_RESOURCE_DENIED", Code(TenantA() with { PersistedResourceTenantId = null }));
    }

    [Fact]
    public void RevocationAndHeadersNeverReauthorizeAnAudit()
    {
        Assert.Equal("AUDIT_PERMISSION_REVOKED_OR_MISSING", Code(TenantA() with { PermissionRevoked = true }));
        Assert.Equal("AUDIT_PERMISSION_REVOKED_OR_MISSING", Code(TenantA() with { CurrentAuditPermissionVerified = false }));
        Assert.Equal("CLIENT_TENANT_OVERRIDE_DENIED", Code(TenantA() with { ClientTenantOverrideAttempted = true }));
    }

    [Theory]
    [InlineData("bad\r\nAuthorization: Bearer abc")]
    [InlineData("bearer / secret")]
    [InlineData("")]
    public void RejectsUnsafeCorrelationAsDataNotAuthority(string value) =>
        Assert.Equal("UNSAFE_CORRELATION_REJECTED", Code(TenantA() with { CorrelationId = value }));

    [Fact]
    public void RejectsOverlongCorrelationAndUnknownAction()
    {
        Assert.Equal("UNSAFE_CORRELATION_REJECTED", Code(TenantA() with { CorrelationId = new string('a', 65) }));
        Assert.Equal("UNKNOWN_AUDIT_CODE", Code(TenantA() with { Action = (C0303SecurityAuditDomainRules.Action)987 }));
    }

    [Fact]
    public void DoesNotAcceptMisrepresentedSuccessForDenial()
    {
        Assert.Equal("ACTION_OUTCOME_MISMATCH",
            Code(TenantA() with { Outcome = C0303SecurityAuditDomainRules.Outcome.Allowed }));
        Assert.Equal("ACTION_OUTCOME_MISMATCH",
            Code(TenantA() with {
                Action = C0303SecurityAuditDomainRules.Action.AuthorizationGranted,
                Outcome = C0303SecurityAuditDomainRules.Outcome.Error
            }));
    }

    [Fact]
    public void CriticalChangeRequiresStrictTransactionPrecondition()
    {
        Assert.Equal("STRICT_TRANSACTIONAL_AUDIT_REQUIRED",
            Code(TenantA() with { Action = C0303SecurityAuditDomainRules.Action.CriticalChange }));
        var result = C0303SecurityAuditDomainRules.Evaluate(TenantA() with {
            Action = C0303SecurityAuditDomainRules.Action.CriticalChange,
            TransactionalSinkVerified = true
        });
        Assert.True(result.NeedsAuthenticatedPersistenceAdapter);
        Assert.False(result.AuthorizesAuditWrite);
    }

    [Fact]
    public void PlatformAuditRequiresExplicitSeparateGrantAndNoEnterpriseFallback()
    {
        var request = TenantA() with {
            Scope = C0303SecurityAuditDomainRules.Scope.Platform,
            ServerResolvedTenantId = null,
            CurrentMembershipTenantId = null,
            PersistedResourceTenantId = null
        };
        Assert.Equal("EXPLICIT_PLATFORM_AUDIT_SCOPE_REQUIRED", Code(request));
        var accepted = C0303SecurityAuditDomainRules.Evaluate(request with {
            PlatformAuthorityVerified = true,
            ExplicitPlatformAuditGrantVerified = true
        });
        Assert.True(accepted.NeedsAuthenticatedPersistenceAdapter);
        Assert.Null(accepted.Candidate!.TenantId);
        Assert.False(accepted.AuthorizesAuditWrite);
        Assert.Equal("EXPLICIT_PLATFORM_AUDIT_SCOPE_REQUIRED",
            Code(request with {
                PlatformAuthorityVerified = true,
                ExplicitPlatformAuditGrantVerified = true,
                PersistedResourceTenantId = 20
            }));
    }
}
