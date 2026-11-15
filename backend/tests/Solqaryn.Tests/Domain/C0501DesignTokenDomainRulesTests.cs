using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class C0501DesignTokenDomainRulesTests
{
    private static readonly IReadOnlySet<string> Keys =
        new HashSet<string>(StringComparer.Ordinal)
        { "primary-color", "spacing-sm", "radius-card", "body-size", "motion-mode" };
    private static readonly string Key = "2c85f621-314a-4f81-88c4-8054e61f812d";
    private static C0501DesignTokenDomainRules.TrustedContext Authority() =>
        new(11, 101, true, true, true, false, 7, 9);
    private static C0501DesignTokenDomainRules.ProposedToken Token() =>
        new(101, 9, 7, "primary-color", "COLOR", "#1A2b3C", Key, false);
    private static C0501DesignTokenDomainRules.Verdict Check(
        C0501DesignTokenDomainRules.TrustedContext? authority = null,
        C0501DesignTokenDomainRules.ProposedToken? token = null) =>
        C0501DesignTokenDomainRules.Evaluate(authority ?? Authority(), token ?? Token(), Keys);

    [Fact]
    public void ValidTokenIsOnlyARequestForIndependentServerReview()
    {
        var v = Check();
        Assert.True(v.ReadyForTrustedApplicationReview);
        Assert.False(v.AuthorizesTenantAccess);
        Assert.False(v.AuthorizesThemeMutation);
        Assert.False(v.CertifiesAccessibility);
        Assert.False(v.AuthorizesRelease);
    }

    [Fact]
    public void CrossTenantAndForeignResourceAreDenied()
    {
        Assert.Equal("CROSS_TENANT_OR_RESOURCE_DENIED", Check(token: Token() with { TenantId = 102 }).Code);
        Assert.Equal("CROSS_TENANT_OR_RESOURCE_DENIED", Check(authority: Authority() with { ResourceOwnedByTenant = false }).Code);
    }

    [Fact]
    public void RevokedOrMissingMembershipIsDenied()
    {
        Assert.Equal("TRUSTED_AUTHORITY_REQUIRED", Check(authority: Authority() with { RoleRevoked = true }).Code);
        Assert.Equal("TRUSTED_AUTHORITY_REQUIRED", Check(authority: Authority() with { MembershipCurrent = false }).Code);
        Assert.Equal("TRUSTED_AUTHORITY_REQUIRED", Check(authority: Authority() with { HasThemeEditPermission = false }).Code);
    }

    [Fact]
    public void InvalidActorsFailClosed()
    {
        Assert.Equal("TRUSTED_AUTHORITY_REQUIRED", Check(authority: Authority() with { ActorId = 0 }).Code);
        Assert.Equal("TRUSTED_AUTHORITY_REQUIRED", Check(authority: Authority() with { TenantId = 0 }).Code);
    }

    [Fact]
    public void PlatformWideMutationsCannotBeRequestedAsTenantOverrides() =>
        Assert.Equal("PLATFORM_CATALOG_MUTATION_DENIED",
            Check(token: Token() with { IsPlatformCatalogMutation = true }).Code);

    [Fact]
    public void ChangingTenantSessionEpochRejectsOldResponses()
    {
        Assert.Equal("STALE_OR_INVALID_SESSION_EPOCH", Check(token: Token() with { SessionEpoch = 8 }).Code);
        Assert.Equal("STALE_OR_INVALID_SESSION_EPOCH", Check(authority: Authority() with { SessionEpoch = 0 }).Code);
    }

    [Fact]
    public void ConcurrencyRequiresExactExpectedRevision()
    {
        Assert.Equal("THEME_REVISION_CONFLICT", Check(token: Token() with { ExpectedRevision = 6 }).Code);
        Assert.Equal("THEME_REVISION_CONFLICT", Check(authority: Authority() with { ActiveRevision = -1 }).Code);
    }

    [Fact]
    public void KeyMustBeInServerAllowlistAndCanonical()
    {
        Assert.Equal("UNKNOWN_OR_UNSAFE_TOKEN", Check(token: Token() with { TokenKey = "not-approved" }).Code);
        Assert.Equal("UNKNOWN_OR_UNSAFE_TOKEN", Check(token: Token() with { TokenKey = "Primary-color" }).Code);
        Assert.Equal("UNKNOWN_OR_UNSAFE_TOKEN", Check(token: Token() with { TokenKey = "primary--color" }).Code);
        Assert.Equal("UNKNOWN_OR_UNSAFE_TOKEN", Check(token: Token() with { TokenKey = "primary-color\nBearer" }).Code);
    }

    [Fact]
    public void SuppliedCaseInsensitiveAllowlistCannotBypassExactCatalogKey()
    {
        IReadOnlySet<string> mismatchedCatalog = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PRIMARY-COLOR"
        };
        var rejected = C0501DesignTokenDomainRules.Evaluate(Authority(), Token(), mismatchedCatalog);
        Assert.Equal("UNKNOWN_OR_UNSAFE_TOKEN", rejected.Code);
        Assert.False(rejected.AuthorizesThemeMutation);

        IReadOnlySet<string> canonicalCatalog = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "primary-color"
        };
        var accepted = C0501DesignTokenDomainRules.Evaluate(Authority(), Token(), canonicalCatalog);
        Assert.True(accepted.ReadyForTrustedApplicationReview);
        Assert.False(accepted.AuthorizesTenantAccess);
    }

    [Fact]
    public void HtmlCssJavascriptAndExternalUrlsAreRejected()
    {
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE", Check(token: Token() with { TokenValue = "url(javascript:alert(1))" }).Code);
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE", Check(token: Token() with { TokenValue = "<script>alert(1)</script>" }).Code);
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE", Check(token: Token() with { TokenValue = "var(--tenant-b)" }).Code);
    }

    [Fact]
    public void ColorMustBeHexRgbNotArbitraryCss()
    {
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE", Check(token: Token() with { TokenValue = "#12GG45" }).Code);
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE", Check(token: Token() with { TokenValue = "#FFF" }).Code);
        Assert.True(Check(token: Token() with { TokenValue = "#000000" }).ReadyForTrustedApplicationReview);
    }

    [Fact]
    public void SpacingAndRadiusAreStrictlyBoundedPixels()
    {
        var spacing = Token() with { TokenKey = "spacing-sm", TokenType = "SPACING", TokenValue = "96px" };
        Assert.True(Check(token: spacing).ReadyForTrustedApplicationReview);
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE", Check(token: spacing with { TokenValue = "97px" }).Code);
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE", Check(token: spacing with { TokenValue = "-1px" }).Code);
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE", Check(token: spacing with { TokenValue = "calc(1px)" }).Code);
        Assert.True(Check(token: Token() with { TokenKey = "radius-card", TokenType = "RADIUS", TokenValue = "0px" }).ReadyForTrustedApplicationReview);
    }

    [Fact]
    public void FontSizeRespectsInclusiveMinMax()
    {
        var t = Token() with { TokenKey = "body-size", TokenType = "FONT_SIZE", TokenValue = "10px" };
        Assert.True(Check(token: t).ReadyForTrustedApplicationReview);
        Assert.True(Check(token: t with { TokenValue = "48px" }).ReadyForTrustedApplicationReview);
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE", Check(token: t with { TokenValue = "9px" }).Code);
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE", Check(token: t with { TokenValue = "49px" }).Code);
    }

    [Fact]
    public void MotionAcceptsOnlySemanticModes()
    {
        var t = Token() with { TokenKey = "motion-mode", TokenType = "MOTION", TokenValue = "REDUCED" };
        Assert.True(Check(token: t).ReadyForTrustedApplicationReview);
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE", Check(token: t with { TokenValue = "spin(999s)" }).Code);
    }

    [Fact]
    public void IdempotencyKeyMustBeCanonicalGuidAndIsNotDurableDeduplication()
    {
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", Check(token: Token() with { IdempotencyKey = "" }).Code);
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", Check(token: Token() with { IdempotencyKey = "shared-across-tenants" }).Code);
    }

    [Fact]
    public void UnknownTypeAndMissingInputFailClosed()
    {
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE", Check(token: Token() with { TokenType = "ARBITRARY_CSS" }).Code);
        Assert.Equal("TRUSTED_AUTHORITY_REQUIRED", C0501DesignTokenDomainRules.Evaluate(null, Token(), Keys).Code);
        Assert.Equal("TRUSTED_AUTHORITY_REQUIRED", C0501DesignTokenDomainRules.Evaluate(Authority(), null, Keys).Code);
        Assert.Throws<ArgumentNullException>(() => C0501DesignTokenDomainRules.Evaluate(Authority(), Token(), null!));
    }
}
