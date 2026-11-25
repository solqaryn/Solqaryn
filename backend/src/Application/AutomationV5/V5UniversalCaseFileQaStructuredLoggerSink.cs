using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C01-04.M03.06: a concrete, structured, DEV-only log sink for sanitized
/// QA observations. This does not establish source trust, certify CI/A5,
/// authorize tenant access, or expose a new HTTP endpoint.
/// </summary>
public sealed class V5UniversalCaseFileQaStructuredLoggerSink
    : IV5UniversalCaseFileQaAuditSink
{
    private static readonly Regex MicrotaskPattern = new(
        @"\AC[0-9]{2}-[0-9]{2}\.M[0-9]{2}\.[0-9]{2}\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex ShaPattern = new(
        @"\A[0-9a-f]{40}\z", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly HashSet<string> RouteKeys = new(StringComparer.Ordinal)
    {
        "casefile.timeline", "casefile.authorization",
        "casefile.attachment", "casefile.qa-evidence"
    };
    private static readonly HashSet<string> CheckStatuses = new(StringComparer.Ordinal)
    {
        "queued", "in_progress", "completed"
    };
    private static readonly HashSet<string> CheckConclusions = new(StringComparer.Ordinal)
    {
        "success", "failure", "cancelled", "skipped", "pending"
    };

    private readonly ILogger<V5UniversalCaseFileQaStructuredLoggerSink> _logger;

    public V5UniversalCaseFileQaStructuredLoggerSink(
        ILogger<V5UniversalCaseFileQaStructuredLoggerSink> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ValueTask WriteAsync(
        V5UniversalCaseFileQaEvidenceAudit.SafeAuditEntry entry,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsSafe(entry))
            throw new ArgumentException("CASEFILE_QA_SAFE_ENTRY_REQUIRED", nameof(entry));

        // IsEnabled can suppress all events at the configured log level.
        // Do not return a successful audit receipt when no event was emitted.
        if (!_logger.IsEnabled(LogLevel.Information))
            throw new InvalidOperationException("CASEFILE_QA_AUDIT_LOGGING_DISABLED");

        // Only fields from the second allowlist validation reach the logger.
        // No tenant, case, user, credential, payload or provider diagnostic.
        _logger.LogInformation(
            new EventId(1040306, "CaseFileQaEvidence"),
            "CaseFileQaEvidence {MicrotaskId} {RouteKey} {SourceSha} {CheckName} {CheckStatus} {CheckConclusion} {ObservedAtUtc} {VerificationClass} {ProviderWorkflowId} {ProviderRunId} {ProviderAttempt} {ProviderJobId} {ProviderCheckRunId} {ProviderCheckSuiteId}",
            entry.MicrotaskId, entry.RouteKey, entry.SourceSha,
            entry.CheckName, entry.CheckStatus, entry.CheckConclusion,
            entry.ObservedAtUtc, entry.VerificationClass,
            entry.ProviderJob?.WorkflowId, entry.ProviderJob?.RunId,
            entry.ProviderJob?.Attempt, entry.ProviderJob?.JobId,
            entry.ProviderJob?.CheckRunId, entry.ProviderJob?.CheckSuiteId);
        return ValueTask.CompletedTask;
    }

    private static bool IsSafe(V5UniversalCaseFileQaEvidenceAudit.SafeAuditEntry? entry) =>
        entry is not null &&
        entry.MicrotaskId is not null && MicrotaskPattern.IsMatch(entry.MicrotaskId) &&
        entry.SourceSha is not null && ShaPattern.IsMatch(entry.SourceSha) &&
        RouteKeys.Contains(entry.RouteKey) &&
        V5UniversalCaseFileQaEvidenceAudit.IsAllowedCheck(entry.MicrotaskId, entry.CheckName) &&
        CheckStatuses.Contains(entry.CheckStatus) &&
        CheckConclusions.Contains(entry.CheckConclusion) &&
        ((entry.CheckStatus == "completed" && entry.CheckConclusion != "pending") ||
         (entry.CheckStatus != "completed" && entry.CheckConclusion == "pending")) &&
        entry.ObservedAtUtc != default &&
        entry.ObservedAtUtc.Offset == TimeSpan.Zero &&
        entry.VerificationClass == "UNVERIFIED_EXTERNAL_REVIEW_REQUIRED" &&
        V5UniversalCaseFileQaEvidenceAudit.ProviderJobMatches(entry.ProviderJob,
            entry.MicrotaskId, entry.SourceSha, entry.CheckName, entry.CheckStatus, entry.CheckConclusion);
}
