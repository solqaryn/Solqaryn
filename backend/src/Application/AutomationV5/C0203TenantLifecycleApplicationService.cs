using Rules = Solqaryn.Domain.AutomationV5.V5TenantLifecycleDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>Server-controlled, tenant-and-operation-fenced evidence reader. No live adapter
/// is registered by C02-03.M03.02.</summary>
public interface IC0203LifecycleEvidenceReader
{
    ValueTask<C0203TenantLifecycleApplicationService.TrustedReadback?> ReadAsync(
        string tenantId, Rules.Command operation, CancellationToken cancellationToken);
}

/// <summary>Non-authorizing Application preflight. Never grants or writes.</summary>
public sealed class C0203TenantLifecycleApplicationService
{
    private readonly IC0203LifecycleEvidenceReader _reader;
    public C0203TenantLifecycleApplicationService(IC0203LifecycleEvidenceReader reader) =>
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    // Client intent cannot provide any trusted JWT/actor/owner/receipt snapshot.
    public sealed record Intent(
        string? TenantId, Rules.Command Operation, Rules.State ExpectedSourceState,
        long ExpectedRevision, string? IdempotencyKey, string? PayloadFingerprint,
        bool ClientClaimsAuthority = false, bool ClientSuppliesDsn = false,
        bool RequestsPhysicalDeletion = false, bool RequestsAutomaticGrants = false,
        bool RequestsProductionCutover = false);

    public sealed record TrustedReadback(
        Rules.TrustedPlatformEvidence? Platform, Rules.ExistingDurableReceipt? Receipt,
        bool ServerControlledReader, bool FreshSessionAndMembership,
        bool TenantFencedReceiptLookup, Rules.Command VerifiedOperation);

    public readonly record struct Outcome(string Code)
    {
        public bool RequiresDurableApplicationAdapter =>
            Code is "DURABLE_LIFECYCLE_CAS_ADAPTER_REQUIRED"
                 or "DURABLE_IDEMPOTENCY_READBACK_REQUIRED"
                 or "INCOMPLETE_REPLAY_REQUIRES_CHECKPOINT_RECOVERY";
        public bool AuthorizesTenantMutation => false;
        public bool GrantsGlobalPermission => false;
        public bool WritesToDatabase => false;
        public bool AuthorizesProductionCutover => false;
    }

    private static bool ValidId(string? id) =>
        id is { Length: >= 1 and <= 128 } &&
        (id.Length > 1 || id[0] is >= '1' and <= '9') &&
        id.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_');

    public async ValueTask<Outcome> InspectAsync(
        Intent? intent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (intent is null || !ValidId(intent.TenantId) || intent.ExpectedRevision <= 0 ||
            !Enum.IsDefined(intent.Operation) || !Enum.IsDefined(intent.ExpectedSourceState))
            return new("INVALID_LIFECYCLE_INTENT");
        if (intent.ClientClaimsAuthority || intent.ClientSuppliesDsn ||
            intent.RequestsPhysicalDeletion || intent.RequestsAutomaticGrants ||
            intent.RequestsProductionCutover)
            return new("UNTRUSTED_LIFECYCLE_REQUEST");

        TrustedReadback? evidence;
        try
        {
            evidence = await _reader.ReadAsync(intent.TenantId!, intent.Operation, cancellationToken);
            // A provider may ignore cancellation and return a stale positive snapshot.
            // It must not enable a preflight after its request has been cancelled.
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Sanitized error: never disclose SQL, token, tenant, provider text.
            return new("LIFECYCLE_EVIDENCE_SOURCE_UNAVAILABLE");
        }

        if (evidence?.Platform is null || !evidence.ServerControlledReader ||
            !evidence.FreshSessionAndMembership || !evidence.TenantFencedReceiptLookup ||
            !Enum.IsDefined(evidence.VerifiedOperation) ||
            evidence.VerifiedOperation != intent.Operation ||
            !string.Equals(evidence.Platform.VerifiedTargetTenantId,
                intent.TenantId, StringComparison.Ordinal))
            return new("TRUSTED_LIFECYCLE_EVIDENCE_REQUIRED");

        var command = new Rules.ProposedLifecycleCommand
        {
            Operation = intent.Operation,
            RequestedTenantId = intent.TenantId,
            ExpectedSourceState = intent.ExpectedSourceState,
            ExpectedRevision = intent.ExpectedRevision,
            IdempotencyKey = intent.IdempotencyKey,
            PayloadFingerprint = intent.PayloadFingerprint,
            PreservesDataAndAuditHistory = true
        };
        Rules.Verdict verdict = Rules.Evaluate(evidence.Platform, command, evidence.Receipt);
        // Positive Domain preflight cannot replace durable JWT/RBAC/tenant
        // checks, an audited idempotent transaction, or an actual CAS.
        return new(verdict.Code == "VERIFIED_PRECONDITIONS_REQUIRE_APPLICATION_CAS"
            ? "DURABLE_LIFECYCLE_CAS_ADAPTER_REQUIRED" : verdict.Code);
    }
}
