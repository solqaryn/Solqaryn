using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C13-04.M03.02. A bounded, read-only application use case for preparing
/// tenant-scoped visual consistency observations for independent browser review.
/// The caller MUST resolve VerifiedContext and the allowlist on a trusted
/// backend path. No client-supplied claim grants identity, RBAC or tenant scope.
/// This service has no API, DI, persistence or external provider side effects.
/// </summary>
public sealed class V5VisualConsistencyApplicationService
{
    public sealed record Request(
        int ClientRequestedTenantId,
        IReadOnlyCollection<V5VisualConsistencyDomainRules.Observation?>? Observations,
        int MaximumObservations = 64);

    public readonly record struct Result(string Code, int ItemsChecked)
    {
        public bool DomainPreflightAccepted => Code == "EXTERNAL_BROWSER_AND_A5_REQUIRED";
        public bool CertifiesAccessibility => false;
        public bool AuthorizesTenantAccess => false;
        public bool AuthorizesRelease => false;
        public bool PersistsEvidence => false;
    }

    public Result Prepare(
        V5VisualConsistencyDomainRules.VerifiedContext? trustedBackend,
        Request? request,
        IReadOnlySet<string>? serverApprovedSurfaces)
    {
        if (trustedBackend is null || trustedBackend.ActorId <= 0 ||
            trustedBackend.TenantId <= 0 || !trustedBackend.MembershipCurrent ||
            !trustedBackend.ExplicitReviewPermission)
            return new Result("SERVER_AUTHORITY_REQUIRED", 0);

        if (request is null || request.ClientRequestedTenantId <= 0 ||
            request.ClientRequestedTenantId != trustedBackend.TenantId)
            return new Result("CROSS_TENANT_REQUEST_DENIED", 0);

        if (serverApprovedSurfaces is null || serverApprovedSurfaces.Count == 0)
            return new Result("SERVER_APPROVED_SURFACE_CATALOG_REQUIRED", 0);

        if (request.Observations is null || request.MaximumObservations is < 1 or > 64 ||
            request.Observations.Count == 0 ||
            request.Observations.Count > request.MaximumObservations)
            return new Result("OBSERVATION_CAPACITY_INVALID", 0);

        var checkedCount = 0;
        foreach (var observation in request.Observations)
        {
            checkedCount++;
            if (observation is null)
                return new Result("NULL_VISUAL_OBSERVATION", checkedCount);

            var domainVerdict = V5VisualConsistencyDomainRules.Evaluate(
                trustedBackend, observation, serverApprovedSurfaces);
            if (!domainVerdict.ReadyForIndependentBrowserReview)
                return new Result(domainVerdict.Code, checkedCount);
        }

        // This is structural preflight only: no real browser, WCAG measurement,
        // authenticated HTTP flow, tenant grant, release, or A5 was performed.
        return new Result("EXTERNAL_BROWSER_AND_A5_REQUIRED", checkedCount);
    }
}
