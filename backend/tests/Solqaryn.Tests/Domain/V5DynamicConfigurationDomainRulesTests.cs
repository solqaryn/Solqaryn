using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5DynamicConfigurationDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class V5DynamicConfigurationDomainRulesTests
{
    private static Rules.TrustedAuthority Actor() =>
        new(11, true, true, true, true, false, false, false);
    private static Rules.CatalogKey Key(Rules.ValueKind kind = Rules.ValueKind.Boolean) =>
        new("ui.compactMode", kind, Rules.ConfigScope.Tenant, false, 120, 0, 100);
    private static Rules.ProposedChange Change(string value = "true") =>
        new("ui.compactMode", Rules.ValueKind.Boolean, Rules.ConfigScope.Tenant,
            11, value, 3, 3, false, false);
    private static IReadOnlySet<string> Allowlist() =>
        new HashSet<string>(StringComparer.Ordinal) { "ui.compactMode", "ui.colorHex" };
    private static Rules.Verdict Eval(Rules.TrustedAuthority? actor = null,
        Rules.CatalogKey? definition = null, Rules.ProposedChange? change = null,
        IReadOnlySet<string>? catalog = null) =>
        Rules.Evaluate(actor ?? Actor(), definition ?? Key(),
            change ?? Change(), catalog ?? Allowlist());

    [Fact]
    public void ValidConfigurationStillRequiresAuthenticatedBackendCas()
    {
        var result = Eval();
        Assert.Equal("TRUSTED_BACKEND_AUTHORIZATION_CAS_REQUIRED", result.Code);
        Assert.False(result.GrantsWrite);
        Assert.False(result.GrantsPlatformScope);
        Assert.False(result.OverridesRbac);
        Assert.False(result.CertifiesDurableCas);
    }

    [Fact]
    public void MissingMembershipRevocationOrRbacFailsClosed()
    {
        Assert.Equal("SERVER_MEMBERSHIP_AND_RBAC_REQUIRED",
            Eval(actor: Actor() with { MembershipCurrent = false }).Code);
        Assert.Equal("SERVER_MEMBERSHIP_AND_RBAC_REQUIRED",
            Eval(actor: Actor() with { PermissionRevoked = true }).Code);
        Assert.Equal("SERVER_MEMBERSHIP_AND_RBAC_REQUIRED",
            Eval(actor: Actor() with { RbacConfigWritePermission = false }).Code);
        Assert.Equal("COMMERCIAL_ENTITLEMENT_REQUIRED",
            Eval(actor: Actor() with { EntitlementVerified = false }).Code);
    }

    [Fact]
    public void ForgedTenantAndRoleAuthorityAreRejected()
    {
        Assert.Equal("CROSS_TENANT_CONFIG_DENIED",
            Eval(change: Change() with { ClientRequestedTenantId = 22 }).Code);
        Assert.Equal("CLIENT_AUTHORITY_OR_SECURITY_BYPASS_DENIED",
            Eval(change: Change() with { ClientSuppliedGrant = true }).Code);
        Assert.Equal("CLIENT_AUTHORITY_OR_SECURITY_BYPASS_DENIED",
            Eval(change: Change() with { AttemptsSecurityBypass = true }).Code);
    }

    [Fact]
    public void UnapprovedAndReservedKeysNeverBecomeEditable()
    {
        Assert.Equal("CATALOG_KEY_NOT_ALLOWED",
            Eval(catalog: new HashSet<string>()).Code);
        var unsafeKey = Key() with { CanonicalKey = "disable_rbac" };
        var unsafeChange = Change() with { Key = "disable_rbac" };
        Assert.Equal("CATALOG_KEY_NOT_ALLOWED",
            Eval(definition: unsafeKey, change: unsafeChange,
                catalog: new HashSet<string> { "disable_rbac" }).Code);
        Assert.Equal("CATALOG_KEY_NOT_ALLOWED",
            Eval(definition: Key() with { CanonicalKey = "../ui" }).Code);
    }

    [Fact]
    public void SecretsUnknownTypesAndWrongScopesAreDenied()
    {
        Assert.Equal("SECRET_CONFIGURATION_NOT_EDITABLE",
            Eval(definition: Key() with { ContainsSecret = true }).Code);
        Assert.Equal("CONFIG_TYPE_OR_SCOPE_MISMATCH",
            Eval(change: Change() with { ClientDeclaredKind = Rules.ValueKind.ColorHex }).Code);
        Assert.Equal("CONFIG_TYPE_OR_SCOPE_MISMATCH",
            Eval(change: Change() with { RequestedScope = Rules.ConfigScope.Platform }).Code);
        Assert.Equal("CONFIG_TYPE_OR_SCOPE_MISMATCH",
            Eval(definition: Key() with { Kind = (Rules.ValueKind)99 }).Code);
    }

    [Fact]
    public void StaleAndZeroVersionsRequireRealOptimisticConcurrency()
    {
        Assert.Equal("STALE_CONFIG_VERSION_DENIED",
            Eval(change: Change() with { ExpectedVersion = 2 }).Code);
        Assert.Equal("STALE_CONFIG_VERSION_DENIED",
            Eval(change: Change() with { ExpectedVersion = 0 }).Code);
    }

    [Fact]
    public void BooleanAndNumericBoundariesAreEnforced()
    {
        Assert.Equal("CONFIG_VALUE_INVALID_OR_UNSAFE", Eval(change: Change("TRUE")).Code);
        var integer = Key(Rules.ValueKind.BoundedInteger);
        var valid = Change("100") with { ClientDeclaredKind = Rules.ValueKind.BoundedInteger };
        Assert.Equal("TRUSTED_BACKEND_AUTHORIZATION_CAS_REQUIRED",
            Eval(definition: integer, change: valid).Code);
        Assert.Equal("CONFIG_VALUE_INVALID_OR_UNSAFE",
            Eval(definition: integer, change: valid with { Value = "101" }).Code);
        Assert.Equal("CONFIG_VALUE_INVALID_OR_UNSAFE",
            Eval(definition: integer, change: valid with { Value = " 4" }).Code);
    }

    [Fact]
    public void SafeColorsAndScriptInjectionAreChecked()
    {
        var colorKey = Key(Rules.ValueKind.ColorHex) with { CanonicalKey = "ui.colorHex" };
        var colorRequest = Change("#112233") with
        { Key = "ui.colorHex", ClientDeclaredKind = Rules.ValueKind.ColorHex };
        Assert.Equal("TRUSTED_BACKEND_AUTHORIZATION_CAS_REQUIRED",
            Eval(definition: colorKey, change: colorRequest).Code);
        Assert.Equal("CONFIG_VALUE_INVALID_OR_UNSAFE",
            Eval(definition: colorKey, change: colorRequest with { Value = "url(javascript:alert(1))" }).Code);
        Assert.Equal("CONFIG_VALUE_INVALID_OR_UNSAFE",
            Eval(definition: colorKey, change: colorRequest with { Value = "#12345x" }).Code);
        var stringKey = Key(Rules.ValueKind.SafeString);
        var stringRequest = Change("<script>alert(1)</script>") with
        { ClientDeclaredKind = Rules.ValueKind.SafeString };
        Assert.Equal("CONFIG_VALUE_INVALID_OR_UNSAFE",
            Eval(definition: stringKey, change: stringRequest).Code);
    }

    [Fact]
    public void PlatformScopeNeedsSeparateAuthorityAndNoTenantHint()
    {
        var platformKey = Key() with { AllowedScope = Rules.ConfigScope.Platform };
        var request = Change() with
        { RequestedScope = Rules.ConfigScope.Platform, ClientRequestedTenantId = null };
        Assert.Equal("INDEPENDENT_PLATFORM_AUTHORITY_REQUIRED",
            Eval(definition: platformKey, change: request).Code);
        var platform = Actor() with { PlatformIdentityVerified = true, PlatformGrantVerified = true };
        var result = Eval(actor: platform, definition: platformKey, change: request);
        Assert.Equal("TRUSTED_BACKEND_AUTHORIZATION_CAS_REQUIRED", result.Code);
        Assert.False(result.GrantsWrite);
    }

    [Fact]
    public void CaseInsensitiveCatalogComparerCannotWidenExactCanonicalAllowlist()
    {
        // R2 regression: callers may accidentally construct a backend catalog
        // with OrdinalIgnoreCase; Domain must still enforce exact canonical keys.
        var caseInsensitive = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ui.compactMode"
        };
        var alteredDefinition = Key() with { CanonicalKey = "UI.compactMode" };
        var alteredChange = Change() with { Key = "UI.compactMode" };
        Assert.Equal("CATALOG_KEY_NOT_ALLOWED",
            Eval(definition: alteredDefinition, change: alteredChange, catalog: caseInsensitive).Code);
        Assert.Equal("TRUSTED_BACKEND_AUTHORIZATION_CAS_REQUIRED",
            Eval(catalog: caseInsensitive).Code);
    }

    [Fact]
    public void NullCatalogAndNullInputsCannotBeTrusted()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Rules.Evaluate(Actor(), Key(), Change(), null!));
        Assert.Equal("SERVER_MEMBERSHIP_AND_RBAC_REQUIRED",
            Rules.Evaluate(null, Key(), Change(), Allowlist()).Code);
        Assert.Equal("CLIENT_AUTHORITY_OR_SECURITY_BYPASS_DENIED",
            Rules.Evaluate(Actor(), Key(), null, Allowlist()).Code);
    }
}
