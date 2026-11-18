using Microsoft.Extensions.Logging;
using Solqaryn.Application.AutomationV5;
using Fence = Solqaryn.Application.AutomationV5.C0102CapabilityAuthorizationFence;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C01-02.M03.06. Structured in-process audit around the isolated read-through
/// evidence provider. Not registered in live DI, not a permission grant or writer.
/// No tenant, actor, resource, capability, exception or secret is ever logged.
/// </summary>
public sealed class C0102SafeCapabilityAuditReader : IC0102VerifiedCapabilityPolicyReader
{
    public const int InvalidInputEventId = 410601;
    public const int EvidenceUnavailableEventId = 410602;
    public const int UntrustedEvidenceEventId = 410603;
    public const int ReaderFailedEventId = 410604;

    private readonly C0102ReadThroughCapabilityProvider _inner;
    private readonly ILogger<C0102SafeCapabilityAuditReader> _logger;

    public C0102SafeCapabilityAuditReader(
        C0102ReadThroughCapabilityProvider inner,
        ILogger<C0102SafeCapabilityAuditReader> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<Fence.VerifiedResourcePolicy?> ReadCurrentAsync(
        int authenticatedTenantId, int resourceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (authenticatedTenantId <= 0 || resourceId <= 0)
        {
            // Static codes only. Never log untrusted IDs, claims, JWT or payloads.
            _logger.LogWarning(new EventId(InvalidInputEventId, "C0102InvalidInput"),
                "C0102_AUDIT_INVALID_INPUT");
            return null;
        }

        try
        {
            var evidence = await _inner.ReadCurrentAsync(
                authenticatedTenantId, resourceId, cancellationToken);
            if (evidence is null)
            {
                _logger.LogWarning(new EventId(EvidenceUnavailableEventId, "C0102NoEvidence"),
                    "C0102_AUDIT_NO_EVIDENCE");
                return null;
            }

            // The wrapped reader forcefully downgrades all authorization flags.
            // Audit of a read is never authorization of a read.
            _logger.LogInformation(new EventId(UntrustedEvidenceEventId, "C0102UntrustedEvidence"),
                "C0102_AUDIT_UNTRUSTED_EVIDENCE_ONLY");
            return evidence;
        }
        catch (OperationCanceledException)
        {
            // Cancellation is not a denied authorization decision; propagate it.
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Never attach the exception: its message may include tokens or tenant data.
            _logger.LogWarning(new EventId(ReaderFailedEventId, "C0102ReaderError"),
                "C0102_AUDIT_READER_UNAVAILABLE");
            return null;
        }
    }
}
