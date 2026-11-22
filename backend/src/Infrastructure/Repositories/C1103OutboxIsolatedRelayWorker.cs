using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Repositories;

/// <summary>
/// C11-03.M03.05: bounded, opt-in relay primitive. No DI registration, hosted
/// service, HTTP route, real provider, scheduler or cross-tenant enumeration.
/// The caller must authorize the tenant; provider must deduplicate by tenant/key.
/// </summary>
public interface IC1103OutboxIsolatedTransport
{
    Task SendAsync(
        int empresaId, Guid eventoId, string tipoEvento, string payloadJson,
        string claveIdempotencia, CancellationToken cancellationToken);
}

public sealed class C1103OutboxIsolatedRelayWorker(
    IMensajeOutboxRepository repository,
    IC1103OutboxIsolatedTransport transport)
{
    public sealed record BatchResult(
        int Claimed, int Delivered, int RetryScheduled, int DeadLetter, int Stale);

    public async Task<BatchResult> RunTenantAsync(
        int authorizedTenantId, DateTime nowUtc, int maximumBatch,
        int maximumAttempts, TimeSpan retryDelay, CancellationToken cancellationToken = default)
    {
        if (authorizedTenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(authorizedTenantId));
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("UTC required.", nameof(nowUtc));
        if (maximumBatch is < 1 or > 200)
            throw new ArgumentOutOfRangeException(nameof(maximumBatch));
        if (maximumAttempts is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        if (retryDelay <= TimeSpan.Zero || retryDelay > TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(nameof(retryDelay));
        if (nowUtc > DateTime.MaxValue - retryDelay)
            throw new ArgumentOutOfRangeException(nameof(nowUtc),
                "UTC retry window exceeds supported timestamp range.");

        cancellationToken.ThrowIfCancellationRequested();
        var claimed = await repository.ClaimDisponiblesAsync(
            authorizedTenantId, nowUtc, maximumBatch, cancellationToken);
        // The repository owns durable claims. A broken or compromised adapter
        // could return A followed by B: validating inside the delivery loop
        // would already transmit A before discovering the forbidden B row.
        // Reject the *entire* claimed batch before any external side effect.
        if (claimed is null || claimed.Count > maximumBatch)
            throw new InvalidOperationException("OUTBOX_CLAIM_INVALID_BATCH");
        foreach (var candidate in claimed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (candidate is null || candidate.EmpresaId != authorizedTenantId
                || candidate.Intentos <= 0)
                throw new InvalidOperationException("OUTBOX_CROSS_TENANT_OR_INVALID_CLAIM");
        }

        var delivered = 0;
        var retries = 0;
        var deadLetters = 0;
        var stale = 0;

        foreach (MensajeOutbox message in claimed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (message.EmpresaId != authorizedTenantId || message.Intentos <= 0)
                throw new InvalidOperationException("OUTBOX_CROSS_TENANT_OR_INVALID_CLAIM");

            try
            {
                // External delivery cannot be rolled back with SQL; its consumer
                // must be idempotent. CAS protects ACK, not exactly-once delivery.
                await transport.SendAsync(
                    authorizedTenantId, message.EventoId, message.TipoEvento,
                    message.PayloadJson, message.ClaveIdempotencia, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (await repository.MarcarEntregadoAsync(
                    authorizedTenantId, message.Id, message.Intentos, nowUtc, cancellationToken))
                    delivered++;
                else
                    stale++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Existing stale-claim recovery owns the retry; never false-ACK.
                throw;
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool terminal = message.Intentos >= maximumAttempts;
                bool persisted = await repository.RegistrarFalloAsync(
                    authorizedTenantId, message.Id, message.Intentos,
                    "OUTBOX_TRANSPORT_FAILURE", nowUtc, nowUtc.Add(retryDelay),
                    terminal, cancellationToken);
                if (!persisted)
                    stale++;
                else if (terminal)
                    deadLetters++;
                else
                    retries++;
            }
        }
        return new BatchResult(claimed.Count, delivered, retries, deadLetters, stale);
    }
}
