using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class V5VisualConsistencyDomainRulesTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly IReadOnlySet<string> Screens =
        new HashSet<string>(StringComparer.Ordinal) { "private-shell", "public-landing" };
    private static V5VisualConsistencyDomainRules.VerifiedContext Authority() =>
        new(7, 11, true, true, Sha);
    private static V5VisualConsistencyDomainRules.Observation Evidence() =>
        new("private-shell", "ERROR", 11, 390, Sha, "ci:browser:001",
            true, true, true, false, 4.8m, 4.5m);
    private static V5VisualConsistencyDomainRules.Verdict Check(
        V5VisualConsistencyDomainRules.Observation? o = null,
        V5VisualConsistencyDomainRules.VerifiedContext? a = null) =>
        V5VisualConsistencyDomainRules.Evaluate(a ?? Authority(), o ?? Evidence(), Screens);

    [Fact]
    public void SafeEvidenceStillDoesNotGrantReleaseOrTenantAccess()
    {
        var v = Check();
        Assert.True(v.ReadyForIndependentBrowserReview);
        Assert.False(v.AuthorizesRelease);
        Assert.False(v.CertifiesAccessibility);
        Assert.False(v.AuthorizesTenantAccess);
    }

    [Fact]
    public void TenantBObservationCannotUseTenantAContext() =>
        Assert.Equal("CROSS_TENANT_VISUAL_EVIDENCE_DENIED",
            Check(Evidence() with { TenantId = 22 }).Code);

    [Fact]
    public void RevokedOrUnprivilegedActorIsDenied()
    {
        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            Check(a: Authority() with { MembershipCurrent = false }).Code);
        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            Check(a: Authority() with { ExplicitReviewPermission = false }).Code);
    }

    [Fact]
    public void WrongOrIncompleteSourceShaDenied()
    {
        Assert.Equal("EXACT_SOURCE_SHA_REQUIRED",
            Check(Evidence() with { SourceSha = new string('a', 40) }).Code);
        Assert.Equal("EXACT_SOURCE_SHA_REQUIRED",
            Check(a: Authority() with { SourceSha = "short" }).Code);
    }

    [Fact]
    public void SurfaceInjectionAndUnapprovedScreenDenied()
    {
        Assert.Equal("UNKNOWN_OR_UNSAFE_SURFACE",
            Check(Evidence() with { SurfaceKey = "private-shell\nBearer" }).Code);
        Assert.Equal("UNKNOWN_OR_UNSAFE_SURFACE",
            Check(Evidence() with { SurfaceKey = "tenantB" }).Code);
    }

    [Fact]
    public void UiStateOrViewportOutsideApprovedSetDenied()
    {
        Assert.Equal("UNKNOWN_STATE_OR_VIEWPORT",
            Check(Evidence() with { UiState = "APPROVED" }).Code);
        Assert.Equal("UNKNOWN_STATE_OR_VIEWPORT",
            Check(Evidence() with { ViewportPixels = 600 }).Code);
    }

    [Fact]
    public void PiiOrUnsafeReferenceIsRejected()
    {
        Assert.Equal("EVIDENCE_MISSING_OR_UNSAFE",
            Check(Evidence() with { EvidenceReference = "ci:1\nsecret" }).Code);
        Assert.Equal("SENSITIVE_DATA_IN_EVIDENCE",
            Check(Evidence() with { ContainsSensitiveData = true }).Code);
    }

    [Fact]
    public void KeyboardMotionAndTextAreRequired()
    {
        Assert.Equal("INTERACTION_NOT_REVIEWABLE",
            Check(Evidence() with { KeyboardReachable = false }).Code);
        Assert.Equal("INTERACTION_NOT_REVIEWABLE",
            Check(Evidence() with { ReducedMotionSafe = false }).Code);
        Assert.Equal("TEXT_STATUS_REQUIRED",
            Check(Evidence() with { TextFeedback = false }).Code);
    }

    [Fact]
    public void ContrastBelowPolicyIsNotAccepted()
    {
        Assert.Equal("CONTRAST_EVIDENCE_INSUFFICIENT",
            Check(Evidence() with { ContrastRatio = 2m }).Code);
        Assert.Equal("CONTRAST_EVIDENCE_INSUFFICIENT",
            Check(Evidence() with { RequiredContrastRatio = 0m }).Code);
    }

    [Fact]
    public void MissingAuthorityAndNullCatalogFailClosed()
    {
        var v = V5VisualConsistencyDomainRules.Evaluate(null, Evidence(), Screens);
        Assert.Equal("SERVER_AUTHORITY_REQUIRED", v.Code);
        Assert.Throws<ArgumentNullException>(() =>
            V5VisualConsistencyDomainRules.Evaluate(Authority(), Evidence(), null!));
    }
}
