using Microsoft.Extensions.Logging;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C14-01.M03.06: an isolated, read-only audit boundary for the functional
/// acceptance preflight. It does NOT authenticate HTTP, change RBAC, grant
/// access, bind production DI, or certify an institutional transition.
/// </summary>
public sealed class C1401FunctionalAcceptanceAuditService
{
    private const int MaxAuditedItems = 64;
    private static readonly EventId PreflightEvent = new(140106, "C1401AcceptancePreflight");
    private readonly C1401FunctionalAcceptanceAuthorizationGate _gate;
    private readonly ILogger<C1401FunctionalAcceptanceAuditService> _logger;

    public C1401FunctionalAcceptanceAuditService(
        C1401FunctionalAcceptanceAuthorizationGate gate,
        ILogger<C1401FunctionalAcceptanceAuditService> logger)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public readonly record struct Outcome(string Code, int ItemsChecked)
    {
        public bool GrantsTenantOrResourceAccess => false;
        public bool AllowsInstitutionalWrite => false;
        public bool CertifiesFunctionalAcceptance => false;
        public bool AuthorizesQaOrProduction => false;
    }

    public async ValueTask<Outcome> InspectAndAuditAsync(
        C1401FunctionalAcceptanceAuthorizationGate.Request? request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string code;
        int items;

        try
        {
            var decision = await _gate.InspectAsync(request, cancellationToken);
            code = decision.Code;
            items = decision.ItemsChecked;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Do not disclose source exceptions, tokens, actor IDs or resources.
            code = "PREFLIGHT_SOURCE_UNAVAILABLE";
            items = 0;
        }

        // Only bounded, fixed-format codes can enter structured logs.
        if (string.IsNullOrEmpty(code) || code.Length > 96 ||
            code.Any(c => c is not (>= 'A' and <= 'Z' or >= '0' and <= '9' or '_')) ||
            items is < 0 or > MaxAuditedItems)
        {
            code = "INVALID_PREFLIGHT_RESULT";
            items = 0;
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            // If the configured audit level is disabled or the sink fails,
            // fail closed instead of silently reporting a successful audit.
            if (!_logger.IsEnabled(LogLevel.Information))
                return new("AUDIT_SINK_UNAVAILABLE", 0);
            _logger.LogInformation(PreflightEvent,
                "C1401 acceptance preflight outcome {OutcomeCode}; items {ItemsChecked}",
                code, items);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("AUDIT_SINK_UNAVAILABLE", 0);
        }

        // A positive domain preflight is still NOT application authorization.
        return new(code, items);
    }
}
