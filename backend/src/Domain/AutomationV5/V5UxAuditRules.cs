namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// Pure server-side validation for V5 UX, accessibility and performance audit
/// observations. Authorized catalogs, tenant scope and origin SHA must be
/// supplied by authenticated application code. No claim, A5 or runtime PASS
/// is certified by this validator.
/// </summary>
public static class V5UxAuditRules
{
    public sealed record Observation(
        string SurfaceId,
        string CriterionKey,
        string TenantScope,
        string SourceCommitSha,
        string EvidenceReference,
        decimal ObservedValue,
        decimal BudgetValue,
        string Comparison);

    public readonly record struct Verdict(bool ReadyForEvidenceReview, string Code)
    {
        public static Verdict Ready() => new(true, "READY_FOR_EVIDENCE_REVIEW");
        public static Verdict Deny(string code) => new(false, code);
    }

    public static Verdict Validate(
        Observation observation,
        IReadOnlySet<string> authorizedSurfaces,
        IReadOnlySet<string> authorizedCriteria,
        string authenticatedTenantScope,
        string expectedSourceSha)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(authorizedSurfaces);
        ArgumentNullException.ThrowIfNull(authorizedCriteria);

        if (string.IsNullOrWhiteSpace(authenticatedTenantScope) ||
            !string.Equals(observation.TenantScope, authenticatedTenantScope, StringComparison.Ordinal))
            return Verdict.Deny("TENANT_SCOPE_MISMATCH");

        if (string.IsNullOrWhiteSpace(observation.SurfaceId) ||
            !authorizedSurfaces.Contains(observation.SurfaceId))
            return Verdict.Deny("UNKNOWN_SURFACE");

        if (string.IsNullOrWhiteSpace(observation.CriterionKey) ||
            !authorizedCriteria.Contains(observation.CriterionKey))
            return Verdict.Deny("UNKNOWN_CRITERION");

        if (!IsFullSha(expectedSourceSha) ||
            !string.Equals(observation.SourceCommitSha, expectedSourceSha, StringComparison.Ordinal))
            return Verdict.Deny("SOURCE_SHA_MISMATCH");

        if (string.IsNullOrWhiteSpace(observation.EvidenceReference) ||
            observation.EvidenceReference.Length > 512 ||
            observation.EvidenceReference.Any(char.IsControl))
            return Verdict.Deny("EVIDENCE_REQUIRED_OR_UNSAFE");

        if (observation.BudgetValue <= 0 || observation.ObservedValue < 0)
            return Verdict.Deny("INVALID_MEASUREMENT");

        var withinBudget = observation.Comparison switch
        {
            "MAX" => observation.ObservedValue <= observation.BudgetValue,
            "MIN" => observation.ObservedValue >= observation.BudgetValue,
            _ => false
        };

        if (observation.Comparison is not ("MAX" or "MIN"))
            return Verdict.Deny("UNKNOWN_COMPARISON");

        return withinBudget ? Verdict.Ready() : Verdict.Deny("BUDGET_EXCEEDED");
    }

    private static bool IsFullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
}
