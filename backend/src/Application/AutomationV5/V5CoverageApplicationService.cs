using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Read-only, fail-closed Application orchestration for a caller-supplied V5
/// requirements-to-work-items snapshot. No client-provided catalog or SHA is
/// authenticated here. The result never grants institutional writes, A5,
/// permission elevation, a live coverage certificate or a background task.
/// </summary>
public sealed class V5CoverageApplicationService
{
    public sealed record Request(
        string ExpectedHeadSha,
        string ObservedHeadSha,
        string ExpectedSourceRevision,
        V5CoverageDomainValidator.SourceSnapshot Source,
        IReadOnlySet<string> ExpectedApprovedRequirementKeys,
        IReadOnlySet<string> ExpectedPlanItemIds,
        IReadOnlyCollection<V5RequirementCoverageRules.Entry> Mappings,
        int MaxMappings = 2340);

    public sealed record Result(
        string Code,
        int MappingsExamined,
        int RequirementsWithoutMapping)
    {
        public bool AllowsInstitutionalWrite => false;
        public bool CertifiesFullCoverage => false;
        public bool GrantsA5OrEnvironmentPromotion => false;
    }

    public Result Prepare(Request request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Source);
        ArgumentNullException.ThrowIfNull(request.ExpectedApprovedRequirementKeys);
        ArgumentNullException.ThrowIfNull(request.ExpectedPlanItemIds);
        ArgumentNullException.ThrowIfNull(request.Mappings);

        if (!FullSha(request.ExpectedHeadSha) ||
            !FullSha(request.ObservedHeadSha) ||
            !string.Equals(request.ExpectedHeadSha, request.ObservedHeadSha,
                StringComparison.OrdinalIgnoreCase))
            return Deny("CHECK_SHA_MISMATCH");

        if (string.IsNullOrWhiteSpace(request.ExpectedSourceRevision) ||
            !string.Equals(request.ExpectedSourceRevision,
                request.Source.SourceRevision, StringComparison.Ordinal))
            return Deny("SOURCE_REVISION_MISMATCH");

        if (request.MaxMappings is < 1 or > 2340 ||
            request.Mappings.Count > request.MaxMappings ||
            request.ExpectedPlanItemIds.Count is < 1 or > 2340 ||
            request.Source.ExistingPlanItemIds is null ||
            request.Source.ExistingPlanItemIds.Count > 2340)
            return Deny("CAPACITY_EXCEEDED");

        if (request.Source.ApprovedRequirementKeys is null ||
            request.ExpectedApprovedRequirementKeys.Count == 0 ||
            request.Source.ApprovedRequirementKeys.Count !=
                request.ExpectedApprovedRequirementKeys.Count ||
            !request.ExpectedApprovedRequirementKeys.All(
                request.Source.ApprovedRequirementKeys.Contains) ||
            request.Source.ExistingPlanItemIds.Count !=
                request.ExpectedPlanItemIds.Count ||
            !request.ExpectedPlanItemIds.All(
                request.Source.ExistingPlanItemIds.Contains))
            return Deny("CATALOG_SNAPSHOT_MISMATCH");

        var assessed = V5CoverageDomainValidator.Assess(
            request.Source, request.Mappings);
        if (!assessed.ReadyForEvidenceReview)
            return new Result(assessed.Code, assessed.MappingsExamined,
                assessed.RequirementsWithoutMapping);

        // Even matching catalogs and complete mappings are *only* a preflight:
        // an independently authenticated adapter must read the actual Standard,
        // Git HEAD, PLAN/COLA/DAG, reviewer identity, CI and BITACORA.
        return new Result("AUTHENTICATED_EVIDENCE_REQUIRED",
            assessed.MappingsExamined, assessed.RequirementsWithoutMapping);
    }

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    private static Result Deny(string code) => new(code, 0, 0);
}
