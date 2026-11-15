using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5UiLightnessDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class V5UiLightnessDomainRulesTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static Rules.TrustedRequestContext Request() => new(
        7, 7, true, true, true,
        true, true, false, false,
        false, false, false, "revision-3", "revision-3");

    private static Rules.TrustedBuildEvidence Evidence() => new(
        590 * 1024, 12 * 1024, Sha, Sha, true, false);

    private static string Code(Rules.TrustedRequestContext? request = null,
        Rules.TrustedBuildEvidence? evidence = null) =>
        Rules.Evaluate(request ?? Request(), evidence ?? Evidence()).Code;

    [Fact]
    public void ValidSnapshotStillRequiresRealBackendAndBrowserChecks()
    {
        var verdict = Rules.Evaluate(Request(), Evidence());
        Assert.Equal("EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED", verdict.Code);
        Assert.True(verdict.NeedsIndependentRuntimeVerification);
    }

    [Fact]
    public void MissingIdentityRoleOrMembershipIsDenied()
    {
        Assert.Equal("BACKEND_AUTHORITY_REQUIRED",
            Rules.Evaluate(null, Evidence()).Code);
        Assert.Equal("BACKEND_AUTHORITY_REQUIRED",
            Code(Request() with { CurrentMembershipVerified = false }));
        Assert.Equal("BACKEND_AUTHORITY_REQUIRED",
            Code(Request() with { ActionPermissionVerified = false }));
    }

    [Fact]
    public void TenantBAndSharedOrRevokedCachesAreDenied()
    {
        Assert.Equal("TENANT_CACHE_ISOLATION_REQUIRED",
            Code(Request() with { PersistedResourceTenantId = 8 }));
        Assert.Equal("TENANT_CACHE_ISOLATION_REQUIRED",
            Code(Request() with { TenantScopedCacheKey = false }));
        Assert.Equal("TENANT_CACHE_ISOLATION_REQUIRED",
            Code(Request() with { CacheInvalidatedForRevocation = false }));
    }

    [Fact]
    public void ClientFlagsPrivatePrefetchAndUnauthorizedResponseMaskingAreDenied()
    {
        Assert.Equal("CLIENT_AUTHORITY_OVERRIDE_DENIED",
            Code(Request() with { ClientSuppliedTenantOrRole = true }));
        Assert.Equal("PRIVATE_PRELOAD_DENIED",
            Code(Request() with { PrivateResourcePreloadRequested = true }));
        Assert.Equal("SECURITY_OR_PRIVACY_REGRESSION_DENIED",
            Code(Request() with { SuppressesUnauthorizedResponse = true }));
        Assert.Equal("SECURITY_OR_PRIVACY_REGRESSION_DENIED",
            Code(Request() with { ContainsJwtOrPersonalTelemetry = true }));
    }

    [Fact]
    public void StaleResponseCannotBeServedFromAnOldTenantSession()
    {
        Assert.Equal("STALE_RESPONSE_REVISION_DENIED",
            Code(Request() with { ExpectedResourceRevision = "revision-4" }));
        Assert.Equal("STALE_RESPONSE_REVISION_DENIED",
            Code(Request() with { ResourceRevision = "" }));
    }

    [Fact]
    public void UnverifiedOrMismatchedShaMeasurementsAreNeverPass()
    {
        Assert.Equal("VERIFIED_BUNDLE_MEASUREMENT_REQUIRED",
            Rules.Evaluate(Request(), null).Code);
        Assert.Equal("VERIFIED_BUNDLE_MEASUREMENT_REQUIRED",
            Code(evidence: Evidence() with { ProducedByVerifiedBuildArtifact = false }));
        Assert.Equal("VERIFIED_BUNDLE_MEASUREMENT_REQUIRED",
            Code(evidence: Evidence() with { MeasuredCommitSha = new string('b', 40) }));
    }

    [Fact]
    public void BudgetErrorsAndWarningsAreDistinctFromPassingEvidence()
    {
        Assert.Equal("BUNDLE_BUDGET_EXCEEDED",
            Code(evidence: Evidence() with { InitialBundleBytes = Rules.InitialErrorBytes + 1 }));
        Assert.Equal("BUNDLE_BUDGET_EXCEEDED",
            Code(evidence: Evidence() with { LargestComponentStyleBytes = Rules.StyleErrorBytes + 1 }));
        Assert.Equal("BUNDLE_BUDGET_WARNING_REVIEW_REQUIRED",
            Code(evidence: Evidence() with { InitialBundleBytes = Rules.InitialWarningBytes + 1 }));
        Assert.Equal("BUNDLE_BUDGET_WARNING_REVIEW_REQUIRED",
            Code(evidence: Evidence() with { LargestComponentStyleBytes = Rules.StyleWarningBytes + 1 }));
    }

    [Fact]
    public void PolicyBoundariesCannotBeMistakenForObservedCwvMeasurements()
    {
        var observed = Evidence() with
        {
            InitialBundleBytes = Rules.InitialWarningBytes,
            LargestComponentStyleBytes = Rules.StyleWarningBytes,
            BrowserCwvEvidenceAvailable = false
        };
        Assert.Equal("EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED",
            Code(evidence: observed));
    }
}
