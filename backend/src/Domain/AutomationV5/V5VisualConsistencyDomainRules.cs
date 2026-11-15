namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C13-04.M03.01. Pure server-side visual evidence preflight. Trusted inputs
/// come from authenticated Application callers, not browser payloads.
/// Never grants resource access, certifies WCAG or authorizes release.
/// </summary>
public static class V5VisualConsistencyDomainRules
{
    public sealed record VerifiedContext(
        int ActorId, int TenantId, bool MembershipCurrent,
        bool ExplicitReviewPermission, string SourceSha);

    public sealed record Observation(
        string SurfaceKey, string UiState, int TenantId, int ViewportPixels,
        string SourceSha, string EvidenceReference,
        bool KeyboardReachable, bool TextFeedback, bool ReducedMotionSafe,
        bool ContainsSensitiveData, decimal ContrastRatio, decimal RequiredContrastRatio);

    public readonly record struct Verdict(string Code)
    {
        public bool ReadyForIndependentBrowserReview => Code == "READY_FOR_BROWSER_REVIEW";
        public bool AuthorizesTenantAccess => false;
        public bool CertifiesAccessibility => false;
        public bool AuthorizesRelease => false;
    }

    private static readonly IReadOnlySet<int> Viewports = new HashSet<int> { 320, 390, 768, 1280 };
    private static readonly IReadOnlySet<string> UiStates = new HashSet<string>(StringComparer.Ordinal)
    { "LOADING", "EMPTY", "ERROR", "SUCCESS", "FORBIDDEN" };

    public static Verdict Evaluate(
        VerifiedContext? trusted, Observation? observation,
        IReadOnlySet<string> serverApprovedSurfaces)
    {
        ArgumentNullException.ThrowIfNull(serverApprovedSurfaces);
        if (trusted is null || observation is null || trusted.ActorId <= 0 ||
            trusted.TenantId <= 0 || !trusted.MembershipCurrent ||
            !trusted.ExplicitReviewPermission)
            return new Verdict("SERVER_AUTHORITY_REQUIRED");
        if (observation.TenantId != trusted.TenantId)
            return new Verdict("CROSS_TENANT_VISUAL_EVIDENCE_DENIED");
        if (!IsFullSha(trusted.SourceSha) ||
            !string.Equals(trusted.SourceSha, observation.SourceSha, StringComparison.Ordinal))
            return new Verdict("EXACT_SOURCE_SHA_REQUIRED");
        if (string.IsNullOrWhiteSpace(observation.SurfaceKey) ||
            observation.SurfaceKey.Any(char.IsControl) ||
            !serverApprovedSurfaces.Contains(observation.SurfaceKey))
            return new Verdict("UNKNOWN_OR_UNSAFE_SURFACE");
        if (!UiStates.Contains(observation.UiState) ||
            !Viewports.Contains(observation.ViewportPixels))
            return new Verdict("UNKNOWN_STATE_OR_VIEWPORT");
        if (string.IsNullOrWhiteSpace(observation.EvidenceReference) ||
            observation.EvidenceReference.Length > 256 ||
            observation.EvidenceReference.Any(char.IsControl))
            return new Verdict("EVIDENCE_MISSING_OR_UNSAFE");
        if (observation.ContainsSensitiveData)
            return new Verdict("SENSITIVE_DATA_IN_EVIDENCE");
        if (!observation.KeyboardReachable || !observation.ReducedMotionSafe)
            return new Verdict("INTERACTION_NOT_REVIEWABLE");
        if (observation.UiState is "ERROR" or "FORBIDDEN" or "EMPTY" && !observation.TextFeedback)
            return new Verdict("TEXT_STATUS_REQUIRED");
        if (observation.RequiredContrastRatio <= 0 ||
            observation.ContrastRatio < observation.RequiredContrastRatio)
            return new Verdict("CONTRAST_EVIDENCE_INSUFFICIENT");
        return new Verdict("READY_FOR_BROWSER_REVIEW");
    }
    private static bool IsFullSha(string? s) =>
        s is { Length: 40 } && s.All(Uri.IsHexDigit);
}
