using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0404PaymentDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0404PaymentLinkApplicationService;
namespace Solqaryn.Tests.Application;

public sealed class C0404M0302PaymentLinkApplicationTests
{
    private readonly Service _sut = new();
    private const string Key = "idem-opaque-stable-key-00001";
    private static readonly string Digest = new('a', 64);
    private static Rules.VerifiedContext Context() => new(100, 71, true, true, false, true, true, true, true, true);
    private static Rules.InvoiceSnapshot Invoice() => new(100, 92, "Emitida", 3, 230m, "HNL");
    private static Rules.IntentCommand Command() => new(92, 3, 50.25m, "HNL", "provider_1", "CREATE_LINK", Key, Digest);
    private static Service.StoredAttempt Attempt(string state="Pending", string? last=null) =>
        new(100, 92, "provider_1", 50.25m, "HNL", state, last);
    private static Service.ProviderEvidence Evidence() =>
        new(100, 92, "provider_1", "evt-001", 50.25m, "HNL", "Settled", true, true, true, true, true, false);
    private static Service.RefundAuthority Refund() => new(100, 71, true, true, false, true, true, true);
    private static void NoEffects(Service.Decision d)
    {
        Assert.False(d.MayCallProvider);
        Assert.False(d.MayWriteLedger);
        Assert.False(d.MayChargeOrRefund);
        Assert.False(d.CertifiesLiveIntegration);
    }
    [Fact] public void ValidCreateStillNeedsAuthorizedDurableReservation()
    {
        var d = _sut.PlanCreate(Context(), Invoice(), Command());
        Assert.Equal(Service.NextStep.RequireDurableReservation, d.Next);
        NoEffects(d);
    }
    [Fact] public void ExactReplayNeverCreatesSecondCharge()
    {
        var d = _sut.PlanCreate(Context(), Invoice(), Command(),
            new Rules.ExistingAttempt(100, "provider_1", "CREATE_LINK", Key, Digest));
        Assert.Equal(Service.NextStep.ReadExistingOnly, d.Next);
        NoEffects(d);
    }
    [Fact] public void SameKeyChangedDigestMustConflict()
    {
        var d = _sut.PlanCreate(Context(), Invoice(), Command(),
            new Rules.ExistingAttempt(100, "provider_1", "CREATE_LINK", Key, new string('f', 64)));
        Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT_409", d.Code);
        Assert.Equal(Service.NextStep.Reject, d.Next);
    }
    [Fact] public void RevokedOrCrossTenantCreateRejected()
    {
        Assert.Equal(Service.NextStep.Reject, _sut.PlanCreate(Context() with { PermissionRevoked = true }, Invoice(), Command()).Next);
        Assert.Equal(Service.NextStep.Reject, _sut.PlanCreate(Context(), Invoice() with { EmpresaId = 200 }, Command()).Next);
    }
    [Fact] public void AmbiguousTimeoutNeverTriggersBlindRetry()
    {
        var d = _sut.PlanReconcile(Evidence() with { State = "Unknown", ProviderSettlementConfirmed = false }, Attempt("Ambiguous"));
        Assert.Equal(Service.NextStep.ReadExistingOnly, d.Next);
        NoEffects(d);
    }
    [Fact] public void VerifiedSettlementOnlyRequestsAtomicLedgerTransaction()
    {
        var d = _sut.PlanReconcile(Evidence(), Attempt());
        Assert.Equal(Service.NextStep.RequireAtomicReconciliation, d.Next);
        NoEffects(d);
    }
    [Theory]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public void UnsignedStaleUnverifiedOrRedirectDenied(bool hmac, bool fresh, bool reference, bool redirect)
    {
        var d = _sut.PlanReconcile(Evidence() with {
            HmacVerified = hmac, TimestampFresh = fresh,
            ProviderReferenceVerified = reference, IsBrowserRedirect = redirect
        }, Attempt());
        Assert.Equal(Service.NextStep.Reject, d.Next);
        NoEffects(d);
    }
    [Fact] public void TenantBWebhookCannotChangeTenantA()
    {
        Assert.Equal("TENANT_OR_REFERENCE_DENIED",
            _sut.PlanReconcile(Evidence() with { EmpresaId = 200 }, Attempt()).Code);
    }
    [Fact] public void AmountAndCurrencyMismatchDenied()
    {
        Assert.Equal(Service.NextStep.Reject, _sut.PlanReconcile(Evidence() with { Amount = 50.26m }, Attempt()).Next);
        Assert.Equal(Service.NextStep.Reject, _sut.PlanReconcile(Evidence() with { Currency = "USD" }, Attempt()).Next);
    }
    [Fact] public void SettledReplayIsNoEffectAndOutOfOrderDenied()
    {
        var replay = _sut.PlanReconcile(Evidence(), Attempt("Settled", "evt-001"));
        Assert.Equal(Service.NextStep.ReadExistingOnly, replay.Next);
        Assert.Equal("SETTLED_REPLAY_NO_EFFECT", replay.Code);
        Assert.Equal(Service.NextStep.Reject, _sut.PlanReconcile(
            Evidence() with { EventId = "evt-002" }, Attempt("Settled", "evt-001")).Next);
    }
    [Fact] public void RefundGrantRequiredButNeverExecutesRefund()
    {
        var d = _sut.PlanRefund(Refund(), Attempt("Settled"));
        Assert.Equal(Service.NextStep.RequireAuthorizedCompensation, d.Next);
        NoEffects(d);
        Assert.Equal(Service.NextStep.Reject, _sut.PlanRefund(Refund() with { RefundGrant = false }, Attempt("Settled")).Next);
        Assert.Equal(Service.NextStep.Reject, _sut.PlanRefund(Refund() with { Revoked = true }, Attempt("Settled")).Next);
    }
    [Fact] public void RefundForeignTenantAndNonSettledAreDenied()
    {
        Assert.Equal(Service.NextStep.Reject, _sut.PlanRefund(Refund() with { EmpresaId = 200 }, Attempt("Settled")).Next);
        Assert.Equal(Service.NextStep.Reject, _sut.PlanRefund(Refund(), Attempt("Ambiguous")).Next);
        Assert.Equal(Service.NextStep.Reject, _sut.PlanRefund(Refund() with { ReceiptVerified = false }, Attempt("Settled")).Next);
    }
}
