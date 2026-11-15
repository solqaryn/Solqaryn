using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5VisualConsistencyApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly V5VisualConsistencyApplicationService Service = new();
    private static readonly IReadOnlySet<string> ServerSurfaces =
        new HashSet<string>(StringComparer.Ordinal) { "private-shell", "public-landing" };

    private static V5VisualConsistencyDomainRules.VerifiedContext Trusted() =>
        new(7, 11, true, true, Sha);

    private static V5VisualConsistencyDomainRules.Observation Evidence() =>
        new("private-shell", "ERROR", 11, 390, Sha, "ci:chromium:run-123",
            true, true, true, false, 4.8m, 4.5m);

    private static V5VisualConsistencyApplicationService.Request Request(
        params V5VisualConsistencyDomainRules.Observation?[] observations) =>
        new(11, observations.Length == 0 ? new[] { Evidence() } : observations);

    [Fact]
    public void AcceptedObservationsStillRequireExternalBrowserAndIndependentA5()
    {
        var result = Service.Prepare(Trusted(),
            Request(Evidence(), Evidence() with { SurfaceKey = "public-landing" }),
            ServerSurfaces);
        Assert.Equal("EXTERNAL_BROWSER_AND_A5_REQUIRED", result.Code);
        Assert.Equal(2, result.ItemsChecked);
        Assert.True(result.DomainPreflightAccepted);
        Assert.False(result.CertifiesAccessibility);
        Assert.False(result.AuthorizesTenantAccess);
        Assert.False(result.AuthorizesRelease);
        Assert.False(result.PersistsEvidence);
    }

    [Fact]
    public void NullRevokedAndUnprivilegedBackendAuthorityAreRejected()
    {
        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            Service.Prepare(null, Request(), ServerSurfaces).Code);
        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            Service.Prepare(Trusted() with { MembershipCurrent = false },
                Request(), ServerSurfaces).Code);
        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            Service.Prepare(Trusted() with { ExplicitReviewPermission = false },
                Request(), ServerSurfaces).Code);
    }

    [Fact]
    public void ClientTenantClaimCannotSelectDifferentTenant()
    {
        var request = Request() with { ClientRequestedTenantId = 22 };
        Assert.Equal("CROSS_TENANT_REQUEST_DENIED",
            Service.Prepare(Trusted(), request, ServerSurfaces).Code);
        Assert.Equal("CROSS_TENANT_REQUEST_DENIED",
            Service.Prepare(Trusted(), null, ServerSurfaces).Code);
    }

    [Fact]
    public void MissingOrEmptyTrustedCatalogIsNotABypass()
    {
        Assert.Equal("SERVER_APPROVED_SURFACE_CATALOG_REQUIRED",
            Service.Prepare(Trusted(), Request(), null).Code);
        Assert.Equal("SERVER_APPROVED_SURFACE_CATALOG_REQUIRED",
            Service.Prepare(Trusted(), Request(), new HashSet<string>()).Code);
    }

    [Fact]
    public void UnboundedEmptyOrMissingObservationBatchIsDenied()
    {
        Assert.Equal("OBSERVATION_CAPACITY_INVALID",
            Service.Prepare(Trusted(), new(11, null), ServerSurfaces).Code);
        Assert.Equal("OBSERVATION_CAPACITY_INVALID",
            Service.Prepare(Trusted(), new(11,
                Array.Empty<V5VisualConsistencyDomainRules.Observation?>()), ServerSurfaces).Code);
        Assert.Equal("OBSERVATION_CAPACITY_INVALID",
            Service.Prepare(Trusted(), Request() with { MaximumObservations = 0 },
                ServerSurfaces).Code);
        Assert.Equal("OBSERVATION_CAPACITY_INVALID",
            Service.Prepare(Trusted(), Request() with { MaximumObservations = 65 },
                ServerSurfaces).Code);
        Assert.Equal("OBSERVATION_CAPACITY_INVALID",
            Service.Prepare(Trusted(), Request(Evidence(), Evidence()) with
                { MaximumObservations = 1 }, ServerSurfaces).Code);
    }

    [Fact]
    public void EarlyNullOrInvalidEvidenceCountsOnlyVisitedItems()
    {
        var bad = Service.Prepare(Trusted(), Request(Evidence(), null, Evidence()), ServerSurfaces);
        Assert.Equal("NULL_VISUAL_OBSERVATION", bad.Code);
        Assert.Equal(2, bad.ItemsChecked);
        Assert.False(bad.DomainPreflightAccepted);
        var foreign = Service.Prepare(Trusted(),
            Request(Evidence(), Evidence() with { TenantId = 22 }), ServerSurfaces);
        Assert.Equal("CROSS_TENANT_VISUAL_EVIDENCE_DENIED", foreign.Code);
        Assert.Equal(2, foreign.ItemsChecked);
    }

    [Fact]
    public void SourceShaAndServerApprovedSurfaceRemainRequired()
    {
        Assert.Equal("EXACT_SOURCE_SHA_REQUIRED",
            Service.Prepare(Trusted(), Request(Evidence() with { SourceSha = new string('f', 40) }),
                ServerSurfaces).Code);
        Assert.Equal("UNKNOWN_OR_UNSAFE_SURFACE",
            Service.Prepare(Trusted(), Request(Evidence() with { SurfaceKey = "tenant-B" }),
                ServerSurfaces).Code);
    }

    [Fact]
    public void AccessibilityCluesAreNotGrantedByTheApplicationWrapper()
    {
        Assert.Equal("INTERACTION_NOT_REVIEWABLE",
            Service.Prepare(Trusted(), Request(Evidence() with { KeyboardReachable = false }),
                ServerSurfaces).Code);
        Assert.Equal("TEXT_STATUS_REQUIRED",
            Service.Prepare(Trusted(), Request(Evidence() with { TextFeedback = false }),
                ServerSurfaces).Code);
        Assert.Equal("CONTRAST_EVIDENCE_INSUFFICIENT",
            Service.Prepare(Trusted(), Request(Evidence() with { ContrastRatio = 1.0m }),
                ServerSurfaces).Code);
    }

    [Fact]
    public void SensitiveDataOrDangerousReferenceIsRejected()
    {
        Assert.Equal("SENSITIVE_DATA_IN_EVIDENCE",
            Service.Prepare(Trusted(), Request(Evidence() with { ContainsSensitiveData = true }),
                ServerSurfaces).Code);
        Assert.Equal("EVIDENCE_MISSING_OR_UNSAFE",
            Service.Prepare(Trusted(), Request(Evidence() with { EvidenceReference = "secret\n" }),
                ServerSurfaces).Code);
    }
}
