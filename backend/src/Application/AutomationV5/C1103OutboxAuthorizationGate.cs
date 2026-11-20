using System.Globalization;
using System.Security.Claims;
using Rules = Solqaryn.Domain.AutomationV5.C1103OutboxEventDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C11-03.M03.03: fail-closed boundary on server-issued JWT principal and
/// fresh membership. Not registered in DI or called by an API at this stage.
/// Does not authorize persistence, dispatch, a provider ACK or production.
/// </summary>
public interface IC1103OutboxCurrentMembershipReader
{
    ValueTask<C1103OutboxAuthorizationGate.CurrentAuthority?> ReadCurrentAsync(
        int authenticatedUserId, int requestedTenantId, CancellationToken cancellationToken);
}

public sealed class C1103OutboxAuthorizationGate
{
    public sealed record CurrentAuthority(
        int UserId, int TenantId, bool ServerTrusted, bool MembershipActive,
        bool RoleGrantActive, bool PermissionRevoked, bool TenantOwnedByUser,
        DateTime VerifiedNowUtc, IReadOnlySet<string> AllowedEventTypes,
        Rules.ExistingIntent? Existing = null);

    public readonly record struct Verdict(string Code)
    {
        public bool PermitsDomainPreflightOnly => Code == "OUTBOX_AUTH_PREFLIGHT_ONLY";
        public bool AuthorizesPersistence => false;
        public bool AuthorizesDispatch => false;
        public bool CertifiesSignedJwtHttpOrMysql => false;
    }

    private readonly IC1103OutboxCurrentMembershipReader _reader;
    private readonly TimeProvider _timeProvider;

    public C1103OutboxAuthorizationGate(
        IC1103OutboxCurrentMembershipReader reader, TimeProvider? timeProvider = null)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<Verdict> InspectAsync(
        ClaimsPrincipal? serverValidatedPrincipal,
        Rules.EventDraft? draft,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (draft is null || draft.EmpresaId <= 0 || draft.EventoId == Guid.Empty ||
            serverValidatedPrincipal?.Identity?.IsAuthenticated != true)
            return new("OUTBOX_AUTH_DENIED");

        // A server-only JWT middleware must validate signature/expiry before
        // invoking this class. Never accept user, tenant or role from headers.
        var identities = serverValidatedPrincipal.Claims
            .Where(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")
            .Select(c => c.Value).ToArray();
        if (identities.Length != 1 ||
            !int.TryParse(identities[0], NumberStyles.None, CultureInfo.InvariantCulture, out var userId) ||
            userId <= 0)
            return new("OUTBOX_AUTH_DENIED");

        CurrentAuthority? current;
        try
        {
            current = await _reader.ReadCurrentAsync(userId, draft.EmpresaId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("OUTBOX_AUTH_BACKEND_UNAVAILABLE");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (current is null || !current.ServerTrusted || !current.MembershipActive ||
            !current.RoleGrantActive || current.PermissionRevoked ||
            !current.TenantOwnedByUser || current.UserId != userId ||
            current.TenantId != draft.EmpresaId ||
            current.AllowedEventTypes is null ||
            current.VerifiedNowUtc.Kind != DateTimeKind.Utc)
            return new("OUTBOX_AUTH_DENIED");

        // Cached or future-dated membership is not a live authorization.
        // Compare to the server clock, never to a client-supplied timestamp.
        var serverNow = _timeProvider.GetUtcNow().UtcDateTime;
        if (current.VerifiedNowUtc < serverNow.AddSeconds(-30) ||
            current.VerifiedNowUtc > serverNow.AddSeconds(5))
            return new("OUTBOX_AUTH_DENIED");

        var domainContext = new Rules.VerifiedServerContext(
            current.TenantId, current.MembershipActive, current.RoleGrantActive,
            current.PermissionRevoked, current.AllowedEventTypes, current.VerifiedNowUtc);
        var result = Rules.CheckEnqueue(domainContext, draft, current.Existing);
        return result.SatisfiesDomainPreflight
            ? new("OUTBOX_AUTH_PREFLIGHT_ONLY")
            : new(result.Code);
    }
}
