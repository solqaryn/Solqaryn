using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5SecurityAcceptanceApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";
    private static readonly V5SecurityAcceptanceApplicationService Service = new();

    private static V5SecurityAcceptanceDomainRules.VerifiedAuthority Authority() =>
        new(10, 100, 7, Sha, true, true, true, true, true, false, false);

    private static V5SecurityAcceptanceDomainRules.PersistedTarget Target() =>
        new(100, 7, 100, true, true, false, false);

    private static V5SecurityAcceptanceDomainRules.Attempt Attempt() =>
        new(V5SecurityAcceptanceDomainRules.Surface.BusinessRead,
            null, Sha, false, false, false);

    private static V5SecurityAcceptanceApplicationService.Request Valid() =>
        new(Sha, Sha, "git-proof:security-dev", Authority(), Target(), Attempt());

    [Fact]
    public void ConsistentSnapshotIsOnlyReadyForExternalTrustedIntegration()
    {
        var result = Service.Prepare(Valid());
        Assert.Equal("TRUSTED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsTrustedAdapter);
        Assert.False(result.AllowsResourceAccess);
        Assert.False(result.AllowsRelease);
        Assert.False(result.AllowsInstitutionalWrite);
    }

    [Fact]
    public void NullAndMismatchedSourceShaNeverPass()
    {
        Assert.Equal("SECURITY_ACCEPTANCE_REQUEST_REQUIRED",
            Service.Prepare(null).Code);
        Assert.Equal("SECURITY_ACCEPTANCE_SOURCE_SHA_MISMATCH",
            Service.Prepare(Valid() with { ObservedCommitSha = OtherSha }).Code);
        Assert.Equal("SECURITY_ACCEPTANCE_EVIDENCE_SHA_MISMATCH",
            Service.Prepare(Valid() with
            {
                Authority = Authority() with { EvidenceSha = OtherSha }
            }).Code);
        Assert.Equal("SECURITY_ACCEPTANCE_EVIDENCE_SHA_MISMATCH",
            Service.Prepare(Valid() with
            {
                Attempt = Attempt() with { EvidenceSha = OtherSha }
            }).Code);
    }

    [Fact]
    public void SensitiveAndMissingProofReferenceIsDenied()
    {
        Assert.Equal("SECURITY_ACCEPTANCE_PROOF_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with { SanitizedProofReference = null }).Code);
        Assert.Equal("SECURITY_ACCEPTANCE_PROOF_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with
            {
                SanitizedProofReference = "https://restricted/secret"
            }).Code);
    }

    [Fact]
    public void MissingMembershipOrPermissionFailsClosed()
    {
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Service.Prepare(Valid() with
            {
                Authority = Authority() with { JwtVerified = false }
            }).Code);
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Service.Prepare(Valid() with
            {
                Authority = Authority() with { RequiredPermissionVerified = false }
            }).Code);
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Service.Prepare(Valid() with
            {
                Authority = Authority() with { ActiveMembershipVerified = false }
            }).Code);
    }

    [Fact]
    public void ForeignTenantAndClientSpoofingAlwaysDenied()
    {
        Assert.Equal("BOLA_RESOURCE_TENANT_MISMATCH",
            Service.Prepare(Valid() with
            {
                Target = Target() with { OwnerTenantId = 200 }
            }).Code);
        Assert.Equal("CLIENT_TENANT_SPOOFING_DENIED",
            Service.Prepare(Valid() with
            {
                Attempt = Attempt() with { ClientTenantOverride = 200 }
            }).Code);
    }

    [Fact]
    public void HostBindingAndFileExpiryRemainProtected()
    {
        Assert.Equal("UNTRUSTED_OR_CROSS_TENANT_HOST",
            Service.Prepare(Valid() with
            {
                Attempt = Attempt() with
                {
                    Surface = V5SecurityAcceptanceDomainRules.Surface.HostResolve
                },
                Target = Target() with { BoundHostTenantId = 200 }
            }).Code);
        Assert.Equal("FILE_LINK_EXPIRED",
            Service.Prepare(Valid() with
            {
                Attempt = Attempt() with
                {
                    Surface = V5SecurityAcceptanceDomainRules.Surface.FileDownload
                },
                Target = Target() with { SignedLinkExpired = true }
            }).Code);
    }

    [Fact]
    public void FilterBypassAndJobWithoutGrantCannotProceed()
    {
        Assert.Equal("TENANT_FILTER_OR_ENUMERATION_BYPASS_DENIED",
            Service.Prepare(Valid() with
            {
                Attempt = Attempt() with { RequestsTenantFilterBypass = true }
            }).Code);
        Assert.Equal("JOB_AUTHORITY_OR_IDEMPOTENCY_REQUIRED",
            Service.Prepare(Valid() with
            {
                Attempt = Attempt() with
                {
                    Surface = V5SecurityAcceptanceDomainRules.Surface.BackgroundJob
                }
            }).Code);
    }

    [Fact]
    public void RestoreAlwaysRequiresSeparateSandboxAuthorization()
    {
        var result = Service.Prepare(Valid() with
        {
            Attempt = Attempt() with
            {
                Surface = V5SecurityAcceptanceDomainRules.Surface.BackupRestore
            }
        });
        Assert.Equal("SEPARATE_SANDBOX_RESTORE_AUTHORIZATION_REQUIRED", result.Code);
        Assert.False(result.AllowsRelease);
    }
}
