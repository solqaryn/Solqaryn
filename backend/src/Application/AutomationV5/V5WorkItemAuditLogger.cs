using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Structured, bounded V5 lifecycle audit logging. Only approved, non-sensitive
/// values can cross the logging boundary. This component does not append to Sheets,
/// move a work item state, acquire a claim, grant tenant access or send a webhook.
/// </summary>
public sealed class V5WorkItemAuditLogger
{
    public sealed record AuditRecord(
        string WorkItemId,
        string TaskId,
        string Operation,
        string Outcome,
        string CommitSha,
        Guid CorrelationId,
        string? ErrorCode = null);

    private static readonly EventId AuditEventId = new(506, "V5WorkItemAudit");
    private static readonly IReadOnlySet<string> Operations = new HashSet<string>(StringComparer.Ordinal)
    {
        "CLAIM", "CANDIDATE_READY", "PUBLISH", "LISTO_DEV", "A5_PENDIENTE",
        "A5_PASS", "A5_FAIL", "AUDIT_A0_A4"
    };
    private static readonly IReadOnlySet<string> Outcomes = new HashSet<string>(StringComparer.Ordinal)
    {
        "PASS", "FAIL", "PENDING"
    };
    private static readonly IReadOnlySet<string> ErrorCodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "INVALID_ID", "UNAUTHORIZED_ACTOR", "CLAIM_CONFLICT", "DEPENDENCY_BLOCKED",
        "CANDIDATE_INCOMPLETE", "AUDIT_REQUIRED", "CHECK_SHA_MISMATCH",
        "SECURITY_DENIED", "VALIDATION_FAILED", "NO_PERMISSION"
    };

    private readonly ILogger<V5WorkItemAuditLogger> _logger;

    public V5WorkItemAuditLogger(ILogger<V5WorkItemAuditLogger> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Return false without emitting data if any field is invalid. No raw exception,
    /// JWT, PII, connection string or arbitrary error message is accepted.
    /// </summary>
    public bool TryWrite(AuditRecord? record)
    {
        if (record is null ||
            !IsV5WorkItemId(record.WorkItemId) ||
            !IsFixedHex(record.TaskId, 32) ||
            !IsFixedHex(record.CommitSha, 40) ||
            !Operations.Contains(record.Operation) ||
            !Outcomes.Contains(record.Outcome) ||
            record.CorrelationId == Guid.Empty ||
            (record.Outcome == "FAIL"
                ? string.IsNullOrWhiteSpace(record.ErrorCode) || !ErrorCodes.Contains(record.ErrorCode)
                : record.ErrorCode is not null))
            return false;

        // No exception object, request body, bearer credential or raw user input is logged.
        if (record.Outcome == "FAIL")
        {
            _logger.LogWarning(AuditEventId,
                "V5 audit {WorkItemId} {Operation} {TaskId} {CommitSha} {Outcome} {CorrelationId} {ErrorCode}",
                record.WorkItemId, record.Operation, record.TaskId, record.CommitSha,
                record.Outcome, record.CorrelationId, record.ErrorCode);
        }
        else
        {
            _logger.LogInformation(AuditEventId,
                "V5 audit {WorkItemId} {Operation} {TaskId} {CommitSha} {Outcome} {CorrelationId} {ErrorCode}",
                record.WorkItemId, record.Operation, record.TaskId, record.CommitSha,
                record.Outcome, record.CorrelationId, "NONE");
        }

        return true;
    }

    private static bool IsFixedHex(string? value, int size) =>
        value is not null && value.Length == size && value.All(Uri.IsHexDigit);

    private static bool IsV5WorkItemId(string? value) =>
        value is { Length: 13 } &&
        Regex.IsMatch(value, @"^C[0-9]{2}-[0-9]{2}\.M[0-9]{2}\.[0-9]{2}$",
            RegexOptions.CultureInvariant);
}
