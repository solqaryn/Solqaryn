using Microsoft.Extensions.Logging;
using Worker = Solqaryn.Infrastructure.AutomationV5.V5GlobalControlPlaneIsolatedDispatchWorker;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Backend-only authority for a non-durable control-plane audit preview.
/// A caller-provided worker receipt is never proof of authentication.
/// </summary>
public interface IV5GlobalControlPlaneAuditAuthority
{
    ValueTask<V5GlobalControlPlaneAuditSink.VerifiedContext?> CurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// C02-04.M03.06: records only a fixed, non-sensitive result code.
/// This is not a JWT adapter, a persisted audit ledger or a grant.
/// </summary>
public sealed class V5GlobalControlPlaneAuditSink
{
    private static readonly EventId AuditEvent = new(204306, "V5GlobalControlPlaneAuditPreview");
    private static readonly HashSet<string> AllowedCodes = new(StringComparer.Ordinal)
    {
        "AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED",
        "CONTROL_PLANE_BATCH_REQUIRED",
        "CONTROL_PLANE_BATCH_LIMIT_DENIED",
        "VERIFIED_CONTROL_PLANE_BATCH_SCOPE_REQUIRED",
        "CONTROL_PLANE_TICKET_INVALID",
        "CONTROL_PLANE_TICKET_DUPLICATE",
        "CONTROL_PLANE_UNVERIFIED_TICKET_SOURCE",
        "CONTROL_PLANE_BATCH_ACTOR_MISMATCH",
        "CONTROL_PLANE_BATCH_TENANT_MISMATCH",
        "CONTROL_PLANE_BATCH_STALE_SHA",
        "CONTROL_PLANE_UNSAFE_TICKET_DENIED",
        "CONTROL_PLANE_CURRENT_BACKEND_AUTHORITY_REQUIRED",
        "MFA_AND_REAUTHENTICATION_REQUIRED",
        "INDEPENDENT_PLATFORM_GRANT_REQUIRED"
    };

    private readonly IV5GlobalControlPlaneAuditAuthority _authority;
    private readonly ILogger<V5GlobalControlPlaneAuditSink> _logger;

    public V5GlobalControlPlaneAuditSink(
        IV5GlobalControlPlaneAuditAuthority authority,
        ILogger<V5GlobalControlPlaneAuditSink> logger)
    {
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Produced only by a trusted backend implementation, never bound from HTTP.
    public sealed record VerifiedContext(
        string? ActorId, string? ActorTenantId, string? TargetTenantId,
        string? SourceSha, string? ObservedSourceSha,
        bool BackendAuthenticated, bool CurrentPlatformRole,
        bool RecentMfaAndReauthentication, bool CurrentAuditPermission,
        bool AuditFenceCurrent, bool Revoked);

    public readonly record struct Receipt(string Code)
    {
        public bool LoggerAccepted => Code == "SAFE_CONTROL_PLANE_AUDIT_PREVIEW_LOGGED";
        public bool GrantsAccess => false;
        public bool GrantsPlatformDirectory => false;
        public bool GrantsExceptionalLease => false;
        public bool CertifiesAuthenticatedRuntime => false;
        public bool CertifiesDurableAudit => false;
        public bool WritesDatabase => false;
        public bool AuthorizesQaOrProduction => false;
    }

    /// <summary>
    /// Only a pre-approved code, never a worker ticket or request payload,
    /// crosses the ILogger boundary. Fresh authority is required for each call.
    /// </summary>
    public async ValueTask<Receipt> RecordAsync(
        Worker.Receipt result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!AllowedCodes.Contains(result.Code))
            return new("CONTROL_PLANE_AUDIT_UNKNOWN_OUTCOME");

        var level = result.Code == "AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED"
            ? LogLevel.Information : LogLevel.Warning;
        if (!_logger.IsEnabled(level))
            return new("CONTROL_PLANE_AUDIT_LOGGER_UNAVAILABLE");

        VerifiedContext? context;
        try { context = await _authority.CurrentAsync(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("CONTROL_PLANE_AUDIT_AUTHORITY_UNAVAILABLE");
        }

        if (context is null || !SafeId(context.ActorId) ||
            !SafeId(context.ActorTenantId) ||
            !string.Equals(context.ActorTenantId, context.TargetTenantId,
                StringComparison.Ordinal) ||
            !CanonicalSha(context.SourceSha) ||
            !string.Equals(context.SourceSha, context.ObservedSourceSha,
                StringComparison.Ordinal) ||
            !context.BackendAuthenticated || !context.CurrentPlatformRole ||
            !context.RecentMfaAndReauthentication ||
            !context.CurrentAuditPermission || !context.AuditFenceCurrent ||
            context.Revoked)
            return new("CURRENT_CONTROL_PLANE_BACKEND_AUTHORITY_REQUIRED");

        try
        {
            _logger.Log(level, AuditEvent,
                "V5 global control-plane audit preview {OutcomeCode}", result.Code);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("CONTROL_PLANE_AUDIT_LOGGER_UNAVAILABLE");
        }
        return new("SAFE_CONTROL_PLANE_AUDIT_PREVIEW_LOGGED");
    }

    private static bool SafeId(string? value) =>
        value is { Length: >= 1 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');

    private static bool CanonicalSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
