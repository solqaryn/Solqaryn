using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5FunctionalAcceptanceApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly V5FunctionalAcceptanceApplicationService Service = new();
    private static readonly IReadOnlySet<string> Cases =
        new HashSet<string>(StringComparer.Ordinal) { "AF01", "AF02" };
    private static readonly IReadOnlySet<string> Actions =
        new HashSet<string>(StringComparer.Ordinal) { "READ_OWN", "PREPARE_ONLY" };

    private static V5FunctionalAcceptanceDomainRules.VerifiedActor Trusted() =>
        new(7, 11, true, true, true, Sha);

    private static V5FunctionalAcceptanceDomainRules.AcceptanceEvidence Good() =>
        new("AF01", "READ_OWN", 11, Sha, "ci:acceptance:123",
            true, true, false, false, true, true);

    private static V5FunctionalAcceptanceApplicationService.Request Request(
        params V5FunctionalAcceptanceDomainRules.AcceptanceEvidence?[] items) =>
        new(11, items.Length == 0 ? new[] { Good() } : items);

    [Fact]
    public void AcceptedDomainInputsStillNeedExternalFunctionalProofAndA5()
    {
        var result = Service.Prepare(Trusted(),
            Request(Good(), Good() with { CaseKey = "AF02" }), Cases, Actions);
        Assert.Equal("EXTERNAL_ACCEPTANCE_EVIDENCE_AND_A5_REQUIRED", result.Code);
        Assert.Equal(2, result.ItemsChecked);
        Assert.True(result.DomainPreflightAccepted);
        Assert.False(result.AuthorizesCommercialAction);
        Assert.False(result.AuthorizesTenantAccess);
        Assert.False(result.CertifiesFunctionalAcceptance);
        Assert.False(result.AuthorizesRelease);
    }

    [Fact]
    public void NullRevokedOrUnprivilegedBackendActorCannotPrepare()
    {
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Service.Prepare(null, Request(), Cases, Actions).Code);
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Service.Prepare(Trusted() with { ActiveMembership = false },
                Request(), Cases, Actions).Code);
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Service.Prepare(Trusted() with { PersistedOwnershipVerified = false },
                Request(), Cases, Actions).Code);
        Assert.Equal("SERVER_VERIFIED_AUTHORITY_REQUIRED",
            Service.Prepare(Trusted() with { EffectiveActionPermission = false },
                Request(), Cases, Actions).Code);
    }

    [Fact]
    public void ClientClaimedTenantBIsNeverServerAuthority()
    {
        Assert.Equal("CROSS_TENANT_REQUEST_DENIED",
            Service.Prepare(Trusted(),
                Request() with { ClientRequestedTenantId = 22 }, Cases, Actions).Code);
        Assert.Equal("CROSS_TENANT_REQUEST_DENIED",
            Service.Prepare(Trusted(), null, Cases, Actions).Code);
    }

    [Fact]
    public void EmptyOrNullCaseAndActionCatalogsAreDenied()
    {
        Assert.Equal("TRUSTED_CASE_AND_ACTION_CATALOG_REQUIRED",
            Service.Prepare(Trusted(), Request(), null, Actions).Code);
        Assert.Equal("TRUSTED_CASE_AND_ACTION_CATALOG_REQUIRED",
            Service.Prepare(Trusted(), Request(), Cases, new HashSet<string>()).Code);
    }

    [Fact]
    public void EmptyMissingOrOversizedBatchesAreDenied()
    {
        Assert.Equal("ACCEPTANCE_BATCH_CAPACITY_INVALID",
            Service.Prepare(Trusted(), new(11, null), Cases, Actions).Code);
        Assert.Equal("ACCEPTANCE_BATCH_CAPACITY_INVALID",
            Service.Prepare(Trusted(), new(11,
                Array.Empty<V5FunctionalAcceptanceDomainRules.AcceptanceEvidence?>()),
                Cases, Actions).Code);
        Assert.Equal("ACCEPTANCE_BATCH_CAPACITY_INVALID",
            Service.Prepare(Trusted(), Request() with { MaximumItems = 65 }, Cases, Actions).Code);
        Assert.Equal("ACCEPTANCE_BATCH_CAPACITY_INVALID",
            Service.Prepare(Trusted(), Request(Good(), Good()) with
                { MaximumItems = 1 }, Cases, Actions).Code);
    }

    [Fact]
    public void FirstBadItemStopsReviewWithoutInspectingLaterItems()
    {
        var result = Service.Prepare(Trusted(),
            Request(Good(), null, Good()), Cases, Actions);
        Assert.Equal("NULL_ACCEPTANCE_EVIDENCE", result.Code);
        Assert.Equal(2, result.ItemsChecked);
        Assert.False(result.DomainPreflightAccepted);
        var b = Service.Prepare(Trusted(),
            Request(Good(), Good() with { ResourceTenantId = 22 }), Cases, Actions);
        Assert.Equal("CROSS_TENANT_OWNERSHIP_DENIED", b.Code);
        Assert.Equal(2, b.ItemsChecked);
    }

    [Fact]
    public void StaleCommitAndForgedCaseOrActionAreRejected()
    {
        Assert.Equal("EXACT_SOURCE_COMMIT_REQUIRED",
            Service.Prepare(Trusted(),
                Request(Good() with { SourceCommitSha = new string('e', 40) }),
                Cases, Actions).Code);
        Assert.Equal("UNAPPROVED_CASE_OR_ACTION",
            Service.Prepare(Trusted(),
                Request(Good() with { ActionKey = "CHARGE_CARD" }),
                Cases, Actions).Code);
    }

    [Fact]
    public void PrivateProductionOrMissingPositiveNegativeProofCannotPass()
    {
        Assert.Equal("UNREDACTED_PRIVATE_EVIDENCE",
            Service.Prepare(Trusted(),
                Request(Good() with { ContainsPrivateData = true }),
                Cases, Actions).Code);
        Assert.Equal("PRODUCTION_ACTION_NOT_AUTHORIZED",
            Service.Prepare(Trusted(),
                Request(Good() with { RequiresProductionAction = true }),
                Cases, Actions).Code);
        Assert.Equal("POSITIVE_AND_NEGATIVE_EVIDENCE_REQUIRED",
            Service.Prepare(Trusted(),
                Request(Good() with { NegativeScenarioPresent = false }),
                Cases, Actions).Code);
    }

    [Fact]
    public void MissingRollbackIdempotencyOrUnsafeReferenceFailsClosed()
    {
        Assert.Equal("ROLLBACK_OR_IDEMPOTENCY_GATE_MISSING",
            Service.Prepare(Trusted(),
                Request(Good() with { IdempotencyRiskControlled = false }),
                Cases, Actions).Code);
        Assert.Equal("ROLLBACK_OR_IDEMPOTENCY_GATE_MISSING",
            Service.Prepare(Trusted(),
                Request(Good() with { RollbackDefined = false }),
                Cases, Actions).Code);
        Assert.Equal("INVALID_EVIDENCE_REFERENCE",
            Service.Prepare(Trusted(),
                Request(Good() with { EvidenceReference = "credential\nunsafe" }),
                Cases, Actions).Code);
    }
}
