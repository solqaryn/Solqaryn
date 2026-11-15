namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C14-01.M03.01. Pure fail-closed preflight for future functional acceptance.
/// Server-verified inputs must be provided by a trusted Application adapter.
/// This class NEVER grants access, executes a commercial action or certifies QA.
/// </summary>
public static class V5FunctionalAcceptanceDomainRules
{
    public sealed record VerifiedActor(
        int ActorId, int TenantId, bool ActiveMembership,
        bool EffectiveActionPermission, bool PersistedOwnershipVerified,
        string EvidenceCommitSha);

    public sealed record AcceptanceEvidence(
        string CaseKey, string ActionKey, int ResourceTenantId,
        string SourceCommitSha, string EvidenceReference,
        bool PositiveScenarioPresent, bool NegativeScenarioPresent,
        bool ContainsPrivateData, bool RequiresProductionAction,
        bool RollbackDefined, bool IdempotencyRiskControlled);

    public readonly record struct Verdict(string Code)
    {
        public bool ReadyForIndependentFunctionalReview =>
            Code == "READY_FOR_FUNCTIONAL_REVIEW";
        public bool AuthorizesCommercialAction => false;
        public bool AuthorizesTenantAccess => false;
        public bool CertifiesQAPromotion => false;
    }

    public static Verdict Evaluate(
        VerifiedActor? trusted, AcceptanceEvidence? evidence,
        IReadOnlySet<string> approvedCaseKeys,
        IReadOnlySet<string> approvedActionKeys)
    {
        ArgumentNullException.ThrowIfNull(approvedCaseKeys);
        ArgumentNullException.ThrowIfNull(approvedActionKeys);

        if (trusted is null || evidence is null || trusted.ActorId <= 0 ||
            trusted.TenantId <= 0 || !trusted.ActiveMembership ||
            !trusted.EffectiveActionPermission || !trusted.PersistedOwnershipVerified)
            return new Verdict("SERVER_VERIFIED_AUTHORITY_REQUIRED");

        if (trusted.TenantId != evidence.ResourceTenantId)
            return new Verdict("CROSS_TENANT_OWNERSHIP_DENIED");

        if (!IsFullSha(trusted.EvidenceCommitSha) ||
            !string.Equals(trusted.EvidenceCommitSha, evidence.SourceCommitSha,
                StringComparison.Ordinal))
            return new Verdict("EXACT_SOURCE_COMMIT_REQUIRED");

        if (string.IsNullOrWhiteSpace(evidence.CaseKey) ||
            !approvedCaseKeys.Contains(evidence.CaseKey) ||
            string.IsNullOrWhiteSpace(evidence.ActionKey) ||
            !approvedActionKeys.Contains(evidence.ActionKey))
            return new Verdict("UNAPPROVED_CASE_OR_ACTION");

        if (string.IsNullOrWhiteSpace(evidence.EvidenceReference) ||
            evidence.EvidenceReference.Length > 256 ||
            evidence.EvidenceReference.Any(char.IsControl))
            return new Verdict("INVALID_EVIDENCE_REFERENCE");

        if (evidence.ContainsPrivateData)
            return new Verdict("UNREDACTED_PRIVATE_EVIDENCE");

        if (evidence.RequiresProductionAction)
            return new Verdict("PRODUCTION_ACTION_NOT_AUTHORIZED");

        if (!evidence.PositiveScenarioPresent || !evidence.NegativeScenarioPresent)
            return new Verdict("POSITIVE_AND_NEGATIVE_EVIDENCE_REQUIRED");

        if (!evidence.RollbackDefined || !evidence.IdempotencyRiskControlled)
            return new Verdict("ROLLBACK_OR_IDEMPOTENCY_GATE_MISSING");

        // This is only a formal preflight; it does not inspect a live HTTP,
        // MySQL, browser or payment outcome and never means A0-A5 PASS.
        return new Verdict("READY_FOR_FUNCTIONAL_REVIEW");
    }

    private static bool IsFullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
}
