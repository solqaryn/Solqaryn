using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5UxAuditAuthorizedApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly V5UxAuditAuthorizedApplicationService Service = new();

    private static V5UxAuditAuthorizedApplicationService.TrustedAuthority Authority() =>
        new(21, "tenant-A", Sha, true, true, true, true, true,
            new HashSet<string>(StringComparer.Ordinal) { "portal", "builder" },
            new HashSet<string>(StringComparer.Ordinal) { "p95-latency" });

    private static V5UxAuditApplicationService.Request Request(
        string tenant = "tenant-A", string surface = "portal") =>
        new(Sha, Sha, tenant, true,
            new HashSet<string>(StringComparer.Ordinal) { "portal" },
            new HashSet<string>(StringComparer.Ordinal) { "p95-latency" },
            new[] { new V5UxAuditRules.Observation(surface, "p95-latency",
                tenant, Sha, "ci:verified-example", 90m, 150m, "MAX") });

    [Fact]
    public void TrustedReadOnlyRequestStillRequiresExternalEvidenceAndA5()
    {
        var result = Service.Prepare(Authority(), Request());
        Assert.Equal("EXTERNAL_EVIDENCE_AND_A5_REQUIRED", result.Code);
        Assert.Equal(1, result.ItemsReviewed);
        Assert.False(result.GrantsAccess);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.CertifiesExternalMeasurement);
        Assert.False(result.AuthorizesProduction);
    }

    [Fact]
    public void NullJwtMembershipAndPermissionAlwaysFailClosed()
    {
        Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED",
            Service.Prepare(null, Request()).Code);
        Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED",
            Service.Prepare(Authority(), null).Code);
        Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED",
            Service.Prepare(Authority() with { JwtValidatedByServer = false }, Request()).Code);
        Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED",
            Service.Prepare(Authority() with { CurrentMembershipVerified = false }, Request()).Code);
        Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED",
            Service.Prepare(Authority() with { RequiredReviewPermissionVerified = false }, Request()).Code);
        Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED",
            Service.Prepare(Authority() with { ServerActorId = 0 }, Request()).Code);
    }

    [Fact]
    public void UnverifiedTenantAndCrossTenantAtoBFailClosed()
    {
        Assert.Equal("TRUSTED_TENANT_CONTEXT_MISMATCH",
            Service.Prepare(Authority(), Request(tenant: "tenant-B")).Code);
        Assert.Equal("TRUSTED_TENANT_CONTEXT_MISMATCH",
            Service.Prepare(Authority(), Request() with { TenantContextVerifiedByServer = false }).Code);
        Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED",
            Service.Prepare(Authority() with { TenantResolvedFromServer = false }, Request()).Code);
        Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED",
            Service.Prepare(Authority() with { ExplicitDevScopeVerified = false }, Request()).Code);
    }

    [Fact]
    public void StaleSourceShaAndUntrustedEvidenceAreDenied()
    {
        Assert.Equal("EXACT_TRUSTED_SOURCE_SHA_REQUIRED",
            Service.Prepare(Authority() with { VerifiedSourceSha = "short" }, Request()).Code);
        Assert.Equal("EXACT_TRUSTED_SOURCE_SHA_REQUIRED",
            Service.Prepare(Authority(), Request() with { ExpectedSourceSha = new string('a', 40) }).Code);
        Assert.Equal("EXACT_TRUSTED_SOURCE_SHA_REQUIRED",
            Service.Prepare(Authority(), Request() with { ObservedSourceSha = new string('b', 40) }).Code);
    }

    [Fact]
    public void BrowserCannotExpandTrustedSurfaceOrCriterionCatalog()
    {
        Assert.Equal("SERVER_APPROVED_CATALOG_REQUIRED",
            Service.Prepare(Authority(), Request() with
                { AuthorizedSurfaces = new HashSet<string> { "admin" } }).Code);
        Assert.Equal("SERVER_APPROVED_CATALOG_REQUIRED",
            Service.Prepare(Authority(), Request() with
                { AuthorizedCriteria = new HashSet<string> { "private-metric" } }).Code);
        Assert.Equal("SERVER_APPROVED_CATALOG_REQUIRED",
            Service.Prepare(Authority() with { ApprovedSurfaces = null }, Request()).Code);
    }

    [Fact]
    public void InvalidObservationStillDeniedByDomainRules()
    {
        Assert.Equal("UNKNOWN_SURFACE",
            Service.Prepare(Authority(), Request(surface: "unapproved")).Code);
        Assert.Equal("BUDGET_EXCEEDED",
            Service.Prepare(Authority(), Request() with
            {
                Observations = new[] { new V5UxAuditRules.Observation(
                    "portal", "p95-latency", "tenant-A", Sha, "ci:example",
                    151m, 150m, "MAX") }
            }).Code);
    }
}
