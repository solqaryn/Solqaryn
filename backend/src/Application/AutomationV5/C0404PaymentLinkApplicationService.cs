using DomainRules = Solqaryn.Domain.AutomationV5.C0404PaymentDomainRules;
namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C04-04.M03.02: pure provider-neutral application decisions. Not connected to
/// DI/API/PSP/ledger. No returned decision executes or authorizes a financial effect.
/// Runtime adapters must reverify JWT/RBAC, tenant, durable UNIQUE and HMAC.
/// </summary>
public sealed class C0404PaymentLinkApplicationService
{
    public enum NextStep { Reject, RequireDurableReservation, ReadExistingOnly, RequireAtomicReconciliation, RequireAuthorizedCompensation }
    public readonly record struct Decision(string Code, NextStep Next)
    {
        public bool MayCallProvider => false;
        public bool MayWriteLedger => false;
        public bool MayChargeOrRefund => false;
        public bool CertifiesLiveIntegration => false;
    }
    public sealed record StoredAttempt(int EmpresaId, int FacturaId, string Provider, decimal Amount, string Currency, string State, string? LastEventId);
    public sealed record ProviderEvidence(int EmpresaId, int FacturaId, string Provider, string EventId, decimal Amount, string Currency,
        string State, bool HmacVerified, bool TimestampFresh, bool ProviderReferenceVerified, bool OwnershipVerified,
        bool ProviderSettlementConfirmed, bool IsBrowserRedirect);
    public sealed record RefundAuthority(int? EmpresaId, int? ActorId, bool MembershipActive, bool RefundGrant,
        bool Revoked, bool ServerOwnershipVerified, bool ReceiptVerified, bool LedgerVersionVerified);

    public Decision PlanCreate(DomainRules.VerifiedContext? server, DomainRules.InvoiceSnapshot? invoice,
        DomainRules.IntentCommand? command, DomainRules.ExistingAttempt? existing = null)
    {
        var code = DomainRules.Evaluate(server, invoice, command, existing).Code;
        return code switch
        {
            "DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED" =>
                new("DURABLE_UNIQUE_RESERVATION_AND_AUTHORIZATION_REQUIRED", NextStep.RequireDurableReservation),
            "EXISTING_ATTEMPT_REQUIRES_FRESH_BACKEND_AUTHORIZATION" =>
                new("REPLAY_READ_EXISTING_AFTER_FRESH_AUTHORIZATION", NextStep.ReadExistingOnly),
            _ => new(code, NextStep.Reject)
        };
    }

    public Decision PlanReconcile(ProviderEvidence? verified, StoredAttempt? original)
    {
        if (verified is null || original is null) return Deny("TRUSTED_EVIDENCE_REQUIRED");
        if (verified.EmpresaId <= 0 || verified.FacturaId <= 0 || original.EmpresaId != verified.EmpresaId ||
            original.FacturaId != verified.FacturaId || original.Provider != verified.Provider || !verified.OwnershipVerified)
            return Deny("TENANT_OR_REFERENCE_DENIED");
        if (!verified.HmacVerified || !verified.TimestampFresh || !verified.ProviderReferenceVerified || verified.IsBrowserRedirect)
            return Deny("UNTRUSTED_CALLBACK_OR_REDIRECT_DENIED");
        if (string.IsNullOrWhiteSpace(verified.EventId) || verified.EventId.Length > 128 || verified.Amount <= 0 ||
            original.Amount != verified.Amount || original.Currency != verified.Currency)
            return Deny("RECONCILIATION_PAYLOAD_MISMATCH");
        if (original.State == "Settled")
            return original.LastEventId == verified.EventId
                ? new("SETTLED_REPLAY_NO_EFFECT", NextStep.ReadExistingOnly)
                : Deny("SETTLED_IMMUTABLE");
        if (original.State is not ("Pending" or "Ambiguous")) return Deny("STATE_NOT_RECONCILABLE");
        if (!verified.ProviderSettlementConfirmed || verified.State != "Settled")
            return new("AMBIGUOUS_REQUIRE_PROVIDER_QUERY_NO_RECHARGE", NextStep.ReadExistingOnly);
        return new("VERIFIED_RECEIPT_REQUIRES_ATOMIC_LEDGER_RECONCILIATION", NextStep.RequireAtomicReconciliation);
    }

    public Decision PlanRefund(RefundAuthority? authority, StoredAttempt? original)
    {
        if (authority is null || original is null || authority.EmpresaId is not > 0 || authority.ActorId is not > 0 ||
            !authority.MembershipActive || !authority.RefundGrant || authority.Revoked)
            return Deny("REFUND_AUTHORIZATION_DENIED");
        if (original.EmpresaId != authority.EmpresaId || !authority.ServerOwnershipVerified)
            return Deny("REFUND_TENANT_DENIED");
        if (!authority.ReceiptVerified || !authority.LedgerVersionVerified || original.State != "Settled")
            return Deny("REFUND_LEDGER_EVIDENCE_REQUIRED");
        return new("APPEND_ONLY_COMPENSATION_REQUIRES_AUTHORIZED_TRANSACTION", NextStep.RequireAuthorizedCompensation);
    }
    private static Decision Deny(string code) => new(code, NextStep.Reject);
}
