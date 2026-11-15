using System.Globalization;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C01-03.M03.01. Pure fail-closed rules for an explicitly approved dynamic
/// configuration catalog. No DB, entitlement grant, JWT or runtime mutation.
/// A trusted Application service must independently authorize and apply CAS.
/// </summary>
public static class V5DynamicConfigurationDomainRules
{
    public enum ValueKind { Boolean = 1, BoundedInteger = 2, SafeString = 3, ColorHex = 4 }
    public enum ConfigScope { Tenant = 1, Platform = 2 }

    public sealed record TrustedAuthority(
        int? AuthenticatedTenantId,
        bool ServerAuthenticated,
        bool MembershipCurrent,
        bool RbacConfigWritePermission,
        bool EntitlementVerified,
        bool PermissionRevoked,
        bool PlatformIdentityVerified,
        bool PlatformGrantVerified);

    public sealed record CatalogKey(
        string? CanonicalKey, ValueKind Kind, ConfigScope AllowedScope,
        bool ContainsSecret, int MaxStringLength, long? MinInteger, long? MaxInteger);

    public sealed record ProposedChange(
        string? Key, ValueKind ClientDeclaredKind, ConfigScope RequestedScope,
        int? ClientRequestedTenantId, string? Value, long ExpectedVersion,
        long CurrentVersion, bool ClientSuppliedGrant, bool AttemptsSecurityBypass);

    public readonly record struct Verdict(string Code)
    {
        // This preflight can never grant write access or change a persisted flag.
        public bool GrantsWrite => false;
        public bool GrantsPlatformScope => false;
        public bool OverridesRbac => false;
        public bool CertifiesDurableCas => false;
    }

    private static readonly string[] ProhibitedPrefixes =
    {
        "disable_", "bypass_", "secret", "secrets.", "database.",
        "security.", "jwt.", "worker.", "automation.", "auth."
    };

    public static Verdict Evaluate(
        TrustedAuthority? authority, CatalogKey? definition,
        ProposedChange? change, IReadOnlySet<string> approvedCatalogKeys)
    {
        ArgumentNullException.ThrowIfNull(approvedCatalogKeys);
        if (authority is null || !authority.ServerAuthenticated ||
            !authority.MembershipCurrent || authority.PermissionRevoked ||
            !authority.RbacConfigWritePermission ||
            authority.AuthenticatedTenantId is not > 0)
            return new("SERVER_MEMBERSHIP_AND_RBAC_REQUIRED");

        if (!authority.EntitlementVerified)
            return new("COMMERCIAL_ENTITLEMENT_REQUIRED");

        if (change is null || change.ClientSuppliedGrant ||
            change.AttemptsSecurityBypass)
            return new("CLIENT_AUTHORITY_OR_SECURITY_BYPASS_DENIED");

        if (definition is null || !ValidKey(definition.CanonicalKey) ||
            !ValidKey(change.Key) ||
            !string.Equals(definition.CanonicalKey, change.Key, StringComparison.Ordinal) ||
            !approvedCatalogKeys.Contains(change.Key!) ||
            IsReserved(change.Key!))
            return new("CATALOG_KEY_NOT_ALLOWED");

        if (definition.ContainsSecret)
            return new("SECRET_CONFIGURATION_NOT_EDITABLE");

        if (!Enum.IsDefined(definition.Kind) ||
            !Enum.IsDefined(change.ClientDeclaredKind) ||
            definition.Kind != change.ClientDeclaredKind ||
            !Enum.IsDefined(change.RequestedScope) ||
            definition.AllowedScope != change.RequestedScope)
            return new("CONFIG_TYPE_OR_SCOPE_MISMATCH");

        if (change.RequestedScope == ConfigScope.Tenant)
        {
            if (change.ClientRequestedTenantId != authority.AuthenticatedTenantId)
                return new("CROSS_TENANT_CONFIG_DENIED");
        }
        else if (change.RequestedScope == ConfigScope.Platform)
        {
            if (!authority.PlatformIdentityVerified ||
                !authority.PlatformGrantVerified ||
                change.ClientRequestedTenantId is not null)
                return new("INDEPENDENT_PLATFORM_AUTHORITY_REQUIRED");
        }
        else
            return new("CONFIG_TYPE_OR_SCOPE_MISMATCH");

        if (change.ExpectedVersion <= 0 ||
            change.CurrentVersion != change.ExpectedVersion)
            return new("STALE_CONFIG_VERSION_DENIED");

        if (!ValidValue(definition, change.Value))
            return new("CONFIG_VALUE_INVALID_OR_UNSAFE");

        // Actual JWT/RBAC, tenant resource ownership, persistence transaction,
        // optimistic concurrency and audit must be independently enforced.
        return new("TRUSTED_BACKEND_AUTHORIZATION_CAS_REQUIRED");
    }

    private static bool ValidKey(string? key) =>
        key is { Length: > 0 and <= 64 } && !key.Contains("..", StringComparison.Ordinal) &&
        key.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                     (c >= '0' && c <= '9') || c == '.' || c == '_');

    private static bool IsReserved(string key) =>
        ProhibitedPrefixes.Any(prefix =>
            key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static bool ValidValue(CatalogKey key, string? value)
    {
        if (value is null) return false;
        switch (key.Kind)
        {
            case ValueKind.Boolean:
                return value is "true" or "false";
            case ValueKind.BoundedInteger:
                return value.Length is > 0 and <= 20 &&
                       string.Equals(value, value.Trim(), StringComparison.Ordinal) &&
                       long.TryParse(value, NumberStyles.AllowLeadingSign,
                           CultureInfo.InvariantCulture, out long number) &&
                       key.MinInteger is { } min && key.MaxInteger is { } max &&
                       min <= max && number >= min && number <= max;
            case ValueKind.ColorHex:
                return (value.Length is 4 or 7) && value[0] == '#' &&
                       value.Skip(1).All(c => (c >= '0' && c <= '9') ||
                           (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
            case ValueKind.SafeString:
                return key.MaxStringLength is > 0 and <= 256 &&
                       value.Length > 0 && value.Length <= key.MaxStringLength &&
                       value.All(c => char.IsLetterOrDigit(c) ||
                           c is ' ' or '_' or '-' or '.' or '/') &&
                       !value.Contains("..", StringComparison.Ordinal);
            default:
                return false;
        }
    }
}
