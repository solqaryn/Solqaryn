using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5CommercialDocumentDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class V5CommercialDocumentApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";
    private static readonly V5CommercialDocumentApplicationService Service = new();

    private static Rules.TrustedContext Authority() =>
        new(7, 11, new HashSet<Rules.Operation>(Enum.GetValues<Rules.Operation>()), true, true);

    private static Rules.PersistedSnapshot Document(string state = "BORRADOR") =>
        new(7, 7, 7, state, "HNL", 125.50m, 4);

    private static Rules.Command Action(Rules.Operation operation = Rules.Operation.ConfirmOrder) =>
        new(operation, 4, 125.50m, "HNL", "order-7-v4", new string('a', 64));

    private static V5CommercialDocumentApplicationService.Request Valid() =>
        new(Sha, Sha, "git-evidence:commercial-readonly", Authority(), Document(), Action());

    [Fact]
    public void ValidPreflightNeverAuthorizesBusinessEffects()
    {
        var result = Service.Prepare(Valid());
        Assert.Equal("AUTHENTICATED_COMMERCIAL_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsAuthenticatedAdapter);
        Assert.False(result.GrantsCommercialMutation);
        Assert.False(result.GrantsPaymentOrFiscalIssuance);
        Assert.False(result.GrantsDocumentExport);
        Assert.False(result.GrantsCrossTenantRead);
    }

    [Fact]
    public void NullRequestAndStaleOrMalformedSourceShaFailClosed()
    {
        Assert.Equal("COMMERCIAL_REQUEST_REQUIRED", Service.Prepare(null).Code);
        Assert.Equal("COMMERCIAL_SOURCE_SHA_UNVERIFIED",
            Service.Prepare(Valid() with { ExpectedSourceSha = null }).Code);
        Assert.Equal("COMMERCIAL_SOURCE_SHA_UNVERIFIED",
            Service.Prepare(Valid() with { ObservedSourceSha = OtherSha }).Code);
        Assert.Equal("COMMERCIAL_SOURCE_SHA_UNVERIFIED",
            Service.Prepare(Valid() with { ObservedSourceSha = "not-a-sha" }).Code);
    }

    [Fact]
    public void ReferenceCannotContainUrlEmailOrUnboundedData()
    {
        Assert.Equal("COMMERCIAL_EVIDENCE_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with { SanitizedEvidenceReference = null }).Code);
        Assert.Equal("COMMERCIAL_EVIDENCE_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with { SanitizedEvidenceReference = "https://internal.local" }).Code);
        Assert.Equal("COMMERCIAL_EVIDENCE_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with { SanitizedEvidenceReference = "a@private" }).Code);
        Assert.Equal("COMMERCIAL_EVIDENCE_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with { SanitizedEvidenceReference = new string('x', 161) }).Code);
    }

    [Fact]
    public void MissingGrantsAndCrossTenantOrLegacyOwnershipAreDenied()
    {
        Assert.Equal("OPERATION_PERMISSION_REQUIRED",
            Service.Prepare(Valid() with
            {
                Authority = Authority() with { ServerGrantedOperations = new HashSet<Rules.Operation>() }
            }).Code);
        Assert.Equal("TENANT_OWNERSHIP_MISMATCH",
            Service.Prepare(Valid() with { Document = Document() with { CustomerTenantId = 8 } }).Code);
        Assert.Equal("TENANT_OWNERSHIP_MISMATCH",
            Service.Prepare(Valid() with { Document = Document() with { ParentTenantId = null } }).Code);
    }

    [Fact]
    public void RevokedContextAndStaleVersionCannotReachAdapter()
    {
        Assert.Equal("TRUSTED_CONTEXT_REQUIRED",
            Service.Prepare(Valid() with
            {
                Authority = Authority() with { AuthenticatedUserId = 0 }
            }).Code);
        Assert.Equal("STALE_DOCUMENT_VERSION",
            Service.Prepare(Valid() with { Command = Action() with { ExpectedVersion = 3 } }).Code);
    }

    [Fact]
    public void CurrencyAmountFingerprintAndStateMismatchAreDenied()
    {
        Assert.Equal("CURRENCY_MISMATCH",
            Service.Prepare(Valid() with { Command = Action() with { Currency = "USD" } }).Code);
        Assert.Equal("INVALID_DOCUMENT_AMOUNT",
            Service.Prepare(Valid() with { Command = Action() with { Amount = 125.51m } }).Code);
        Assert.Equal("INVALID_IDEMPOTENCY_CONTRACT",
            Service.Prepare(Valid() with { Command = Action() with { PayloadSha256 = "bad" } }).Code);
        Assert.Equal("DOCUMENT_STATE_CONFLICT",
            Service.Prepare(Valid() with { Document = Document("ANULADO") }).Code);
    }

    [Fact]
    public void FiscalAndProviderFlagsStillDoNotIssueOrCharge()
    {
        var invoice = Valid() with
        {
            Document = Document("CONFIRMADO"),
            Command = Action(Rules.Operation.IssueInvoice)
        };
        Assert.Equal("FISCAL_AUTHORITY_NOT_VERIFIED",
            Service.Prepare(invoice with { Authority = Authority() with { FiscalIssuerVerified = false } }).Code);
        Assert.Equal("AUTHENTICATED_COMMERCIAL_ADAPTER_REQUIRED", Service.Prepare(invoice).Code);

        var pay = Valid() with
        {
            Document = Document("EMITIDO"),
            Command = Action(Rules.Operation.ApplyPayment)
        };
        Assert.Equal("PAYMENT_PROVIDER_NOT_AUTHORIZED",
            Service.Prepare(pay with { Authority = Authority() with { PaymentProviderApproved = false } }).Code);
        var good = Service.Prepare(pay);
        Assert.True(good.NeedsAuthenticatedAdapter);
        Assert.False(good.GrantsPaymentOrFiscalIssuance);
    }
}
