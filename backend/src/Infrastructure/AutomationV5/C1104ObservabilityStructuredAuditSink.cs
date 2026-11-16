using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Trusted backend audit evidence, never a browser DTO or client-asserted claim.
/// No live implementation or DI registration is installed by this microtask.
/// </summary>
public interface IC1104ObservabilityAuditEvidenceSource
{
    ValueTask<C1104ObservabilityStructuredAuditSink.TrustedContext?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// C11-04.M03.06: structured in-process audit of the existing bounded,
/// tenant-scoped observability review. ILogger is not a durable audit store.
/// Does not read private metrics, start a worker, export or grant access.
/// </summary>
public sealed class C1104ObservabilityStructuredAuditSink
{
    private static readonly EventId AuditEvent = new(110406, "C1104BoundedObservabilityAudit");
    private readonly IC1104ObservabilityAuditEvidenceSource _authority;
    private readonly ILogger<C1104ObservabilityStructuredAuditSink> _logger;

    public C1104ObservabilityStructuredAuditSink(
        IC1104ObservabilityAuditEvidenceSource authority,
        ILogger<C1104ObservabilityStructuredAuditSink> logger)
    {
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public sealed record TrustedContext(
        string? ExpectedSourceSha, string? ObservedSourceSha,
        bool BackendAuthenticated, bool MembershipCurrent,
        bool ExplicitReadPermissionCurrent, bool TenantPartitionMatches,
        bool AuditFenceCurrent);

    public readonly record struct Receipt(string Code)
    {
        public bool LoggerAcceptedEvent => Code == "SAFE_STRUCTURED_OBSERVABILITY_PREVIEW_LOGGED";
        public bool GrantsTenantRead => false;
        public bool ExposesMetricsOrPayload => false;
        public bool CertifiesSloOrCost => false;
        public bool CertifiesDurableAudit => false;
        public bool AuthorizesQaOrProduction => false;
        public bool StartsWorkerOrExporter => false;
    }

    /// <summary>
    /// Only fixed result codes and bounded counts cross the logging boundary.
    /// No tenant, actor, JWT, SHA, raw exception or correlation is ever logged.
    /// A forged review can at most produce a non-authorizing preview log.
    /// </summary>
    public async ValueTask<Receipt> WriteAsync(
        C1104ObservabilityBoundedReadWorker.Review review,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var allowed = review.Code is
            "OBSERVABILITY_PREVIEW_REQUIRES_AUTHENTICATED_API" or
            "OBSERVABILITY_BATCH_REQUIRED_OR_TOO_LARGE" or
            "OBSERVABILITY_UNTRUSTED_OR_INVALID_TICKET" or
            "OBSERVABILITY_MIXED_TENANT_OR_SOURCE_SHA" or
            "OBSERVABILITY_DUPLICATE_TICKET" or
            "OBSERVABILITY_SOURCE_REJECTED" or
            "OBSERVABILITY_SOURCE_UNAVAILABLE";
        var preview = review.Code == "OBSERVABILITY_PREVIEW_REQUIRES_AUTHENTICATED_API";
        if (!allowed || (preview ? review.Examined is < 1 or > 16 : review.Examined != 0))
            return new("UNTRUSTED_OR_INVALID_AUDIT_RESULT");

        var level = preview ? LogLevel.Information : LogLevel.Warning;
        if (!_logger.IsEnabled(level))
            return new("AUDIT_LOGGER_UNAVAILABLE");

        TrustedContext? context;
        try
        {
            context = await _authority.ReadCurrentAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("TRUSTED_AUDIT_EVIDENCE_UNAVAILABLE");
        }

        if (context is null || !LowercaseSha(context.ExpectedSourceSha) ||
            !string.Equals(context.ExpectedSourceSha, context.ObservedSourceSha,
                StringComparison.Ordinal) ||
            !context.BackendAuthenticated || !context.MembershipCurrent ||
            !context.ExplicitReadPermissionCurrent ||
            !context.TenantPartitionMatches || !context.AuditFenceCurrent)
            return new("CURRENT_AUTHORITY_AND_TENANT_FENCE_REQUIRED");

        try
        {
            // Schema is constant and arguments have been strictly allowlisted.
            // This is a non-durable preview; a real audit provider is NOT wired.
            _logger.Log(level, AuditEvent,
                "C1104 bounded observability audit {AuditCode} {InspectedCount}",
                review.Code, review.Examined);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("AUDIT_LOGGER_UNAVAILABLE");
        }

        return new("SAFE_STRUCTURED_OBSERVABILITY_PREVIEW_LOGGED");
    }

    private static bool LowercaseSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
