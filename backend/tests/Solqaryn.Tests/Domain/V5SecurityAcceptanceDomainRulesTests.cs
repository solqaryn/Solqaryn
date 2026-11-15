using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class V5SecurityAcceptanceDomainRulesTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static V5SecurityAcceptanceDomainRules.VerifiedAuthority Authority() =>
        new(10, 100, 5, Sha, true, true, true, true, true, false, false);

    private static V5SecurityAcceptanceDomainRules.PersistedTarget OwnTarget() =>
        new(100, 5, 100, true, true, false, false);

    private static V5SecurityAcceptanceDomainRules.Attempt Read() =>
        new(V5SecurityAcceptanceDomainRules.Surface.BusinessRead, null, Sha,
            false, false, false);

    private static string Check(
        V5SecurityAcceptanceDomainRules.VerifiedAuthority? authority = null,
        V5SecurityAcceptanceDomainRules.PersistedTarget? target = null,
        V5SecurityAcceptanceDomainRules.Attempt? attempt = null) =>
        V5SecurityAcceptanceDomainRules.Evaluate(
            authority ?? Authority(), target ?? OwnTarget(), attempt ?? Read()).Code;

    [Fact]
    public void OwnResourceIsOnlyReadyForTrustedIntegrationNotAccessOrRelease()
    {
        var verdict = V5SecurityAcceptanceDomainRules.Evaluate(
            Authority(), OwnTarget(), Read());
        Assert.True(verdict.RequiresTrustedIntegration);
        Assert.False(verdict.AuthorizesResourceAccess);
        Assert.False(verdict.AuthorizesRelease);
    }

    [Fact]
    public void RejectsMissingJwtMembershipPermissionAndStaleRole()
    {
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { JwtVerified = false }));
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { ActiveMembershipVerified = false }));
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { RequiredPermissionVerified = false }));
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { PermissionSnapshotCurrent = false }));
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { TenantResolvedOnServer = false }));
    }

    [Fact]
    public void KnownResourceIdFromTenantBDoesNotGrantTenantAAccess()
    {
        Assert.Equal("BOLA_RESOURCE_TENANT_MISMATCH",
            Check(target: OwnTarget() with { OwnerTenantId = 200 }));
        Assert.Equal("BOLA_RESOURCE_TENANT_MISMATCH",
            Check(target: OwnTarget() with { OwnershipVerifiedFromStore = false }));
        Assert.Equal("BOLA_RESOURCE_TENANT_MISMATCH",
            Check(target: OwnTarget() with { OwnerTenantId = null }));
    }

    [Fact]
    public void ClientCannotOverrideTenantOrBypassEfFilters()
    {
        Assert.Equal("CLIENT_TENANT_SPOOFING_DENIED",
            Check(attempt: Read() with { ClientTenantOverride = 200 }));
        Assert.Equal("TENANT_FILTER_OR_ENUMERATION_BYPASS_DENIED",
            Check(attempt: Read() with { RequestsTenantFilterBypass = true }));
        Assert.Equal("TENANT_FILTER_OR_ENUMERATION_BYPASS_DENIED",
            Check(attempt: Read() with { RequestsCrossTenantEnumeration = true }));
    }

    [Fact]
    public void BranchAndRevocationAreFailClosed()
    {
        Assert.Equal("BRANCH_OWNER_MISMATCH",
            Check(attempt: Read() with { Surface = V5SecurityAcceptanceDomainRules.Surface.BranchRead },
                  target: OwnTarget() with { OwnerBranchId = 9 }));
        Assert.Equal("RESOURCE_OR_PERMISSION_REVOKED",
            Check(target: OwnTarget() with { AccessRevoked = true }));
    }

    [Fact]
    public void HostSpoofAndExpiredSignedLinkAreRejected()
    {
        Assert.Equal("UNTRUSTED_OR_CROSS_TENANT_HOST",
            Check(attempt: Read() with { Surface = V5SecurityAcceptanceDomainRules.Surface.HostResolve },
                target: OwnTarget() with { BoundHostTenantId = 200 }));
        Assert.Equal("UNTRUSTED_OR_CROSS_TENANT_HOST",
            Check(attempt: Read() with { Surface = V5SecurityAcceptanceDomainRules.Surface.HostResolve },
                target: OwnTarget() with { HostBindingVerified = false }));
        Assert.Equal("FILE_LINK_EXPIRED",
            Check(attempt: Read() with { Surface = V5SecurityAcceptanceDomainRules.Surface.FileDownload },
                target: OwnTarget() with { SignedLinkExpired = true }));
    }

    [Fact]
    public void JobsNeedServerGrantAndIdempotency()
    {
        var job = Read() with { Surface = V5SecurityAcceptanceDomainRules.Surface.BackgroundJob };
        Assert.Equal("JOB_AUTHORITY_OR_IDEMPOTENCY_REQUIRED", Check(attempt: job));
        Assert.Equal("JOB_AUTHORITY_OR_IDEMPOTENCY_REQUIRED",
            Check(authority: Authority() with { ServiceActorVerified = true }, attempt: job));
        Assert.Equal("READY_FOR_TRUSTED_SECURITY_INTEGRATION",
            Check(authority: Authority() with
                { ServiceActorVerified = true, ServiceGrantVerified = true },
                attempt: job with { IdempotencyEvidenceVerified = true }));
    }

    [Fact]
    public void BackupRestoreNeedsSeparateAuthorization()
    {
        Assert.Equal("SEPARATE_SANDBOX_RESTORE_AUTHORIZATION_REQUIRED",
            Check(attempt: Read() with { Surface = V5SecurityAcceptanceDomainRules.Surface.BackupRestore }));
    }

    [Fact]
    public void StaleShaAndUnknownSurfaceNeverPass()
    {
        Assert.Equal("EXACT_EVIDENCE_SHA_REQUIRED",
            Check(attempt: Read() with { EvidenceSha = new string('b', 40) }));
        Assert.Equal("EXACT_EVIDENCE_SHA_REQUIRED",
            Check(authority: Authority() with { EvidenceSha = "short" }));
        Assert.Equal("UNKNOWN_SECURITY_SURFACE",
            Check(attempt: Read() with { Surface = (V5SecurityAcceptanceDomainRules.Surface)999 }));
    }

    [Fact]
    public void NullInputsAreDeniedWithoutThrowing()
    {
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            V5SecurityAcceptanceDomainRules.Evaluate(null, OwnTarget(), Read()).Code);
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            V5SecurityAcceptanceDomainRules.Evaluate(Authority(), null, Read()).Code);
    }
}
