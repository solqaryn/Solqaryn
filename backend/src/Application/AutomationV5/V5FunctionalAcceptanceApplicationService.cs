using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C14-01.M03.02. Bounded, read-only Application use case that composes
/// Domain functional acceptance rules. VerifiedActor and case/action catalogs
/// MUST originate from a trusted backend adapter, not browser/user claims.
/// There are no runtime bindings, writes, payment effects or QA promotion.
/// </summary>
public sealed class V5FunctionalAcceptanceApplicationService
{
    public sealed record Request(
        int ClientRequestedTenantId,
        IReadOnlyCollection<V5FunctionalAcceptanceDomainRules.AcceptanceEvidence?>? Items,
        int MaximumItems = 64);

    public readonly record struct Result(string Code, int ItemsChecked)
    {
        public bool DomainPreflightAccepted => Code == "EXTERNAL_ACCEPTANCE_EVIDENCE_AND_A5_REQUIRED";
        public bool AuthorizesCommercialAction => false;
        public bool AuthorizesTenantAccess => false;
        public bool CertifiesFunctionalAcceptance => false;
        public bool AuthorizesRelease => false;
    }

    public Result Prepare(
        V5FunctionalAcceptanceDomainRules.VerifiedActor? trustedBackend,
        Request? request,
        IReadOnlySet<string>? serverApprovedCaseKeys,
        IReadOnlySet<string>? serverApprovedActionKeys)
    {
        if (trustedBackend is null || trustedBackend.ActorId <= 0 ||
            trustedBackend.TenantId <= 0 || !trustedBackend.ActiveMembership ||
            !trustedBackend.EffectiveActionPermission ||
            !trustedBackend.PersistedOwnershipVerified)
            return new Result("SERVER_VERIFIED_AUTHORITY_REQUIRED", 0);

        if (request is null || request.ClientRequestedTenantId <= 0 ||
            request.ClientRequestedTenantId != trustedBackend.TenantId)
            return new Result("CROSS_TENANT_REQUEST_DENIED", 0);

        if (serverApprovedCaseKeys is null || serverApprovedCaseKeys.Count == 0 ||
            serverApprovedActionKeys is null || serverApprovedActionKeys.Count == 0)
            return new Result("TRUSTED_CASE_AND_ACTION_CATALOG_REQUIRED", 0);

        if (request.Items is null || request.MaximumItems is < 1 or > 64 ||
            request.Items.Count == 0 || request.Items.Count > request.MaximumItems)
            return new Result("ACCEPTANCE_BATCH_CAPACITY_INVALID", 0);

        var checkedCount = 0;
        foreach (var item in request.Items)
        {
            checkedCount++;
            if (item is null)
                return new Result("NULL_ACCEPTANCE_EVIDENCE", checkedCount);

            var verdict = V5FunctionalAcceptanceDomainRules.Evaluate(
                trustedBackend, item, serverApprovedCaseKeys, serverApprovedActionKeys);
            if (!verdict.ReadyForIndependentFunctionalReview)
                return new Result(verdict.Code, checkedCount);
        }

        // Formal preflight, not actual authenticated HTTP, DB, payment or
        // independent QA/A5 proof. A true Domain verdict does not grant access.
        return new Result("EXTERNAL_ACCEPTANCE_EVIDENCE_AND_A5_REQUIRED", checkedCount);
    }
}
