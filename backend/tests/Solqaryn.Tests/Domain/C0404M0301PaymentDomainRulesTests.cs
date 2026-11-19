using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0404PaymentDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class C0404M0301PaymentDomainRulesTests
{
    private const string Key = "opaque-stable-key-123456789";
    private static readonly string Digest = new('a', 64);
    private static Rules.VerifiedContext GoodContext() => new(
        100, 17, true, true, false, true, true, true, true, true);
    private static Rules.InvoiceSnapshot GoodInvoice() =>
        new(100, 77, "Emitida", 5, 120.50m, "HNL");
    private static Rules.IntentCommand GoodCommand() =>
        new(77, 5, 50.25m, "HNL", "provider_1", "CREATE_LINK", Key, Digest);

    [Fact]
    public void ValidInvoiceOnlyReachesBackendGateWithoutPermissionToCharge()
    {
        var verdict = Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand());
        Assert.Equal("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED", verdict.Code);
        Assert.False(verdict.AuthorizesCharge);
        Assert.False(verdict.AuthorizesProviderCall);
        Assert.False(verdict.AuthorizesPersistence);
        Assert.False(verdict.CertifiesJwtOrTenantIntegration);
    }

    [Fact]
    public void NullAndUnauthenticatedServerEvidenceFailsClosed()
    {
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED",
            Rules.Evaluate(null, GoodInvoice(), GoodCommand()).Code);
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED",
            Rules.Evaluate(GoodContext(), null, GoodCommand()).Code);
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED",
            Rules.Evaluate(GoodContext(), GoodInvoice(), null).Code);
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED",
            Rules.Evaluate(GoodContext() with { AuthenticatedActorId = null },
                GoodInvoice(), GoodCommand()).Code);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void RevokedInactiveOrUnprivilegedActorCannotPrepare(
        bool active, bool granted, bool revoked)
    {
        var context = GoodContext() with
        {
            MembershipActive = active,
            ExplicitPaymentRbacGrant = granted,
            PermissionRevoked = revoked
        };
        Assert.Equal("PAYMENT_PERMISSION_OR_MEMBERSHIP_DENIED",
            Rules.Evaluate(context, GoodInvoice(), GoodCommand()).Code);
    }

    [Fact]
    public void CrossTenantNullLegacyAndForgedInvoiceIdAreDenied()
    {
        Assert.Equal("INVOICE_TENANT_OR_OWNERSHIP_DENIED",
            Rules.Evaluate(GoodContext(), GoodInvoice() with { EmpresaId = 200 }, GoodCommand()).Code);
        Assert.Equal("INVOICE_TENANT_OR_OWNERSHIP_DENIED",
            Rules.Evaluate(GoodContext(), GoodInvoice() with { EmpresaId = null }, GoodCommand()).Code);
        Assert.Equal("INVOICE_TENANT_OR_OWNERSHIP_DENIED",
            Rules.Evaluate(GoodContext() with { InvoiceOwnershipServerVerified = false },
                GoodInvoice(), GoodCommand()).Code);
        Assert.Equal("INVOICE_TENANT_OR_OWNERSHIP_DENIED",
            Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand() with { FacturaId = 88 }).Code);
    }

    [Fact]
    public void StaleVersionAndNonPayableInvoiceAreDenied()
    {
        Assert.Equal("STALE_INVOICE_VERSION",
            Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand() with { ExpectedVersion = 4 }).Code);
        foreach (var state in new[] { "Pagada", "Anulada", "Borrador", "Vencida" })
            Assert.Equal("INVOICE_NOT_PAYABLE",
                Rules.Evaluate(GoodContext(), GoodInvoice() with { State = state }, GoodCommand()).Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(120.51)]
    [InlineData(50.251)]
    [InlineData(999999999.99)]
    public void InvalidAmountAndBoundariesAreDenied(decimal amount)
    {
        Assert.Equal("INVALID_SERVER_VERIFIED_AMOUNT",
            Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand() with { Amount = amount }).Code);
    }

    [Fact]
    public void UnverifiedServerAmountAndZeroOutstandingCannotPass()
    {
        Assert.Equal("INVALID_SERVER_VERIFIED_AMOUNT",
            Rules.Evaluate(GoodContext() with { AmountServerVerified = false },
                GoodInvoice(), GoodCommand()).Code);
        Assert.Equal("INVALID_SERVER_VERIFIED_AMOUNT",
            Rules.Evaluate(GoodContext(), GoodInvoice() with { Outstanding = 0m }, GoodCommand()).Code);
    }

    [Fact]
    public void CurrencyPolicyIsExactAndServerApproved()
    {
        Assert.Equal("CURRENCY_POLICY_DENIED",
            Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand() with { Currency = "USD" }).Code);
        Assert.Equal("CURRENCY_POLICY_DENIED",
            Rules.Evaluate(GoodContext(), GoodInvoice() with { Currency = "hnl" }, GoodCommand()).Code);
        Assert.Equal("CURRENCY_POLICY_DENIED",
            Rules.Evaluate(GoodContext() with { CurrencyServerApproved = false },
                GoodInvoice(), GoodCommand()).Code);
    }

    [Fact]
    public void ProviderWhitelistAndOperationEnforced()
    {
        Assert.Equal("PROVIDER_OR_OPERATION_NOT_APPROVED",
            Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand() with { ProviderId = "../evil" }).Code);
        Assert.Equal("PROVIDER_OR_OPERATION_NOT_APPROVED",
            Rules.Evaluate(GoodContext() with { ProviderApproved = false },
                GoodInvoice(), GoodCommand()).Code);
        Assert.Equal("PROVIDER_OR_OPERATION_NOT_APPROVED",
            Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand() with { Operation = "REFUND" }).Code);
    }

    [Fact]
    public void StableKeyAndServerCanonicalDigestAreRequired()
    {
        Assert.Equal("STABLE_IDEMPOTENCY_EVIDENCE_REQUIRED",
            Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand() with { IdempotencyKey = "" }).Code);
        Assert.Equal("STABLE_IDEMPOTENCY_EVIDENCE_REQUIRED",
            Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand() with { PayloadDigestCanonico = "abc" }).Code);
        Assert.Equal("STABLE_IDEMPOTENCY_EVIDENCE_REQUIRED",
            Rules.Evaluate(GoodContext() with { CanonicalDigestServerVerified = false },
                GoodInvoice(), GoodCommand()).Code);
    }

    [Fact]
    public void ExactReplayDoesNotPermitSecondCharge()
    {
        var existing = new Rules.ExistingAttempt(100, "provider_1", "CREATE_LINK", Key, Digest);
        var verdict = Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand(), existing);
        Assert.Equal("EXISTING_ATTEMPT_REQUIRES_FRESH_BACKEND_AUTHORIZATION", verdict.Code);
        Assert.False(verdict.AuthorizesCharge);
        Assert.False(verdict.AuthorizesPersistence);
    }

    [Fact]
    public void SameStableKeyWithDifferentPayloadRequiresConflictBeforeEffect()
    {
        var stored = new Rules.ExistingAttempt(100, "provider_1", "CREATE_LINK", Key, new string('f', 64));
        var verdict = Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand(), stored);
        Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT_409", verdict.Code);
        Assert.False(verdict.AuthorizesCharge);
        Assert.False(verdict.AuthorizesProviderCall);
    }

    [Fact]
    public void CrossTenantExistingAttemptCannotBeRevealedAsReplay()
    {
        var otherTenant = new Rules.ExistingAttempt(200, "provider_1", "CREATE_LINK", Key, Digest);
        Assert.Equal("EXISTING_ATTEMPT_IDENTITY_MISMATCH",
            Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand(), otherTenant).Code);
    }

    [Fact]
    public void SameKeyOtherProviderOrOperationDoesNotReuseWrongAttempt()
    {
        foreach (var existing in new[]
        {
            new Rules.ExistingAttempt(100, "other_provider", "CREATE_LINK", Key, Digest),
            new Rules.ExistingAttempt(100, "provider_1", "REFUND", Key, Digest)
        })
            Assert.Equal("EXISTING_ATTEMPT_IDENTITY_MISMATCH",
                Rules.Evaluate(GoodContext(), GoodInvoice(), GoodCommand(), existing).Code);
    }
}
