using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C04-04.M03.06: structured audit of the DISCONNECTED dry-run queue only.
/// Never registered in DI, HTTP, hosted services, payment providers or ledger.
/// Future callers must authenticate and authorize tenant independently.
/// </summary>
public sealed class C0404DryRunAuditedQueue(
    C0404DryRunDispatchQueue queue,
    ILogger<C0404DryRunAuditedQueue> logger)
{
    public Task<C0404DryRunDispatchQueue.Result> EnqueueAsync(
        C0404DryRunDispatchQueue.Command? command, CancellationToken token = default) =>
        ObserveAsync("ENQUEUE", command, () => queue.EnqueueDryRunAsync(command, token), token);

    public Task<C0404DryRunDispatchQueue.Result> InspectAsync(
        C0404DryRunDispatchQueue.Command? command, CancellationToken token = default) =>
        ObserveAsync("INSPECT", command, () => queue.InspectAsync(command, token), token);

    private async Task<C0404DryRunDispatchQueue.Result> ObserveAsync(
        string action,
        C0404DryRunDispatchQueue.Command? command,
        Func<Task<C0404DryRunDispatchQueue.Result>> operation,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(logger);
        token.ThrowIfCancellationRequested();
        try
        {
            var result = await operation();
            token.ThrowIfCancellationRequested();
            AuditSafe(LogLevel.Information, "C0404_DRYRUN_RESULT", action,
                command?.EmpresaId ?? 0, result.Code);
            return result;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            AuditSafe(LogLevel.Warning, "C0404_DRYRUN_CANCELLED", action,
                command?.EmpresaId ?? 0, C0404DryRunDispatchQueue.Status.Unavailable);
            throw;
        }
        catch (Exception)
        {
            // No unsafe EF/SQL/providing exception text, object or inner exception.
            AuditSafe(LogLevel.Error, "C0404_DRYRUN_FAILED", action,
                command?.EmpresaId ?? 0, C0404DryRunDispatchQueue.Status.Unavailable);
            return new C0404DryRunDispatchQueue.Result(
                C0404DryRunDispatchQueue.Status.Unavailable);
        }
    }

    private void AuditSafe(LogLevel level, string code, string action, int tenant,
        C0404DryRunDispatchQueue.Status status)
    {
        try
        {
            // Fixed codes, operation, numeric tenant and bounded enum only.
            // Never log provider, invoice, key, digest, money, user or exception.
            logger.Log(level,
                "C0404Audit Code={AuditCode} Action={AuditAction} Tenant={Tenant} Status={Status}",
                code, action, tenant, status);
        }
        catch (Exception)
        {
            // Logging failure must not alter the durable dry-run queue.
        }
    }
}
