using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class C0601M0301LandingDomainRulesTests
{
    private static readonly DateTime UtcNow = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    private static C0601PublicContentSnapshot Snapshot() => new(
        "/propuesta", "sitio.example.test", "propuesta-v1", 7,
        UtcNow.AddDays(-1), UtcNow.AddDays(1),
        true, true, true, false, false, false, false, false);

    private static C0601PublicReadContext Context() =>
        new("/propuesta", "sitio.example.test", UtcNow, true);

    private static C0601PublicContentDecision Check(
        C0601PublicContentSnapshot? publication = null,
        C0601PublicReadContext? context = null) =>
        C0601LandingDomainRules.Evaluate(publication ?? Snapshot(), context ?? Context());

    [Fact]
    public void ApprovedVersionedSafeSnapshotIsOnlyCandidateNotAuthority()
    {
        var result = Check();
        Assert.Equal("CANDIDATE_ONLY_NO_PUBLICATION_GRANT", result.Code);
        Assert.True(result.IsCandidateForFurtherReview);
        Assert.False(result.AuthorizesPublication);
        Assert.False(result.AuthorizesPrivateAccess);
        Assert.False(result.AuthorizesPayment);
        Assert.False(result.CertifiesHttpOrMysql);
    }

    [Fact]
    public void MissingSnapshotOrUntrustedServerCannotCreateCandidate()
    {
        Assert.Equal("TRUSTED_SERVER_SOURCE_REQUIRED",
            C0601LandingDomainRules.Evaluate(null, Context()).Code);
        Assert.Equal("TRUSTED_SERVER_SOURCE_REQUIRED",
            C0601LandingDomainRules.Evaluate(Snapshot(), null).Code);
        Assert.Equal("TRUSTED_SERVER_SOURCE_REQUIRED",
            Check(context: Context() with { ServerSourceVerified = false }).Code);
    }

    [Theory]
    [InlineData("/login")]
    [InlineData("/dashboard")]
    [InlineData("/api/publico")]
    [InlineData("/tienda")]
    [InlineData("/plataforma")]
    [InlineData("/facturas")]
    [InlineData("/admin")]
    [InlineData("/erp")]
    public void ReservedPrivateRoutesNeverBecomePublicCandidates(string route)
    {
        Assert.Equal("CANONICAL_ROUTE_HOST_MISMATCH",
            Check(publication: Snapshot() with { RouteKey = route },
                context: Context() with { ServerRouteKey = route }).Code);
    }

    [Theory]
    [InlineData("/Propuesta")]
    [InlineData("/propuesta?tenant=2")]
    [InlineData("//propuesta")]
    [InlineData("/../admin")]
    [InlineData("/propuesta/%2F")]
    [InlineData("/propuesta/../../factura")]
    [InlineData("/propuesta\r\nother")]
    public void UnsafeNonCanonicalRoutesAreRejected(string route)
    {
        Assert.Equal("CANONICAL_ROUTE_HOST_MISMATCH",
            Check(publication: Snapshot() with { RouteKey = route },
                context: Context() with { ServerRouteKey = route }).Code);
    }

    [Theory]
    [InlineData("SITIO.EXAMPLE.TEST")]
    [InlineData("sitio.example.test:443")]
    [InlineData("sitio..example.test")]
    [InlineData("sitio.example.test/")]
    [InlineData("-sitio.example.test")]
    [InlineData("sitio-.example.test")]
    [InlineData("sítio.example.test")]
    public void UnverifiedOrNonCanonicalHostsAreRejected(string host)
    {
        Assert.Equal("CANONICAL_ROUTE_HOST_MISMATCH",
            Check(publication: Snapshot() with { CanonicalHost = host },
                context: Context() with { ServerCanonicalHost = host }).Code);
    }

    [Fact]
    public void SpoofedHostAndRouteCannotMatchTrustedPlatformSource()
    {
        Assert.Equal("CANONICAL_ROUTE_HOST_MISMATCH",
            Check(context: Context() with { ServerCanonicalHost = "tenant-b.example.test" }).Code);
        Assert.Equal("CANONICAL_ROUTE_HOST_MISMATCH",
            Check(context: Context() with { ServerRouteKey = "/otra" }).Code);
    }

    [Fact]
    public void TenantDataPriceLegalDraftAndMarkupAlwaysFailClosed()
    {
        foreach (var input in new[]
        {
            Snapshot() with { ContainsTenantData = true },
            Snapshot() with { ContainsUnapprovedPrice = true },
            Snapshot() with { ContainsUnapprovedLegalText = true },
            Snapshot() with { ContainsExecutableMarkup = true }
        })
            Assert.Equal("UNSAFE_OR_UNAPPROVED_CONTENT", Check(publication: input).Code);
    }

    [Fact]
    public void SourceApprovalAndPublicationStateAreMandatory()
    {
        foreach (var input in new[]
        {
            Snapshot() with { PlatformApproved = false },
            Snapshot() with { SourceVersionVerified = false },
            Snapshot() with { Active = false },
            Snapshot() with { Draft = true }
        })
            Assert.Equal("PLATFORM_APPROVAL_REQUIRED", Check(publication: input).Code);
    }

    [Theory]
    [InlineData(0, "propuesta-v1")]
    [InlineData(-1, "propuesta-v1")]
    [InlineData(1, "../tenant-b")]
    [InlineData(1, "propuesta\nclave")]
    [InlineData(1, "PLATFORM_PLAN")]
    public void RevisionAndContentKeyMustBeCanonical(long revision, string key)
    {
        Assert.Equal("VERSIONED_CONTENT_KEY_REQUIRED",
            Check(publication: Snapshot() with { Revision = revision, ContentKey = key }).Code);
    }

    [Fact]
    public void TooLongContentKeyCannotBeEnumerated()
    {
        Assert.Equal("VERSIONED_CONTENT_KEY_REQUIRED",
            Check(publication: Snapshot() with { ContentKey = new string('x', 81) }).Code);
    }

    [Fact]
    public void TimeBoundsAndExpiredEvidenceRejectEvenIfPreviouslyApproved()
    {
        var snapshot = Snapshot();
        Assert.Equal("PUBLICATION_WINDOW_INVALID",
            Check(publication: snapshot with { ValidUntilUtc = UtcNow }).Code);
        Assert.Equal("PUBLICATION_WINDOW_INVALID",
            Check(publication: snapshot with { ValidFromUtc = UtcNow.AddSeconds(1) }).Code);
        Assert.Equal("PUBLICATION_WINDOW_INVALID",
            Check(publication: snapshot with { ValidUntilUtc = snapshot.ValidFromUtc }).Code);
        Assert.True(Check(context: Context() with { NowUtc = snapshot.ValidFromUtc })
            .IsCandidateForFurtherReview);
    }

    [Fact]
    public void NonUtcEvidenceAndTimeAreRejected()
    {
        Assert.Equal("UTC_EVIDENCE_REQUIRED",
            Check(context: Context() with { NowUtc = DateTime.SpecifyKind(UtcNow, DateTimeKind.Local) }).Code);
        Assert.Equal("UTC_EVIDENCE_REQUIRED",
            Check(publication: Snapshot() with
            {
                ValidFromUtc = DateTime.SpecifyKind(UtcNow.AddDays(-1), DateTimeKind.Unspecified)
            }).Code);
    }
}
