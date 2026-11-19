using Rules = Solqaryn.Domain.AutomationV5.C1103OutboxEventDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C11-03.M03.02. A bounded application use case. Its plans NEVER authorize
/// persistence, provider delivery or a durable ACK. No DI/API runtime binding.
/// </summary>
public interface IC1103OutboxEvidenceReader
{
    ValueTask<C1103OutboxApplicationService.TrustedEvidence?> ReadAsync(
        int requestedTenant, Guid eventId, string opaqueKey, CancellationToken cancellationToken);
}

public sealed class C1103OutboxApplicationService
{
    private readonly IC1103OutboxEvidenceReader _reader;

    public C1103OutboxApplicationService(IC1103OutboxEvidenceReader reader)
        => _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    /// <summary>
    /// A future authenticated backend adapter must construct this snapshot.
    /// These booleans do not themselves prove live JWT/RBAC/HTTP integration.
    /// </summary>
    public sealed record TrustedEvidence(
        Rules.VerifiedServerContext? Context,
        Rules.ExistingIntent? Existing,
        int AuthenticatedTenantId,
        bool FromTrustedBackend,
        bool CurrentJwtBound,
        bool CurrentMembershipRechecked,
        bool CurrentExplicitEventGrantRechecked,
        bool AuditReady);

    public readonly record struct Plan(string Code)
    {
        public bool RequiresAtomicReservation =>
            Code == "ATOMIC_RESERVATION_AND_RUNTIME_AUTH_REQUIRED";
        public bool AuthorizesDispatch => false;
        public bool AuthorizesPersistence => false;
        public bool CertifiesSignedJwtHttpOrMySql => false;
    }

    public async ValueTask<Plan> PrepareEnqueueAsync(
        Rules.EventDraft? draft, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (draft is null || draft.EmpresaId <= 0 || draft.EventoId == Guid.Empty ||
            draft.IdempotencyKey is null || draft.IdempotencyKey.Length is < 16 or > 256)
            return new("INVALID_UNTRUSTED_REQUEST");

        TrustedEvidence? evidence;
        try
        {
            evidence = await _reader.ReadAsync(
                draft.EmpresaId, draft.EventoId, draft.IdempotencyKey, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Fail closed, do not disclose provider details or simulate a write.
            return new("TRUSTED_EVIDENCE_READER_UNAVAILABLE");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (evidence is null || evidence.Context is null ||
            !evidence.FromTrustedBackend || !evidence.CurrentJwtBound ||
            !evidence.CurrentMembershipRechecked ||
            !evidence.CurrentExplicitEventGrantRechecked || !evidence.AuditReady)
            return new("FRESH_BACKEND_AUTHORITY_AND_AUDIT_REQUIRED");

        if (evidence.AuthenticatedTenantId <= 0 ||
            evidence.AuthenticatedTenantId != draft.EmpresaId ||
            evidence.Context.EmpresaId != evidence.AuthenticatedTenantId)
            return new("AUTHENTICATED_TENANT_MISMATCH");

        var domain = Rules.CheckEnqueue(evidence.Context, draft, evidence.Existing);
        if (!domain.SatisfiesDomainPreflight)
            return new(domain.Code);

        // M03.03-06 must supply fresh runtime auth, a MySQL unique tenant/key
        // reservation, transaction, worker and durable provider ACK separately.
        return new("ATOMIC_RESERVATION_AND_RUNTIME_AUTH_REQUIRED");
    }
}
