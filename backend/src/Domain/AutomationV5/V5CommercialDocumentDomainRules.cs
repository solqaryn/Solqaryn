namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// Pure, fail-closed domain preflight for C07-04.M03.01 commercial documents.
/// Trusted context and persisted ownership must be resolved by authenticated backend code.
/// This class cannot authenticate, charge, emit fiscal documents, persist, or authorize release.
/// </summary>
public static class V5CommercialDocumentDomainRules
{
    public enum Operation { ConfirmOrder, IssueInvoice, ApplyPayment, CancelOrder, ExportDocument }

    public sealed record TrustedContext(
        int? AuthenticatedTenantId,
        int? AuthenticatedUserId,
        IReadOnlySet<Operation> ServerGrantedOperations,
        bool FiscalIssuerVerified,
        bool PaymentProviderApproved);

    public sealed record PersistedSnapshot(
        int? DocumentTenantId,
        int? CustomerTenantId,
        int? ParentTenantId,
        string State,
        string Currency,
        decimal Amount,
        long Version);

    public sealed record Command(
        Operation RequestedOperation,
        long ExpectedVersion,
        decimal Amount,
        string Currency,
        string IdempotencyKey,
        string PayloadSha256);

    public readonly record struct Verdict(string Code)
    {
        // A positive verdict only allows an application service to perform its
        // independent auth, transaction, PSP/fiscal and concurrency checks.
        public bool NeedsApplicationVerification => Code == "VALID_FOR_BACKEND_TRANSACTION_REVIEW";
    }

    public static Verdict Evaluate(TrustedContext? context, PersistedSnapshot? snapshot, Command? command)
    {
        if (context is null || snapshot is null || command is null ||
            context.AuthenticatedTenantId is not > 0 ||
            context.AuthenticatedUserId is not > 0 ||
            context.ServerGrantedOperations is null)
            return new Verdict("TRUSTED_CONTEXT_REQUIRED");

        if (!Enum.IsDefined(command.RequestedOperation) ||
            !context.ServerGrantedOperations.Contains(command.RequestedOperation))
            return new Verdict("OPERATION_PERMISSION_REQUIRED");

        // NULL legacy ownership remains quarantined. Tenant IDs in a request
        // are never used as an authority; these snapshots must come from persistence.
        var tenant = context.AuthenticatedTenantId.Value;
        if (snapshot.DocumentTenantId is not > 0 ||
            snapshot.CustomerTenantId is not > 0 ||
            snapshot.ParentTenantId is not > 0 ||
            snapshot.DocumentTenantId != tenant ||
            snapshot.CustomerTenantId != tenant ||
            snapshot.ParentTenantId != tenant)
            return new Verdict("TENANT_OWNERSHIP_MISMATCH");

        if (snapshot.Version <= 0 || command.ExpectedVersion != snapshot.Version)
            return new Verdict("STALE_DOCUMENT_VERSION");

        if (snapshot.Amount <= 0m || command.Amount <= 0m ||
            snapshot.Amount > 999999999.99m ||
            command.Amount != snapshot.Amount ||
            decimal.Round(command.Amount, 2) != command.Amount)
            return new Verdict("INVALID_DOCUMENT_AMOUNT");

        if (string.IsNullOrEmpty(snapshot.Currency) ||
            snapshot.Currency.Length != 3 ||
            !snapshot.Currency.All(c => c >= 'A' && c <= 'Z') ||
            !string.Equals(snapshot.Currency, command.Currency, StringComparison.Ordinal))
            return new Verdict("CURRENCY_MISMATCH");

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) ||
            command.IdempotencyKey.Length > 128 ||
            !command.IdempotencyKey.All(c => c >= '!' && c <= '~') ||
            string.IsNullOrEmpty(command.PayloadSha256) ||
            command.PayloadSha256.Length != 64 ||
            !command.PayloadSha256.All(Uri.IsHexDigit))
            return new Verdict("INVALID_IDEMPOTENCY_CONTRACT");

        var requiredState = command.RequestedOperation switch
        {
            Operation.ConfirmOrder => "BORRADOR",
            Operation.IssueInvoice => "CONFIRMADO",
            Operation.ApplyPayment => "EMITIDO",
            Operation.CancelOrder => "CONFIRMADO",
            Operation.ExportDocument => "EMITIDO",
            _ => string.Empty
        };
        if (!string.Equals(snapshot.State, requiredState, StringComparison.Ordinal))
            return new Verdict("DOCUMENT_STATE_CONFLICT");

        if (command.RequestedOperation == Operation.IssueInvoice && !context.FiscalIssuerVerified)
            return new Verdict("FISCAL_AUTHORITY_NOT_VERIFIED");

        if (command.RequestedOperation == Operation.ApplyPayment && !context.PaymentProviderApproved)
            return new Verdict("PAYMENT_PROVIDER_NOT_AUTHORIZED");

        // No mutation, no payments, no fiscal issuance, no event dispatch.
        return new Verdict("VALID_FOR_BACKEND_TRANSACTION_REVIEW");
    }
}
