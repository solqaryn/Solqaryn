namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C05-01.M03.01: pure server-side preflight for typed design tokens.
/// Trusted snapshots must be constructed by authenticated Application services.
/// This class never authenticates a JWT, grants RBAC, changes a theme or writes data.
/// </summary>
public static class C0501DesignTokenDomainRules
{
    public sealed record TrustedContext(
        int ActorId, int TenantId, bool MembershipCurrent,
        bool HasThemeEditPermission, bool ResourceOwnedByTenant,
        bool RoleRevoked, long ActiveRevision, int SessionEpoch);

    public sealed record ProposedToken(
        int TenantId, int SessionEpoch, long ExpectedRevision,
        string TokenKey, string TokenType, string TokenValue,
        string IdempotencyKey, bool IsPlatformCatalogMutation);

    public readonly record struct Verdict(string Code)
    {
        public bool ReadyForTrustedApplicationReview =>
            Code == "READY_FOR_TRUSTED_APPLICATION_REVIEW";
        public bool AuthorizesTenantAccess => false;
        public bool AuthorizesThemeMutation => false;
        public bool CertifiesAccessibility => false;
        public bool AuthorizesRelease => false;
    }

    public static Verdict Evaluate(
        TrustedContext? trusted,
        ProposedToken? proposed,
        IReadOnlySet<string> serverApprovedTokenKeys)
    {
        ArgumentNullException.ThrowIfNull(serverApprovedTokenKeys);

        if (trusted is null || proposed is null ||
            trusted.ActorId <= 0 || trusted.TenantId <= 0 ||
            !trusted.MembershipCurrent || !trusted.HasThemeEditPermission ||
            trusted.RoleRevoked)
            return new Verdict("TRUSTED_AUTHORITY_REQUIRED");

        if (proposed.TenantId != trusted.TenantId ||
            !trusted.ResourceOwnedByTenant)
            return new Verdict("CROSS_TENANT_OR_RESOURCE_DENIED");

        if (proposed.IsPlatformCatalogMutation)
            return new Verdict("PLATFORM_CATALOG_MUTATION_DENIED");

        if (trusted.SessionEpoch <= 0 ||
            proposed.SessionEpoch != trusted.SessionEpoch)
            return new Verdict("STALE_OR_INVALID_SESSION_EPOCH");

        if (trusted.ActiveRevision < 0 ||
            proposed.ExpectedRevision != trusted.ActiveRevision)
            return new Verdict("THEME_REVISION_CONFLICT");

        if (!SafeTokenKey(proposed.TokenKey) ||
            !serverApprovedTokenKeys.Contains(proposed.TokenKey))
            return new Verdict("UNKNOWN_OR_UNSAFE_TOKEN");

        if (string.IsNullOrWhiteSpace(proposed.IdempotencyKey) ||
            proposed.IdempotencyKey.Length != 36 ||
            !Guid.TryParseExact(proposed.IdempotencyKey, "D", out _))
            return new Verdict("IDEMPOTENCY_KEY_REQUIRED");

        if (!SafeTypedValue(proposed.TokenType, proposed.TokenValue))
            return new Verdict("UNSAFE_OR_UNTYPED_VALUE");

        return new Verdict("READY_FOR_TRUSTED_APPLICATION_REVIEW");
    }

    private static bool SafeTokenKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 64 ||
            key[0] is < 'a' or > 'z' || key[^1] == '-')
            return false;
        var previousDash = false;
        foreach (var c in key)
        {
            if (c == '-')
            {
                if (previousDash)
                    return false;
                previousDash = true;
                continue;
            }
            if (c is not (>= 'a' and <= 'z') &&
                c is not (>= '0' and <= '9'))
                return false;
            previousDash = false;
        }
        return true;
    }

    private static bool SafeTypedValue(string? type, string? value)
    {
        if (string.IsNullOrWhiteSpace(type) ||
            string.IsNullOrWhiteSpace(value) ||
            value.Length > 40 || value.Any(char.IsControl))
            return false;

        return type switch
        {
            "COLOR" => value.Length == 7 &&
                value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit),
            "SPACING" or "RADIUS" => SafePixels(value, 0, 96),
            "FONT_SIZE" => SafePixels(value, 10, 48),
            "MOTION" => value is "NONE" or "REDUCED" or "STANDARD",
            _ => false
        };
    }

    private static bool SafePixels(string value, int min, int max)
    {
        if (!value.EndsWith("px", StringComparison.Ordinal) ||
            value.Length <= 2)
            return false;
        var number = value.AsSpan(0, value.Length - 2);
        if (number.Length > 3)
            return false;
        foreach (var c in number)
            if (c is < '0' or > '9')
                return false;
        return int.TryParse(number, out var pixels) &&
            pixels >= min && pixels <= max;
    }
}
