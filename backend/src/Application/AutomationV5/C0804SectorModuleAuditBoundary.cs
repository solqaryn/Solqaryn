using Rules = Solqaryn.Domain.AutomationV5.V5SectorModuleLifecycleDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Future trusted audit adapter. No implementation is registered in DI; callers must
/// provide a real append-only sink before treating this boundary as an audit trail.
/// </summary>
public interface IC0804SectorModuleAuditWriter
{
    ValueTask AppendAsync(
        C0804SectorModuleAuditBoundary.AuditRecord record,
        CancellationToken cancellationToken);
}

/// <summary>
/// C08-04.M03.06: fail-closed, in-process audit envelope around the read-only
/// sector module probe. It never grants capabilities or persists an audit itself.
/// </summary>
public sealed class C0804SectorModuleAuditBoundary
{
    private readonly C0804SectorModuleReadOnlyIntegrationBoundary _probe;
    private readonly IC0804SectorModuleAuditWriter _audit;

    public C0804SectorModuleAuditBoundary(
        C0804SectorModuleReadOnlyIntegrationBoundary probe,
        IC0804SectorModuleAuditWriter audit)
    {
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    // Only fixed, non-identifying strings are allowed. Never include token,
    // tenant, module, source SHA, provider payload or exception in this record.
    public sealed record AuditRecord(
        string CorrelationId, string Stage, string OperationClass, string OutcomeCode);

    public readonly record struct Outcome(string Code)
    {
        public bool GrantsJwtOrRbac => false;
        public bool GrantsTenantAccess => false;
        public bool EnablesModule => false;
        public bool DispatchesWork => false;
        public bool WritesDatabase => false;
        public bool CertifiesDurableAudit => false;
        public bool AuthorizesRelease => false;
    }

    private async ValueTask<bool> TryAppendAsync(
        AuditRecord record, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _audit.AppendAsync(record, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return false;
        }
    }

    public async ValueTask<Outcome> InspectWithAuditAsync(
        C0804SectorModuleAuthorizationFence.Intent? intent,
        bool clientRequestsDispatch = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var correlationId = Guid.NewGuid().ToString("N");
        var operationClass = !clientRequestsDispatch &&
            intent is { Operation: Rules.Operation.Inspect,
                ClientClaimsPermissions: false, ClientRequestsGlobalMutation: false }
                ? "READ_ONLY_INSPECT" : "UNTRUSTED_OR_OTHER_OPERATION";

        // An unavailable audit port prevents even the read-only provider probe.
        var attempt = new AuditRecord(correlationId, "ATTEMPT",
            operationClass, "NOT_CERTIFIED");
        if (!await TryAppendAsync(attempt, cancellationToken))
            return new("SECTOR_AUDIT_UNAVAILABLE");

        string outcome;
        try
        {
            var probe = await _probe.ProbeAsync(
                intent, clientRequestsDispatch, cancellationToken);
            // Whitelist response codes; never echo untrusted provider details.
            outcome = probe.Code switch
            {
                "READ_ONLY_PROVIDER_READY_NON_AUTHORIZING" =>
                    "SECTOR_READINESS_STAGED_NOT_AUTHORIZED",
                "READ_ONLY_PROVIDER_NOT_READY" => "SECTOR_READINESS_NOT_READY",
                "READ_ONLY_PROVIDER_UNAVAILABLE" => "SECTOR_READINESS_SOURCE_UNAVAILABLE",
                _ => "SECTOR_READINESS_DENIED"
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            outcome = "SECTOR_READINESS_SOURCE_UNAVAILABLE";
        }

        // If the outcome cannot be audited, no positive readiness escapes.
        var completed = new AuditRecord(correlationId, "OUTCOME",
            operationClass, outcome);
        if (!await TryAppendAsync(completed, cancellationToken))
            return new("SECTOR_AUDIT_UNAVAILABLE");

        return new(outcome);
    }
}
