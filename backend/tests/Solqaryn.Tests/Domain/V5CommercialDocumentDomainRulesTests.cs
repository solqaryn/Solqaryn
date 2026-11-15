using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5CommercialDocumentDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class V5CommercialDocumentDomainRulesTests
{
    private static readonly string Fingerprint = new('a', 64);

    private static Rules.TrustedContext Context() => new(
        7, 11, new HashSet<Rules.Operation>(Enum.GetValues<Rules.Operation>()), true, true);

    private static Rules.PersistedSnapshot Snapshot(string state = "BORRADOR") =>
        new(7, 7, 7, state, "HNL", 125.50m, 4);

    private static Rules.Command Action(Rules.Operation operation = Rules.Operation.ConfirmOrder) =>
        new(operation, 4, 125.50m, "HNL", "pedido-7", Fingerprint);

    private static string Code(Rules.TrustedContext? context = null,
        Rules.PersistedSnapshot? snapshot = null, Rules.Command? command = null) =>
        Rules.Evaluate(context ?? Context(), snapshot ?? Snapshot(), command ?? Action()).Code;

    [Fact]
    public void ValidOrderOnlyAllowsFurtherBackendReview()
    {
        var verdict = Rules.Evaluate(Context(), Snapshot(), Action());
        Assert.Equal("VALID_FOR_BACKEND_TRANSACTION_REVIEW", verdict.Code);
        Assert.True(verdict.NeedsApplicationVerification);
    }

    [Fact]
    public void NullOrUnauthenticatedContextIsDenied()
    {
        Assert.Equal("TRUSTED_CONTEXT_REQUIRED", Rules.Evaluate(null, Snapshot(), Action()).Code);
        Assert.Equal("TRUSTED_CONTEXT_REQUIRED", Code(Context() with { AuthenticatedTenantId = null }));
        Assert.Equal("TRUSTED_CONTEXT_REQUIRED", Code(Context() with { AuthenticatedUserId = 0 }));
        Assert.Equal("TRUSTED_CONTEXT_REQUIRED", Code(Context() with { ServerGrantedOperations = null! }));
    }

    [Fact]
    public void PermissionsAreResolvedOnlyFromTrustedContext()
    {
        Assert.Equal("OPERATION_PERMISSION_REQUIRED",
            Code(Context() with { ServerGrantedOperations = new HashSet<Rules.Operation>() }));
        Assert.Equal("OPERATION_PERMISSION_REQUIRED",
            Code(command: Action() with { RequestedOperation = (Rules.Operation)999 }));
    }

    [Fact]
    public void TenantBAndLegacyNullOwnersNeverInheritTenantA()
    {
        Assert.Equal("TENANT_OWNERSHIP_MISMATCH", Code(snapshot: Snapshot() with { DocumentTenantId = 8 }));
        Assert.Equal("TENANT_OWNERSHIP_MISMATCH", Code(snapshot: Snapshot() with { CustomerTenantId = 8 }));
        Assert.Equal("TENANT_OWNERSHIP_MISMATCH", Code(snapshot: Snapshot() with { ParentTenantId = 8 }));
        Assert.Equal("TENANT_OWNERSHIP_MISMATCH", Code(snapshot: Snapshot() with { ParentTenantId = null }));
        Assert.Equal("TENANT_OWNERSHIP_MISMATCH", Code(snapshot: Snapshot() with { DocumentTenantId = null }));
    }

    [Fact]
    public void ConcurrentVersionAndAmountConflictsFailWithoutEffects()
    {
        Assert.Equal("STALE_DOCUMENT_VERSION", Code(command: Action() with { ExpectedVersion = 3 }));
        Assert.Equal("INVALID_DOCUMENT_AMOUNT", Code(command: Action() with { Amount = 125.51m }));
        Assert.Equal("INVALID_DOCUMENT_AMOUNT", Code(command: Action() with { Amount = 125.501m }));
        Assert.Equal("INVALID_DOCUMENT_AMOUNT", Code(snapshot: Snapshot() with { Amount = -1m }));
    }

    [Fact]
    public void CurrencyAndIdempotencyMustMatchDurableContract()
    {
        Assert.Equal("CURRENCY_MISMATCH", Code(command: Action() with { Currency = "USD" }));
        Assert.Equal("CURRENCY_MISMATCH", Code(snapshot: Snapshot() with { Currency = "hnl" }));
        Assert.Equal("INVALID_IDEMPOTENCY_CONTRACT", Code(command: Action() with { IdempotencyKey = "key space" }));
        Assert.Equal("INVALID_IDEMPOTENCY_CONTRACT", Code(command: Action() with { PayloadSha256 = new string('a', 63) }));
        Assert.Equal("INVALID_IDEMPOTENCY_CONTRACT", Code(command: Action() with { PayloadSha256 = new string('g', 64) }));
    }

    [Theory]
    [InlineData(Rules.Operation.ConfirmOrder, "CONFIRMADO")]
    [InlineData(Rules.Operation.IssueInvoice, "BORRADOR")]
    [InlineData(Rules.Operation.ApplyPayment, "CONFIRMADO")]
    [InlineData(Rules.Operation.CancelOrder, "ANULADO")]
    [InlineData(Rules.Operation.ExportDocument, "BORRADOR")]
    public void UnexpectedDocumentStatesAreRejected(Rules.Operation operation, string state) =>
        Assert.Equal("DOCUMENT_STATE_CONFLICT", Code(snapshot: Snapshot(state), command: Action(operation)));

    [Fact]
    public void FiscalAndPaymentServicesRequireSeparateVerifiedAuthority()
    {
        Assert.Equal("FISCAL_AUTHORITY_NOT_VERIFIED",
            Code(Context() with { FiscalIssuerVerified = false }, Snapshot("CONFIRMADO"),
                Action(Rules.Operation.IssueInvoice)));
        Assert.Equal("PAYMENT_PROVIDER_NOT_AUTHORIZED",
            Code(Context() with { PaymentProviderApproved = false }, Snapshot("EMITIDO"),
                Action(Rules.Operation.ApplyPayment)));
    }

    [Theory]
    [InlineData(Rules.Operation.IssueInvoice, "CONFIRMADO")]
    [InlineData(Rules.Operation.ApplyPayment, "EMITIDO")]
    [InlineData(Rules.Operation.CancelOrder, "CONFIRMADO")]
    [InlineData(Rules.Operation.ExportDocument, "EMITIDO")]
    public void ValidOtherOperationsStillRequireApplicationReview(Rules.Operation operation, string state) =>
        Assert.Equal("VALID_FOR_BACKEND_TRANSACTION_REVIEW",
            Code(snapshot: Snapshot(state), command: Action(operation)));
}
