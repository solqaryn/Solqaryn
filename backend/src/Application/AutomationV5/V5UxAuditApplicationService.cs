using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Bounded, read-only UX/accessibility/performance observation preflight.
/// Never certifies measurements, signs evidence, grants tenant privileges,
/// dispatches jobs, or writes institutional sources.
/// </summary>
public sealed class V5UxAuditApplicationService
{
    public sealed record Request(
        string? ExpectedSourceSha,
        string? ObservedSourceSha,
        string? ServerResolvedTenantScope,
        bool TenantContextVerifiedByServer,
        IReadOnlySet<string> AuthorizedSurfaces,
        IReadOnlySet<string> AuthorizedCriteria,
        IReadOnlyCollection<V5UxAuditRules.Observation> Observations,
        int MaxObservations = 500);

    public readonly record struct Result(string Code, int ItemsReviewed)
    {
        public bool AllowsInstitutionalWrite => false;
        public bool CertifiesExternalMeasurement => false;
        public bool GrantsUserAccess => false;
    }

    public Result Prepare(Request request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.AuthorizedSurfaces);
        ArgumentNullException.ThrowIfNull(request.AuthorizedCriteria);
        ArgumentNullException.ThrowIfNull(request.Observations);

        if (!request.TenantContextVerifiedByServer ||
            string.IsNullOrWhiteSpace(request.ServerResolvedTenantScope))
            return new Result("VERIFIED_TENANT_CONTEXT_REQUIRED", 0);

        if (!FullSha(request.ExpectedSourceSha) ||
            !string.Equals(request.ExpectedSourceSha,
                request.ObservedSourceSha, StringComparison.Ordinal))
            return new Result("EXACT_SHA_MISMATCH", 0);

        if (request.MaxObservations is < 1 or > 500 ||
            request.Observations.Count is < 1 ||
            request.Observations.Count > request.MaxObservations)
            return new Result("OBSERVATION_CAPACITY_EXCEEDED", 0);

        if (request.AuthorizedSurfaces.Count == 0 ||
            request.AuthorizedCriteria.Count == 0)
            return new Result("VERIFIED_CATALOG_REQUIRED", 0);

        var index = 0;
        foreach (var observation in request.Observations)
        {
            if (observation is null)
                return new Result("NULL_OBSERVATION", index);
            index++;
            var check = V5UxAuditRules.Validate(
                observation,
                request.AuthorizedSurfaces,
                request.AuthorizedCriteria,
                request.ServerResolvedTenantScope,
                request.ExpectedSourceSha!);
            if (!check.ReadyForEvidenceReview)
                return new Result(check.Code, index);
        }

        // The input can only be declared structurally valid. Real browser
        // observations, tenant authentication, source checks and A5 remain
        // the responsibility of the future authenticated adapter.
        return new Result("EXTERNAL_EVIDENCE_AND_A5_REQUIRED", index);
    }

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
}
