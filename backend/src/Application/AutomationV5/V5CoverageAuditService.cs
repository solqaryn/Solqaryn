using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Emits safe, structured operational audit records for V5 requirement coverage.
/// This service validates existing mappings, but it never writes a claim, grants access,
/// dispatches a task, or substitutes for the institutional A:Z audit ledger.
/// </summary>
public sealed class V5CoverageAuditService
{
    private static readonly EventId AcceptedEvent = new(530601, "V5CoverageAccepted");
    private static readonly EventId RejectedEvent = new(530602, "V5CoverageRejected");
    private static readonly EventId FailureEvent = new(530603, "V5CoverageSafeFailure");
    private readonly ILogger<V5CoverageAuditService> _logger;

    public V5CoverageAuditService(ILogger<V5CoverageAuditService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public V5RequirementCoverageRules.Verdict ValidateAndAudit(
        V5RequirementCoverageRules.Entry entry,
        IReadOnlySet<string> authorizedRequirementKeys,
        IReadOnlySet<string> existingWorkItemIds,
        string? exactCommitSha)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(authorizedRequirementKeys);
        ArgumentNullException.ThrowIfNull(existingWorkItemIds);

        if (!IsFullSha(exactCommitSha))
        {
            LogRejection("CHECK_SHA_MISMATCH");
            return V5RequirementCoverageRules.Verdict.Reject("CHECK_SHA_MISMATCH");
        }

        var verdict = V5RequirementCoverageRules.Validate(
            entry, authorizedRequirementKeys, existingWorkItemIds);
        if (!verdict.Accepted)
        {
            LogRejection(verdict.Code);
            return verdict;
        }

        // Only an irreversible digest of a catalog-validated identifier reaches logs.
        // Do not log requirement keys, URLs, source text, exception messages, or PII.
        _logger.LogInformation(AcceptedEvent,
            "V5 coverage accepted WorkItemDigest={WorkItemDigest} CommitSha={CommitSha}",
            Fingerprint(entry.WorkItemId), exactCommitSha);
        return verdict;
    }

    public V5RequirementCoverageRules.Verdict ValidateBatchAndAudit(
        IEnumerable<V5RequirementCoverageRules.Entry> entries,
        IReadOnlySet<string> authorizedRequirementKeys,
        IReadOnlySet<string> existingWorkItemIds,
        string? exactCommitSha)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(authorizedRequirementKeys);
        ArgumentNullException.ThrowIfNull(existingWorkItemIds);

        if (!IsFullSha(exactCommitSha))
        {
            LogRejection("CHECK_SHA_MISMATCH");
            return V5RequirementCoverageRules.Verdict.Reject("CHECK_SHA_MISMATCH");
        }

        var snapshot = entries.ToArray();
        var verdict = V5RequirementCoverageRules.ValidateBatch(
            snapshot, authorizedRequirementKeys, existingWorkItemIds);
        if (!verdict.Accepted)
        {
            LogRejection(verdict.Code);
            return verdict;
        }

        _logger.LogInformation(AcceptedEvent,
            "V5 coverage batch accepted Count={Count} CommitSha={CommitSha}",
            snapshot.Length, exactCommitSha);
        return verdict;
    }

    /// <summary>Records only a known failure category, never raw exception text.</summary>
    public void RecordSafeFailure(string? untrustedCode, string? exactCommitSha)
    {
        var safeCode = untrustedCode switch
        {
            "CLAIM_CONFLICT" => "CLAIM_CONFLICT",
            "DEPENDENCY_BLOCKED" => "DEPENDENCY_BLOCKED",
            "UNAUTHORIZED_ACTOR" => "UNAUTHORIZED_ACTOR",
            "AUDIT_REQUIRED" => "AUDIT_REQUIRED",
            "CI_FAILED" => "CI_FAILED",
            "CHECK_SHA_MISMATCH" => "CHECK_SHA_MISMATCH",
            _ => "UNCLASSIFIED_FAILURE"
        };
        _logger.LogWarning(FailureEvent,
            "V5 coverage operation failed Reason={Reason} CommitSha={CommitSha}",
            safeCode, IsFullSha(exactCommitSha) ? exactCommitSha : "UNVERIFIED");
    }

    private void LogRejection(string code) =>
        _logger.LogWarning(RejectedEvent, "V5 coverage rejected Reason={Reason}", code);

    private static bool IsFullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);

    private static string Fingerprint(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
