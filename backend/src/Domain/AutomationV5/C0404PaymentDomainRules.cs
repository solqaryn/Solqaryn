namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C04-04.M03.01: pure, fail-closed payment-domain preflight.
/// All snapshots must originate from authenticated server-side resolution.
/// This does not implement or authorize a PSP, charge, webhook, database write or HTTP endpoint.
/// </summary>
public static class C0404PaymentDomainRules
{
    public sealed record VerifiedContext(
        int? AuthenticatedEmpresaId,
        int? AuthenticatedActorId,
        bool MembershipActive,
        bool ExplicitPaymentRbacGrant,
        bool PermissionRevoked,
        bool InvoiceOwnershipServerVerified,
        bool AmountServerVerified,
        bool CurrencyServerApproved,
        bool ProviderApproved,
        bool CanonicalDigestServerVerified);

    public sealed record InvoiceSnapshot(
        int? EmpresaId,
        int FacturaId,
        string? State,
        long Version,
        decimal Outstanding,
        string? Currency);

    public sealed record IntentCommand(
        int FacturaId,
        long ExpectedVersion,
        decimal Amount,
        string? Currency,
        string? ProviderId,
        string? Operation,
        string? IdempotencyKey,
        string? PayloadDigestCanonico);

    public sealed record ExistingAttempt(
        int EmpresaId,
        string ProviderId,
        string Operation,
        string IdempotencyKey,
        string PayloadDigestCanonico);

    public readonly record struct Decision(string Code)
    {
        public bool AuthorizesCharge => false;
        public bool AuthorizesProviderCall => false;
        public bool AuthorizesPersistence => false;
        public bool CertifiesJwtOrTenantIntegration => false;
    }

    public static Decision Evaluate(
        VerifiedContext? context, InvoiceSnapshot? invoice,
        IntentCommand? command, ExistingAttempt? existing = null)
    {
        if (context is null || invoice is null || command is null ||
            context.AuthenticatedEmpresaId is not > 0 ||
            context.AuthenticatedActorId is not > 0)
            return Deny("VERIFIED_SERVER_CONTEXT_REQUIRED");

        if (!context.MembershipActive || context.PermissionRevoked ||
            !context.ExplicitPaymentRbacGrant)
            return Deny("PAYMENT_PERMISSION_OR_MEMBERSHIP_DENIED");

        if (invoice.EmpresaId is not > 0 ||
            invoice.EmpresaId != context.AuthenticatedEmpresaId ||
            !context.InvoiceOwnershipServerVerified ||
            command.FacturaId <= 0 || command.FacturaId != invoice.FacturaId)
            return Deny("INVOICE_TENANT_OR_OWNERSHIP_DENIED");

        if (invoice.Version <= 0 || command.ExpectedVersion != invoice.Version)
            return Deny("STALE_INVOICE_VERSION");

        if (invoice.State is not ("Emitida" or "ParcialmentePagada"))
            return Deny("INVOICE_NOT_PAYABLE");

        if (!context.AmountServerVerified || invoice.Outstanding <= 0m ||
            command.Amount <= 0m || command.Amount > invoice.Outstanding ||
            command.Amount > 999999999.99m ||
            decimal.Round(command.Amount, 2) != command.Amount)
            return Deny("INVALID_SERVER_VERIFIED_AMOUNT");

        if (!context.CurrencyServerApproved ||
            !SafeCurrency(invoice.Currency) ||
            !string.Equals(invoice.Currency, command.Currency, StringComparison.Ordinal))
            return Deny("CURRENCY_POLICY_DENIED");

        if (!context.ProviderApproved || !SafeCode(command.ProviderId) ||
            !string.Equals(command.Operation, "CREATE_LINK", StringComparison.Ordinal))
            return Deny("PROVIDER_OR_OPERATION_NOT_APPROVED");

        if (!SafeKey(command.IdempotencyKey) ||
            !context.CanonicalDigestServerVerified ||
            !SafeDigest(command.PayloadDigestCanonico))
            return Deny("STABLE_IDEMPOTENCY_EVIDENCE_REQUIRED");

        if (existing is not null)
        {
            if (existing.EmpresaId != context.AuthenticatedEmpresaId ||
                !string.Equals(existing.ProviderId, command.ProviderId, StringComparison.Ordinal) ||
                !string.Equals(existing.Operation, command.Operation, StringComparison.Ordinal) ||
                !string.Equals(existing.IdempotencyKey, command.IdempotencyKey, StringComparison.Ordinal))
                return Deny("EXISTING_ATTEMPT_IDENTITY_MISMATCH");

            if (!string.Equals(existing.PayloadDigestCanonico,
                    command.PayloadDigestCanonico, StringComparison.OrdinalIgnoreCase))
                return Deny("IDEMPOTENCY_PAYLOAD_CONFLICT_409");

            return Deny("EXISTING_ATTEMPT_REQUIRES_FRESH_BACKEND_AUTHORIZATION");
        }

        // Pure preconditions, never a payment authorization. Application/API must
        // repeat JWT/RBAC/tenant checks and persist a durable UNIQUE identity
        // before making any external provider call. No charge is possible here.
        return Deny("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED");
    }

    private static Decision Deny(string code) => new(code);
    private static bool SafeCurrency(string? s) =>
        s is { Length: 3 } && s.All(c => c >= 'A' && c <= 'Z');

    private static bool SafeCode(string? s) =>
        s is { Length: >= 2 and <= 64 } &&
        s.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_');

    private static bool SafeKey(string? s) =>
        s is { Length: >= 16 and <= 128 } &&
        s.All(c => c is >= '!' and <= '~');

    private static bool SafeDigest(string? s) =>
        s is { Length: 64 } && s.All(Uri.IsHexDigit);
}
