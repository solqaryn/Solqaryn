using Microsoft.Extensions.Logging;
using Service = Solqaryn.Application.AutomationV5.C0402ContractApplicationService;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Sanitized, non-authorizing audit record. Never carries a caller-provided cause,
/// a contract identifier, a user identifier, credentials or tenant metadata.
/// </summary>
public sealed record C0402ContractAuditEntry(
    string EventCode, string OutcomeCode, bool IsDomainCandidateOnly)
{
    // Server-generated, opaque identifier for this attempt; never an actor or contract ID.
    public string EventId { get; } = Guid.NewGuid().ToString("N");
}

/// <summary>
/// The caller must supply a trustworthy sink. This contract alone is not durable
/// storage, a server authorization source, or a production DI registration.
/// </summary>
public interface IC0402ContractAuditSink
{
    ValueTask<bool> AppendAsync(C0402ContractAuditEntry entry, CancellationToken cancellationToken);
}

/// <summary>
/// C04-02.M03.06: fail-closed structured logging and audit boundary for the
/// existing read-only contract preflight. No contract mutations, charges, refunds
/// or legal approvals, and no background worker or provider registration.
/// </summary>
public sealed class C0402ContractAuditedPreflight
{
    private readonly Service _preflight;
    private readonly IC0402ContractAuditSink _audit;
    private readonly ILogger<C0402ContractAuditedPreflight> _logger;

    public C0402ContractAuditedPreflight(
        Service preflight,
        IC0402ContractAuditSink audit,
        ILogger<C0402ContractAuditedPreflight> logger)
    {
        _preflight = preflight ?? throw new ArgumentNullException(nameof(preflight));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<Service.Preview> PrepareAuditedAsync(
        Service.ClientIntent? intent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Service.Preview preview;
        try
        {
            preview = await _preflight.PrepareAsync(intent, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Never serialize a provider exception or client data into a log.
            preview = new Service.Preview("SERVER_PREFLIGHT_UNAVAILABLE", false);
        }
        cancellationToken.ThrowIfCancellationRequested();

        var entry = new C0402ContractAuditEntry(
            "C0402_CONTRACT_PREFLIGHT", preview.Code, preview.IsDomainCandidateOnly);
        try
        {
            // A throwing logger must fail BEFORE durable MySQL COMMIT, not
            // leave a positive audit row followed by a negative response.
            // An attempted event is NOT a durable commit or authorization.
            if (!_logger.IsEnabled(LogLevel.Information))
                return Deny("CONTRACT_STRUCTURED_LOG_UNAVAILABLE");
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation(
                "Contract preflight audit attempt {EventId} {EventCode} {OutcomeCode} {DomainCandidateOnly}",
                entry.EventId, entry.EventCode, entry.OutcomeCode, entry.IsDomainCandidateOnly);
            cancellationToken.ThrowIfCancellationRequested();
            if (!await _audit.AppendAsync(entry, cancellationToken))
                return Deny("CONTRACT_AUDIT_SINK_UNAVAILABLE");
            cancellationToken.ThrowIfCancellationRequested();
            return preview;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Deny("CONTRACT_AUDIT_OR_LOG_UNAVAILABLE");
        }
    }

    private static Service.Preview Deny(string code) => new(code, false);
}
