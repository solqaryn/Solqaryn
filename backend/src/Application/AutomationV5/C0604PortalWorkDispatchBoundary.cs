using Gate = Solqaryn.Application.AutomationV5.C0604PortalClienteAuthorizationGate;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C06-04.M03.05: fail-closed boundary for future customer-portal worker dispatch.
/// No live adapter is registered by this change. The existing Gate never grants write,
/// so this component cannot enqueue work with today's backend.
/// </summary>
public interface IC0604PortalWorkQueue
{
    ValueTask EnqueueAsync(
        C0604PortalWorkDispatchBoundary.Command command,
        CancellationToken cancellationToken);
}

public sealed class C0604PortalWorkDispatchBoundary
{
    private readonly Gate _authorization;
    private readonly IC0604PortalWorkQueue _queue;

    public C0604PortalWorkDispatchBoundary(
        Gate authorization, IC0604PortalWorkQueue queue)
    {
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    public sealed record Request(
        Gate.Request? Authorization, string? Operation, string? IdempotencyKey,
        DateTime RequestedAtUtc, bool ExternalDeliveryRequested);

    // Never include customer PII, tokens, payloads, or client-supplied tenant IDs.
    public sealed record Command(
        string ResourceId, string Operation, string IdempotencyKey, long ExpectedVersion);

    public readonly record struct Result(string Code, bool Enqueued);

    public async ValueTask<Result> TryEnqueueAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request?.Authorization is not { } auth ||
            !SafeOperation(request.Operation) ||
            !SafeKey(request.IdempotencyKey) ||
            request.RequestedAtUtc.Kind != DateTimeKind.Utc ||
            request.ExternalDeliveryRequested ||
            auth.AttemptsWriteOrExport || auth.ClientSuppliedRole ||
            auth.IncludesSensitiveTelemetry ||
            string.IsNullOrWhiteSpace(auth.ResourceId) ||
            auth.ExpectedVersion <= 0)
            return new("PORTAL_WORK_REQUEST_DENIED", false);

        Gate.Result gate;
        try
        {
            gate = await _authorization.EvaluateAsync(auth, cancellationToken);
            // A trusted reader may finish after cancellation without throwing.
            // Never report ordinary authorization denial for an aborted dispatch.
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("PORTAL_WORK_TRUSTED_EVIDENCE_UNAVAILABLE", false);
        }

        // A favourable preflight code is never sufficient: Gate.GrantsWrite is
        // deliberately false until an independently audited runtime authorizer
        // and durable tenant-scoped idempotency store are installed.
        if (!gate.GrantsWrite)
            return new("PORTAL_WORK_RUNTIME_AUTHORIZATION_REQUIRED", false);

        // Future opt-in is a separate R2 change requiring authorization,
        // atomic deduplication, retry/dead-letter and tenant-aware integration.
        // No production execution can reach this line with the current Gate.
        await _queue.EnqueueAsync(new Command(
            auth.ResourceId!, request.Operation!, request.IdempotencyKey!,
            auth.ExpectedVersion), cancellationToken);
        return new("PORTAL_WORK_QUEUED", true);
    }

    private static bool SafeOperation(string? operation) =>
        operation is "customer-preference-sync" or "customer-reservation-confirmation";

    private static bool SafeKey(string? key) =>
        key is { Length: >= 8 and <= 64 } &&
        key.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                     (c >= '0' && c <= '9') || c is '-' or '_' or '.');
}
