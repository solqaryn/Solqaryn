using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Backend-only source of fresh UI-lightness audit authority. No client DTO
/// or browser assertion can implement this trust boundary.
/// </summary>
public interface IV5UiLightnessAuditAuthority
{
    ValueTask<V5UiLightnessQueueAuditSink.VerifiedContext?> CurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// C05-04.M03.06: structured, non-durable audit of bounded isolated queue
/// outcomes. Does not start a hosted worker, emit browser telemetry or
/// authorize a tenant, a release or a production operation.
/// </summary>
public sealed class V5UiLightnessQueueAuditSink
{
    private static readonly EventId AuditEvent = new(504306, "V5UiLightnessReviewAudit");
    private static readonly HashSet<string> Codes = new(StringComparer.Ordinal)
    {
        "EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED",
        "BACKEND_AUTHORITY_REQUIRED",
        "QUEUED_FOR_ISOLATED_REVIEW",
        "REPLAY_REQUIRES_DURABLE_IDEMPOTENCY",
        "IDEMPOTENCY_KEY_CONFLICT_DENIED",
        "ISOLATED_QUEUE_TENANT_SCOPE_MISMATCH",
        "ISOLATED_QUEUE_SOURCE_SHA_MISMATCH",
        "INVALID_TENANT_SCOPE",
        "INVALID_RESOURCE_OR_IDEMPOTENCY_KEY",
        "IDEMPOTENCY_REGISTRY_FULL_FAIL_CLOSED",
        "BOUNDED_QUEUE_FULL",
        "NO_PENDING_ISOLATED_REVIEW"
    };

    private readonly IV5UiLightnessAuditAuthority _authority;
    private readonly ILogger<V5UiLightnessQueueAuditSink> _logger;

    public V5UiLightnessQueueAuditSink(
        IV5UiLightnessAuditAuthority authority,
        ILogger<V5UiLightnessQueueAuditSink> logger)
    {
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public sealed record VerifiedContext(
        int ActorId, int ActorTenantId, int ResourceTenantId,
        string? SourceSha, string? ObservedSourceSha,
        bool BackendAuthenticated, bool MembershipCurrent,
        bool RoleCurrent, bool ExplicitReadPermissionCurrent,
        bool AuditFenceCurrent, bool Revoked);

    public readonly record struct Receipt(string Code)
    {
        public bool LoggerAccepted => Code == "SAFE_UI_LIGHTNESS_AUDIT_PREVIEW_LOGGED";
        public bool GrantsAccess => false;
        public bool RunsWorkerOrProvider => false;
        public bool CertifiesBrowserOrCoreWebVitals => false;
        public bool CertifiesDurableAudit => false;
        public bool WritesDatabase => false;
        public bool AuthorizesQaOrProduction => false;
    }

    /// <summary>
    /// Only fixed codes and fixed severity cross the logging boundary. The
    /// current backend scope is checked each time and never logged.
    /// </summary>
    public async ValueTask<Receipt> RecordAsync(
        V5UiLightnessBoundedReviewQueue.Outcome result,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Codes.Contains(result.Code))
            return new("UNRECOGNIZED_OR_UNTRUSTED_QUEUE_OUTCOME");

        // A reviewed item is not a live browser measurement or a grant.
        var info = result.Code == "EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED";
        var level = info ? LogLevel.Information : LogLevel.Warning;
        if (!_logger.IsEnabled(level))
            return new("AUDIT_LOGGER_UNAVAILABLE");

        VerifiedContext? context;
        try { context = await _authority.CurrentAsync(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("AUDIT_AUTHORITY_UNAVAILABLE");
        }

        if (context is null || context.ActorId <= 0 ||
            context.ActorTenantId <= 0 ||
            context.ResourceTenantId != context.ActorTenantId ||
            !CanonicalSha(context.SourceSha) ||
            !string.Equals(context.SourceSha, context.ObservedSourceSha, StringComparison.Ordinal) ||
            !context.BackendAuthenticated || !context.MembershipCurrent ||
            !context.RoleCurrent || !context.ExplicitReadPermissionCurrent ||
            !context.AuditFenceCurrent || context.Revoked)
            return new("CURRENT_BACKEND_TENANT_AUTHORIZATION_REQUIRED");

        try
        {
            _logger.Log(level, AuditEvent,
                "V5 UI lightness review {OutcomeCode}", result.Code);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("AUDIT_LOGGER_UNAVAILABLE");
        }

        return new("SAFE_UI_LIGHTNESS_AUDIT_PREVIEW_LOGGED");
    }

    private static bool CanonicalSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
