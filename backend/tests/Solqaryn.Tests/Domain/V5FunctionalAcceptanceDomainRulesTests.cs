using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class V5FunctionalAcceptanceDomainRulesTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly IReadOnlySet<string> Cases =
        new HashSet<string>(StringComparer.Ordinal) { "AF01", "AF02" };
    private static readonly IReadOnlySet<string> Actions =
        new HashSet<string>(StringComparer.Ordinal) { "READ_OWN", "PREPARE_ONLY" };

    private static V5FunctionalAcceptanceDomainRules.VerifiedActor Actor() =>
        new(7, 11, true, true, true, Sha);

    private static V5FunctionalAcceptanceDomainRules.AcceptanceEvidence Evidence() =>
        new("AF01", "READ_OWN", 11, Sha, "ci:acceptance:001",
            true, true, false, false, true, true);

    private static V5FunctionalAcceptanceDomainRules.Verdict Check(
        V5FunctionalAcceptanceDomainRules.AcceptanceEvidence? evidence = null,
        V5FunctionalAcceptanceDomainRules.VerifiedActor? actor = null) =>
        V5FunctionalAcceptanceDomainRules.Evaluate(
            actor ?? Actor(), evidence ?? Evidence(), Cases, Actions);

    [Fact]
    public void ValidEvidenceIsReviewableButNeverAuthorizesCommercialEffects()
    {
        var v = Check();
        Assert.True(v.ReadyForIndependentFunctionalReview);
        Assert.False(v.AuthorizesCommercialAction);
        Assert.False(v.AuthorizesTenantAccess);
        Assert.False(v.CertifiesQAPromotion);
    }

    [Fact]
    public void CrossTenantResourceIsDenied()
    {
        Assert.Equal("CROSS_TENANT_OWNERSHIP_DENIED",
            Check(Evidence() with { ResourceTenantId = 22 }).Code);
    }

    [Fact]
    public void RevokedRoleOrUnknownOwnerIsDenied()
    {
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Check(actor: Actor() with { ActiveMembership = false }).Code);
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Check(actor: Actor() with { EffectiveActionPermission = false }).Code);
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Check(actor: Actor() with { PersistedOwnershipVerified = false }).Code);
    }

    [Fact]
    public void StaleOrMalformedCommitCannotAuthorizeReview()
    {
        Assert.Equal("EXACT_SOURCE_COMMIT_REQUIRED",
            Check(Evidence() with { SourceCommitSha = new string('b', 40) }).Code);
        Assert.Equal("EXACT_SOURCE_COMMIT_REQUIRED",
            Check(actor: Actor() with { EvidenceCommitSha = "short" }).Code);
    }

    [Fact]
    public void ForgedCaseOrActionIsNotAllowlisted()
    {
        Assert.Equal("UNAPPROVED_CASE_OR_ACTION",
            Check(Evidence() with { CaseKey = "AF99" }).Code);
        Assert.Equal("UNAPPROVED_CASE_OR_ACTION",
            Check(Evidence() with { ActionKey = "CHARGE_CARD" }).Code);
    }

    [Fact]
    public void UnsafeEvidenceReferenceIsRejected()
    {
        Assert.Equal("INVALID_EVIDENCE_REFERENCE",
            Check(Evidence() with { EvidenceReference = "token\nsecret" }).Code);
        Assert.Equal("INVALID_EVIDENCE_REFERENCE",
            Check(Evidence() with { EvidenceReference = "" }).Code);
    }

    [Fact]
    public void UnredactedPrivateDataCannotPass()
    {
        Assert.Equal("UNREDACTED_PRIVATE_EVIDENCE",
            Check(Evidence() with { ContainsPrivateData = true }).Code);
    }

    [Fact]
    public void ProductionOrPaymentEffectIsNotAuthorized()
    {
        Assert.Equal("PRODUCTION_ACTION_NOT_AUTHORIZED",
            Check(Evidence() with { RequiresProductionAction = true }).Code);
    }

    [Fact]
    public void BothPositiveAndNegativeEvidenceRequired()
    {
        Assert.Equal("POSITIVE_AND_NEGATIVE_EVIDENCE_REQUIRED",
            Check(Evidence() with { PositiveScenarioPresent = false }).Code);
        Assert.Equal("POSITIVE_AND_NEGATIVE_EVIDENCE_REQUIRED",
            Check(Evidence() with { NegativeScenarioPresent = false }).Code);
    }

    [Fact]
    public void RollbackAndIdempotencyMustBeControlled()
    {
        Assert.Equal("ROLLBACK_OR_IDEMPOTENCY_GATE_MISSING",
            Check(Evidence() with { RollbackDefined = false }).Code);
        Assert.Equal("ROLLBACK_OR_IDEMPOTENCY_GATE_MISSING",
            Check(Evidence() with { IdempotencyRiskControlled = false }).Code);
    }

    [Fact]
    public void NullAuthorityNeverBecomesAccepted()
    {
        var v = V5FunctionalAcceptanceDomainRules.Evaluate(null, Evidence(), Cases, Actions);
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED", v.Code);
        Assert.Throws<ArgumentNullException>(() =>
            V5FunctionalAcceptanceDomainRules.Evaluate(Actor(), Evidence(), null!, Actions));
    }
}
