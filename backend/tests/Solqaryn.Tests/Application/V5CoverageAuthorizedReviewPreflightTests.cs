using System.Security.Claims;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5CoverageAuthorizedReviewPreflightTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly V5CoverageAuthorizedReviewPreflight Service = new();

    private static ClaimsPrincipal Principal(string subject = "user-A", string tenant = "tenant-A") =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim("tenant_id", tenant)
        }, "verified-test-context"));

    private static V5CoverageAuthorizedReviewPreflight.Context Context() =>
        new(Principal(), "user-A", "tenant-A", true,
            new HashSet<string>(StringComparer.Ordinal) { "coverage.review" },
            true, Sha, Sha);

    private static V5CoverageApplicationService.Request Coverage(bool complete = true) =>
        new(Sha, Sha, "V5 FINAL",
            new V5CoverageDomainValidator.SourceSnapshot("V5 FINAL",
                new HashSet<string> { "REQ-A", "REQ-B" },
                new HashSet<string> { "C15-01.M03.01", "C15-01.M03.02" }),
            new HashSet<string> { "REQ-A", "REQ-B" },
            new HashSet<string> { "C15-01.M03.01", "C15-01.M03.02" },
            complete ? new[]
            {
                new V5RequirementCoverageRules.Entry("REQ-A", "C15-01.M03.01",
                    "V5 FINAL", "docs:source", "xunit:case", "git:blob"),
                new V5RequirementCoverageRules.Entry("REQ-B", "C15-01.M03.02",
                    "V5 FINAL", "docs:source", "xunit:case", "git:blob")
            } : new[]
            {
                new V5RequirementCoverageRules.Entry("REQ-A", "C15-01.M03.01",
                    "V5 FINAL", "docs:source", "xunit:case", "git:blob")
            });

    private static V5CoverageAuthorizedReviewPreflight.ReviewRequest Request(bool complete = true) =>
        new("user-A", "tenant-A", Coverage(complete));

    [Fact]
    public void InternallyConsistentCoverageNeverCertifiesRealSourceOrPromotes()
    {
        var result = Service.Prepare(Context(), Request());
        Assert.Equal("INDEPENDENT_VERIFIED_SOURCE_REVIEW_REQUIRED", result.Code);
        Assert.Equal(2, result.MappingsExamined);
        Assert.False(result.AuthorizesInstitutionalWrite);
        Assert.False(result.CertifiesFullCoverage);
        Assert.False(result.GrantsIndependentA5);
        Assert.False(result.AuthorizesEnvironmentPromotion);
        Assert.False(result.GrantsCrossTenantAccess);
    }

    [Fact]
    public void UnauthenticatedOrAbsentBackendContextIsDenied()
    {
        Assert.Equal("BACKEND_AUTHENTICATION_REQUIRED", Service.Prepare(null, Request()).Code);
        Assert.Equal("BACKEND_AUTHENTICATION_REQUIRED",
            Service.Prepare(Context() with { ValidatedPrincipal = new ClaimsPrincipal() }, Request()).Code);
        Assert.Equal("BACKEND_AUTHENTICATION_REQUIRED",
            Service.Prepare(Context() with { BackendSubjectId = null }, Request()).Code);
    }

    [Fact]
    public void SpoofedPrincipalAndCrossTenantRequestAreDenied()
    {
        Assert.Equal("BACKEND_IDENTITY_TENANT_MISMATCH",
            Service.Prepare(Context() with { ValidatedPrincipal = Principal(tenant: "tenant-B") }, Request()).Code);
        Assert.Equal("BACKEND_IDENTITY_TENANT_MISMATCH",
            Service.Prepare(Context() with { ValidatedPrincipal = Principal(subject: "user-B") }, Request()).Code);
        Assert.Equal("TRUSTED_REVIEW_BOUNDARY_MISMATCH",
            Service.Prepare(Context(), Request() with { RequestedTenantId = "tenant-B" }).Code);
    }

    [Fact]
    public void RevokedMembershipAndMissingRbacFailClosed()
    {
        Assert.Equal("MEMBERSHIP_OR_RBAC_REVIEW_PERMISSION_REQUIRED",
            Service.Prepare(Context() with { BackendMembershipVerified = false }, Request()).Code);
        Assert.Equal("MEMBERSHIP_OR_RBAC_REVIEW_PERMISSION_REQUIRED",
            Service.Prepare(Context() with { EffectivePermissions = new HashSet<string>() }, Request()).Code);
    }

    [Fact]
    public void MissingScopeLockAndStaleShaFailClosed()
    {
        Assert.Equal("DEV_SCOPE_OR_COMMIT_SHA_REQUIRED",
            Service.Prepare(Context() with { DevScopeLockVerified = false }, Request()).Code);
        Assert.Equal("DEV_SCOPE_OR_COMMIT_SHA_REQUIRED",
            Service.Prepare(Context() with { ObservedHeadSha = new string('a', 40) }, Request()).Code);
        Assert.Equal("DEV_SCOPE_OR_COMMIT_SHA_REQUIRED",
            Service.Prepare(Context() with { ExpectedHeadSha = "bad" }, Request()).Code);
    }

    [Fact]
    public void InconsistentRequestHeadCannotPass()
    {
        var mismatched = Request() with
        {
            Coverage = Coverage() with { ObservedHeadSha = new string('b', 40) }
        };
        Assert.Equal("TRUSTED_REVIEW_BOUNDARY_MISMATCH",
            Service.Prepare(Context(), mismatched).Code);
        Assert.Equal("TRUSTED_REVIEW_BOUNDARY_MISMATCH",
            Service.Prepare(Context(), null).Code);
    }

    [Fact]
    public void MissingRequirementNeverBecomesCoverageCertificate()
    {
        var result = Service.Prepare(Context(), Request(complete: false));
        Assert.Equal("REQUIREMENTS_UNMAPPED", result.Code);
        Assert.False(result.CertifiesFullCoverage);
        Assert.False(result.AuthorizesInstitutionalWrite);
    }
}
