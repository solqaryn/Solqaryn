using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0404PaymentDomainRules;

namespace Solqaryn.Tests.Application;

/// <summary>C04-04.M05.01 pure rule tests: no real payments or providers.</summary>
public sealed class C0404M0501PaymentDomainUnitTests
{
    private static readonly string Digest = new('a', 64);
    private const string Key = "c0404-stable-idempotency-00001";
    private static Rules.VerifiedContext C() => new(101, 7, true, true, false, true, true, true, true, true);
    private static Rules.InvoiceSnapshot I() => new(101, 99, "Emitida", 4, 100m, "HNL");
    private static Rules.IntentCommand X() => new(99, 4, 25.25m, "HNL", "provider_1", "CREATE_LINK", Key, Digest);
    private static Rules.Decision Check(Rules.VerifiedContext? c = null, Rules.InvoiceSnapshot? i = null,
        Rules.IntentCommand? x = null, Rules.ExistingAttempt? old = null) =>
        Rules.Evaluate(c ?? C(), i ?? I(), x ?? X(), old);
    private static void NoEffects(Rules.Decision d)
    {
        Assert.False(d.AuthorizesCharge);
        Assert.False(d.AuthorizesProviderCall);
        Assert.False(d.AuthorizesPersistence);
        Assert.False(d.CertifiesJwtOrTenantIntegration);
    }

    [Fact] public void ValidRuleOnlyAllowsBackendPreflightNotCharge()
    {
        var d = Check();
        Assert.Equal("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED", d.Code);
        Assert.Equal(d.Code, Check().Code);
        NoEffects(d);
    }

    [Fact] public void MissingServerContextMustReject()
    {
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED", Rules.Evaluate(null, I(), X()).Code);
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED", Rules.Evaluate(C(), null, X()).Code);
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED", Rules.Evaluate(C(), I(), null).Code);
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED", Check(c: C() with { AuthenticatedActorId = null }).Code);
    }

    [Fact] public void TenantAAndBAndOwnershipCannotMix()
    {
        Assert.Equal("INVOICE_TENANT_OR_OWNERSHIP_DENIED", Check(c: C() with { AuthenticatedEmpresaId = 202 }).Code);
        Assert.Equal("INVOICE_TENANT_OR_OWNERSHIP_DENIED", Check(c: C() with { InvoiceOwnershipServerVerified = false }).Code);
        Assert.Equal("INVOICE_TENANT_OR_OWNERSHIP_DENIED", Check(x: X() with { FacturaId = 98 }).Code);
    }

    [Fact] public void RevokedMissingGrantOrInactiveMemberDenied()
    {
        Assert.Equal("PAYMENT_PERMISSION_OR_MEMBERSHIP_DENIED", Check(c: C() with { PermissionRevoked = true }).Code);
        Assert.Equal("PAYMENT_PERMISSION_OR_MEMBERSHIP_DENIED", Check(c: C() with { MembershipActive = false }).Code);
        Assert.Equal("PAYMENT_PERMISSION_OR_MEMBERSHIP_DENIED", Check(c: C() with { ExplicitPaymentRbacGrant = false }).Code);
    }

    [Fact] public void StaleVersionAndCancelledInvoiceDenied()
    {
        Assert.Equal("STALE_INVOICE_VERSION", Check(x: X() with { ExpectedVersion = 3 }).Code);
        Assert.Equal("INVOICE_NOT_PAYABLE", Check(i: I() with { State = "Anulada" }).Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void AmountOutsideInvoiceBoundariesDenied(int amount)
    {
        var d = Check(x: X() with { Amount = amount });
        Assert.Equal("INVALID_SERVER_VERIFIED_AMOUNT", d.Code);
        NoEffects(d);
    }

    [Fact] public void FractionalPrecisionAndAbsoluteAmountCapDenied()
    {
        Assert.Equal("INVALID_SERVER_VERIFIED_AMOUNT", Check(x: X() with { Amount = 25.251m }).Code);
        Assert.Equal("INVALID_SERVER_VERIFIED_AMOUNT",
            Check(i: I() with { Outstanding = 1000000000m }, x: X() with { Amount = 1000000000m }).Code);
    }

    [Fact] public void CurrencyCaseAndServerApprovalStrict()
    {
        Assert.Equal("CURRENCY_POLICY_DENIED", Check(x: X() with { Currency = "hnl" }).Code);
        Assert.Equal("CURRENCY_POLICY_DENIED", Check(x: X() with { Currency = "USD" }).Code);
        Assert.Equal("CURRENCY_POLICY_DENIED", Check(c: C() with { CurrencyServerApproved = false }).Code);
    }

    [Fact] public void ProviderAndRefundOperationCannotBeSmuggled()
    {
        Assert.Equal("PROVIDER_OR_OPERATION_NOT_APPROVED", Check(x: X() with { ProviderId = "https://evil.invalid" }).Code);
        Assert.Equal("PROVIDER_OR_OPERATION_NOT_APPROVED", Check(x: X() with { Operation = "REFUND" }).Code);
    }

    [Fact] public void IdempotencyRequiresBoundedKeyAndHexDigest()
    {
        Assert.Equal("STABLE_IDEMPOTENCY_EVIDENCE_REQUIRED", Check(x: X() with { IdempotencyKey = "short" }).Code);
        Assert.Equal("STABLE_IDEMPOTENCY_EVIDENCE_REQUIRED", Check(x: X() with { IdempotencyKey = new string('k', 129) }).Code);
        Assert.Equal("STABLE_IDEMPOTENCY_EVIDENCE_REQUIRED", Check(x: X() with { PayloadDigestCanonico = new string('z', 64) }).Code);
    }

    [Fact] public void ReplayIsReadOnlyAndConflictingPayloadIs409()
    {
        var old = new Rules.ExistingAttempt(101, "provider_1", "CREATE_LINK", Key, Digest);
        var same = Check(old: old);
        Assert.Equal("EXISTING_ATTEMPT_REQUIRES_FRESH_BACKEND_AUTHORIZATION", same.Code);
        NoEffects(same);
        Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT_409",
            Check(old: old with { PayloadDigestCanonico = new string('f', 64) }).Code);
    }

    [Fact] public void ReplayFromOtherTenantMustReject()
    {
        var old = new Rules.ExistingAttempt(202, "provider_1", "CREATE_LINK", Key, Digest);
        var d = Check(old: old);
        Assert.Equal("EXISTING_ATTEMPT_IDENTITY_MISMATCH", d.Code);
        NoEffects(d);
    }
}
