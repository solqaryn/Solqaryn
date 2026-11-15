using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5OperationalAuditSecurityGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static V5OperationalAuditSecurityGate.Context Valid() => new(
        PrincipalId: "verified-user",
        ServerResolvedTenantId: "tenant-A",
        MembershipTenantId: "tenant-A",
        TargetResourceTenantId: "tenant-A",
        ClientRequestedTenantId: null,
        EvidenceSha: Sha,
        IsAuthenticated: true,
        TenantResolvedOnServer: true,
        MembershipActive: true,
        HasExplicitAuditPermission: true,
        PermissionRevoked: false,
        IsServiceActor: false,
        ServiceActorGrantVerified: false,
        RequestedQueryFilterBypass: false,
        RequestedInstitutionalWrite: false);

    private static void Denies(
        V5OperationalAuditSecurityGate.Context context, string reason)
    {
        var result = V5OperationalAuditSecurityGate.Evaluate(context);
        Assert.Equal(reason, result.Code);
        Assert.False(result.NeedsExternalAdapterVerification);
        Assert.False(result.AllowsInstitutionalWrite);
    }

    [Fact]
    public void VerifiedSameTenantPreflightStillCannotWriteOrCertify()
    {
        var result = V5OperationalAuditSecurityGate.Evaluate(Valid());
        Assert.Equal("AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED", result.Code);
        Assert.True(result.NeedsExternalAdapterVerification);
        Assert.False(result.AllowsInstitutionalWrite);
    }

    [Fact]
    public void CrossTenantABAndUnownedResourcesAreRejected()
    {
        Denies(Valid() with { TargetResourceTenantId = "tenant-B" },
            "CROSS_TENANT_OR_UNOWNED_RESOURCE_DENIED");
        Denies(Valid() with { TargetResourceTenantId = null },
            "CROSS_TENANT_OR_UNOWNED_RESOURCE_DENIED");
    }

    [Fact]
    public void ClientProvidedTenantCannotReplaceServerAuthority()
    {
        Denies(Valid() with { ClientRequestedTenantId = "tenant-B" },
            "CLIENT_TENANT_OVERRIDE_DENIED");
        var harmlessMatchingHint = V5OperationalAuditSecurityGate.Evaluate(
            Valid() with { ClientRequestedTenantId = "tenant-A" });
        Assert.Equal("AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED",
            harmlessMatchingHint.Code);
        Assert.False(harmlessMatchingHint.AllowsInstitutionalWrite);
    }

    [Fact]
    public void AnonymousAndMissingPrincipalCannotPass()
    {
        Denies(Valid() with { IsAuthenticated = false },
            "AUTHENTICATION_REQUIRED");
        Denies(Valid() with { PrincipalId = null },
            "AUTHENTICATION_REQUIRED");
    }

    [Fact]
    public void MissingServerVerifiedTenantAndLegacyNullAreDenied()
    {
        Denies(Valid() with { TenantResolvedOnServer = false },
            "SERVER_VERIFIED_TENANT_REQUIRED");
        Denies(Valid() with { ServerResolvedTenantId = null },
            "SERVER_VERIFIED_TENANT_REQUIRED");
    }

    [Fact]
    public void TenantMembershipMustMatchTrustedTenant()
    {
        Denies(Valid() with { MembershipTenantId = "tenant-B" },
            "TENANT_MEMBERSHIP_MISMATCH");
        Denies(Valid() with { MembershipTenantId = null },
            "TENANT_MEMBERSHIP_MISMATCH");
    }

    [Fact]
    public void RevokedOrInactiveMembershipFailsClosed()
    {
        Denies(Valid() with { PermissionRevoked = true },
            "REVOKED_OR_INACTIVE_MEMBERSHIP");
        Denies(Valid() with { MembershipActive = false },
            "REVOKED_OR_INACTIVE_MEMBERSHIP");
    }

    [Fact]
    public void ExplicitAuditPermissionIsRequired()
    {
        Denies(Valid() with { HasExplicitAuditPermission = false },
            "EXPLICIT_AUDIT_PERMISSION_REQUIRED");
    }

    [Fact]
    public void ServicePrincipalsRequireSeparateVerifiedGrant()
    {
        Denies(Valid() with { IsServiceActor = true },
            "SERVICE_ACTOR_GRANT_REQUIRED");
        var preflight = V5OperationalAuditSecurityGate.Evaluate(
            Valid() with { IsServiceActor = true, ServiceActorGrantVerified = true });
        Assert.True(preflight.NeedsExternalAdapterVerification);
        Assert.False(preflight.AllowsInstitutionalWrite);
    }

    [Fact]
    public void QueryFilterBypassAndInstitutionalWriteAreAlwaysDenied()
    {
        Denies(Valid() with { RequestedQueryFilterBypass = true },
            "QUERY_FILTER_BYPASS_DENIED");
        Denies(Valid() with { RequestedInstitutionalWrite = true },
            "INSTITUTIONAL_WRITE_OUT_OF_SCOPE");
    }

    [Fact]
    public void ExactShaIsRequired()
    {
        Denies(Valid() with { EvidenceSha = "stale" },
            "EXACT_SHA_REQUIRED");
        Denies(Valid() with { EvidenceSha = null },
            "EXACT_SHA_REQUIRED");
    }

    [Fact]
    public void ApplicationPreflightHonorsSecurityGateWithoutGrantingWrite()
    {
        var service = new V5OperationalAuditApplicationService();
        var source = new V5OperationalAuditDomainRules.SourceSnapshot(
            "revision-one",
            new HashSet<string>(StringComparer.Ordinal) { "C15-05.M03.03" },
            new HashSet<string>(StringComparer.Ordinal) { "author", "reviewer" });
        var request = new V5OperationalAuditApplicationService.Request(
            Sha, Sha, "revision-one", source,
            new[]
            {
                new V5OperationalAuditDomainRules.WorkItem(
                    "C15-05.M03.03", "LISTO", "author", Sha, "reviewer",
                    "github:verified-check", "bitacora:verified-a5")
            },
            new HashSet<string>(StringComparer.Ordinal) { "author", "reviewer" });
        var denied = service.PrepareWithTenantSecurityPreflight(
            request, Valid() with { TargetResourceTenantId = "tenant-B" });
        Assert.Equal("CROSS_TENANT_OR_UNOWNED_RESOURCE_DENIED", denied.Code);
        Assert.False(denied.AllowsInstitutionalWrite);
        var guarded = service.PrepareWithTenantSecurityPreflight(request, Valid());
        Assert.Equal("AUTHENTICATED_EVIDENCE_REQUIRED", guarded.Code);
        Assert.False(guarded.AllowsInstitutionalWrite);
    }
}
