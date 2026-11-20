using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.Repositories;

/// <summary>
/// C11-03.M03.06: opt-in structured audit of a tenant-authorized outbox relay.
/// No DI registration, hosted service, provider, public API or scheduler.
/// </summary>
public sealed class C1103OutboxAuditedRelay(
    C1103OutboxIsolatedRelayWorker worker,
    ILogger<C1103OutboxAuditedRelay> logger)
{
    public async Task<C1103OutboxIsolatedRelayWorker.BatchResult> RunTenantAsync(
        int authorizedTenantId, DateTime nowUtc, int maximumBatch,
        int maximumAttempts, TimeSpan retryDelay,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worker);
        ArgumentNullException.ThrowIfNull(logger);
        try
        {
            var result = await worker.RunTenantAsync(
                authorizedTenantId, nowUtc, maximumBatch,
                maximumAttempts, retryDelay, cancellationToken);
            AuditSafe(LogLevel.Information, "OUTBOX_BATCH_COMPLETED",
                authorizedTenantId, result);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AuditSafe(LogLevel.Warning, "OUTBOX_BATCH_CANCELLED",
                authorizedTenantId, default);
            throw;
        }
        catch (ArgumentException) { throw; }
        catch (Exception)
        {
            AuditSafe(LogLevel.Error, "OUTBOX_BATCH_FAILED",
                authorizedTenantId, default);
            // Provider/SQL exception details and inner exceptions are unsafe.
            throw new InvalidOperationException("OUTBOX_RELAY_INTERNAL_FAILURE");
        }
    }

    private void AuditSafe(LogLevel level, string code, int tenantId,
        C1103OutboxIsolatedRelayWorker.BatchResult? result)
    {
        try
        {
            // Fixed codes and numeric counters only; never payload, idempotency
            // key, connection string, provider exception or user data.
            logger.Log(level,
                "C1103OutboxAudit Code={AuditCode} TenantId={TenantId} Claimed={Claimed} Delivered={Delivered} RetryScheduled={RetryScheduled} DeadLetter={DeadLetter} Stale={Stale}",
                code, tenantId, result?.Claimed ?? 0, result?.Delivered ?? 0,
                result?.RetryScheduled ?? 0, result?.DeadLetter ?? 0,
                result?.Stale ?? 0);
        }
        catch (Exception)
        {
            // Telemetry failure must never repeat external delivery or change
            // durable SQL state; this sink is deliberately best-effort.
        }
    }
}
