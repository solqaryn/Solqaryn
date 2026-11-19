using Solqaryn.Domain.Entities;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C11-03.M03.01. Pure domain preflight for outbox event intents.
/// Input snapshots must be constructed by a trusted server-side adapter.
/// This does not register a worker, authorize a dispatch, persist records,
/// authenticate a JWT, or establish a database transaction.
/// </summary>
public static class C1103OutboxEventDomainRules
{
    public sealed record VerifiedServerContext(
        int EmpresaId,
        bool MembershipActive,
        bool ExplicitEventGrant,
        bool PermissionRevoked,
        IReadOnlySet<string> AllowedEventTypes,
        DateTime TrustedNowUtc);

    public sealed record EventDraft(
        int EmpresaId,
        Guid EventoId,
        string? IdempotencyKey,
        string? EventType,
        string? PayloadSha256,
        DateTime OccurredAtUtc,
        string? CorrelationId,
        int SchemaVersion,
        int Attempt,
        int MaxAttempts);

    public sealed record ExistingIntent(
        int EmpresaId,
        Guid EventoId,
        string IdempotencyKey,
        string EventType,
        string PayloadSha256,
        int Attempt,
        bool Delivered,
        bool DeadLetter);

    public readonly record struct Decision(string Code)
    {
        public bool SatisfiesDomainPreflight => Code == "DOMAIN_PRECONDITIONS_ONLY";
        public bool AuthorizesDispatch => false;
        public bool AuthorizesPersistence => false;
        public bool CertifiesJwtOrMySqlIntegration => false;
    }

    public static Decision CheckEnqueue(
        VerifiedServerContext? context,
        EventDraft? draft,
        ExistingIntent? existing = null)
    {
        if (context is null || draft is null || context.EmpresaId <= 0 ||
            context.AllowedEventTypes is null ||
            context.TrustedNowUtc.Kind != DateTimeKind.Utc)
            return Deny("TRUSTED_CONTEXT_REQUIRED");

        if (!context.MembershipActive || !context.ExplicitEventGrant ||
            context.PermissionRevoked)
            return Deny("MEMBERSHIP_OR_EVENT_GRANT_DENIED");

        if (draft.EmpresaId <= 0 || draft.EmpresaId != context.EmpresaId)
            return Deny("TENANT_MISMATCH");

        if (draft.EventoId == Guid.Empty ||
            !SafeEventType(draft.EventType) ||
            !context.AllowedEventTypes.Contains(draft.EventType!))
            return Deny("EVENT_ID_OR_TYPE_DENIED");

        if (!SafeIdempotencyKey(draft.IdempotencyKey) ||
            !SafeSha256(draft.PayloadSha256))
            return Deny("IDEMPOTENCY_OR_PAYLOAD_DIGEST_INVALID");

        if (draft.SchemaVersion != 1 ||
            draft.OccurredAtUtc.Kind != DateTimeKind.Utc ||
            draft.OccurredAtUtc > context.TrustedNowUtc ||
            draft.OccurredAtUtc < DateTime.UnixEpoch)
            return Deny("SCHEMA_OR_TRUSTED_UTC_INVALID");

        if (!SafeCorrelation(draft.CorrelationId) ||
            draft.MaxAttempts is < 1 or > 100 ||
            draft.Attempt < 0 || draft.Attempt >= draft.MaxAttempts)
            return Deny("CORRELATION_OR_RETRY_LIMIT_INVALID");

        if (existing is not null)
        {
            // A cross-tenant record is never proof of a replay. Never use
            // CorrelationId or the caller's event ID as an authority to fetch it.
            if (existing.EmpresaId != context.EmpresaId)
                return Deny("EXISTING_INTENT_TENANT_MISMATCH");

            if (!string.Equals(existing.IdempotencyKey, draft.IdempotencyKey,
                    StringComparison.Ordinal))
                return Deny("EXISTING_IDEMPOTENCY_KEY_MISMATCH");

            if (existing.DeadLetter)
                return Deny("DEAD_LETTER_TERMINAL");

            if (existing.EventoId != draft.EventoId ||
                !string.Equals(existing.EventType, draft.EventType,
                    StringComparison.Ordinal) ||
                !string.Equals(existing.PayloadSha256, draft.PayloadSha256,
                    StringComparison.OrdinalIgnoreCase))
                return Deny("IDEMPOTENCY_PAYLOAD_CONFLICT_409");

            // Exact same tenant/key/intent never grants a second effect.
            return Deny("EXACT_REPLAY_NO_NEW_DISPATCH");
        }

        // A domain precondition is not an authorization to persist or send.
        // M03.02-06 must enforce live JWT/RBAC, MySQL unique index, CAS,
        // transaction, ACK semantics and bounded worker retries separately.
        return new Decision("DOMAIN_PRECONDITIONS_ONLY");
    }

    public static Decision CheckDurableAck(
        VerifiedServerContext? context,
        ExistingIntent? stored,
        int expectedTenant,
        Guid expectedEventId,
        int expectedAttempt,
        bool durableProviderAcknowledgement)
    {
        if (context is null || context.EmpresaId <= 0 ||
            !context.MembershipActive || !context.ExplicitEventGrant ||
            context.PermissionRevoked)
            return Deny("TRUSTED_ACK_AUTHORITY_REQUIRED");

        if (stored is null || expectedTenant != context.EmpresaId ||
            stored.EmpresaId != context.EmpresaId ||
            stored.EventoId != expectedEventId || expectedEventId == Guid.Empty)
            return Deny("ACK_TENANT_OR_EVENT_MISMATCH");

        if (stored.DeadLetter || stored.Delivered)
            return Deny("ACK_TERMINAL_STATE");

        if (expectedAttempt <= 0 || expectedAttempt != stored.Attempt)
            return Deny("STALE_ACK_ATTEMPT");

        if (!durableProviderAcknowledgement)
            return Deny("DURABLE_ACK_REQUIRED");

        // It remains an in-memory preflight; no state transition is performed.
        return new Decision("ACK_PRECONDITIONS_ONLY");
    }

    private static Decision Deny(string code) => new(code);

    private static bool SafeEventType(string? text) =>
        text is { Length: >= 2 and <= MensajeOutbox.LongitudMaximaTipoEvento } &&
        text.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '.' or '_' or '-');

    private static bool SafeIdempotencyKey(string? text) =>
        text is { Length: >= 16 and <= MensajeOutbox.LongitudMaximaClaveIdempotencia } &&
        text.All(c => c is >= '!' and <= '~');

    private static bool SafeSha256(string? text) =>
        text is { Length: 64 } &&
        text.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');

    private static bool SafeCorrelation(string? text) =>
        text is null || (text.Length <= MensajeOutbox.LongitudMaximaCorrelationId &&
            text.All(c => !char.IsControl(c)));
}
