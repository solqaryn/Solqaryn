namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C05-04.M03.01: pure server-side preflight for the lightness/performance contract.
/// This guard cannot authenticate JWT, read private resources, cache responses,
/// measure Core Web Vitals or authorize bypass of tenant ownership.
/// </summary>
public static class V5UiLightnessDomainRules
{
    // Matches Angular's current source-control policy; these are thresholds,
    // not measured performance or permission to raise a failing budget.
    public const long InitialWarningBytes = 650L * 1024;
    public const long InitialErrorBytes = 750L * 1024;
    public const long StyleWarningBytes = 16L * 1024;
    public const long StyleErrorBytes = 32L * 1024;

    public sealed record TrustedRequestContext(
        int? AuthenticatedTenantId,
        int? PersistedResourceTenantId,
        bool BackendIdentityVerified,
        bool CurrentMembershipVerified,
        bool ActionPermissionVerified,
        bool TenantScopedCacheKey,
        bool CacheInvalidatedForRevocation,
        bool PrivateResourcePreloadRequested,
        bool PrivatePreloadAuthorized,
        bool ClientSuppliedTenantOrRole,
        bool SuppressesUnauthorizedResponse,
        bool ContainsJwtOrPersonalTelemetry,
        string? ResourceRevision,
        string? ExpectedResourceRevision);

    public sealed record TrustedBuildEvidence(
        long? InitialBundleBytes,
        long? LargestComponentStyleBytes,
        string? ExpectedCommitSha,
        string? MeasuredCommitSha,
        bool ProducedByVerifiedBuildArtifact,
        bool BrowserCwvEvidenceAvailable);

    public readonly record struct Verdict(string Code)
    {
        // Never interpreted as authorization, bundle measurement, or CWV PASS.
        public bool NeedsIndependentRuntimeVerification =>
            Code == "EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED";
    }

    private static bool ValidSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c =>
            (c >= '0' && c <= '9') ||
            (c >= 'a' && c <= 'f'));

    public static Verdict Evaluate(
        TrustedRequestContext? context,
        TrustedBuildEvidence? build)
    {
        if (context is null || !context.BackendIdentityVerified ||
            !context.CurrentMembershipVerified ||
            !context.ActionPermissionVerified ||
            context.AuthenticatedTenantId is not > 0)
            return new("BACKEND_AUTHORITY_REQUIRED");

        if (context.ClientSuppliedTenantOrRole)
            return new("CLIENT_AUTHORITY_OVERRIDE_DENIED");

        if (context.PersistedResourceTenantId is not > 0 ||
            context.PersistedResourceTenantId != context.AuthenticatedTenantId ||
            !context.TenantScopedCacheKey ||
            !context.CacheInvalidatedForRevocation)
            return new("TENANT_CACHE_ISOLATION_REQUIRED");

        if (context.PrivateResourcePreloadRequested &&
            !context.PrivatePreloadAuthorized)
            return new("PRIVATE_PRELOAD_DENIED");

        if (context.SuppressesUnauthorizedResponse ||
            context.ContainsJwtOrPersonalTelemetry)
            return new("SECURITY_OR_PRIVACY_REGRESSION_DENIED");

        if (string.IsNullOrWhiteSpace(context.ResourceRevision) ||
            !string.Equals(
                context.ResourceRevision,
                context.ExpectedResourceRevision,
                StringComparison.Ordinal))
            return new("STALE_RESPONSE_REVISION_DENIED");

        if (build is null ||
            !build.ProducedByVerifiedBuildArtifact ||
            !ValidSha(build.ExpectedCommitSha) ||
            !string.Equals(build.ExpectedCommitSha, build.MeasuredCommitSha,
                StringComparison.Ordinal) ||
            build.InitialBundleBytes is not >= 0 ||
            build.LargestComponentStyleBytes is not >= 0)
            return new("VERIFIED_BUNDLE_MEASUREMENT_REQUIRED");

        if (build.InitialBundleBytes > InitialErrorBytes ||
            build.LargestComponentStyleBytes > StyleErrorBytes)
            return new("BUNDLE_BUDGET_EXCEEDED");

        if (build.InitialBundleBytes > InitialWarningBytes ||
            build.LargestComponentStyleBytes > StyleWarningBytes)
            return new("BUNDLE_BUDGET_WARNING_REVIEW_REQUIRED");

        // Backend authorization, actual Angular build, browser CWV, network and
        // tenant A/B integration must still be proven with separate CI evidence.
        return new("EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED");
    }
}
