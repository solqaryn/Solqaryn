using Microsoft.Extensions.Logging;
using System.Linq;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C15-01.M03.06: bounded non-authorizing in-process audit of requirement reviews.
/// No institutional write, global coverage assertion, real authentication, A5,
/// live provider or durable audit is performed by this component.
/// </summary>
public sealed class C1501RequirementCoverageSafeAuditSink
{
    private static readonly EventId AuditEvent = new(150106, "V5RequirementPreviewAudit");
    private readonly ILogger<C1501RequirementCoverageSafeAuditSink> _logger;

    public C1501RequirementCoverageSafeAuditSink(ILogger<C1501RequirementCoverageSafeAuditSink> logger)
        => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public sealed record Evidence(
        int VerifiedTenantId, int ResourceTenantId,
        bool BackendIdentityVerified, bool MembershipActive,
        bool ReviewerPermissionCurrent, bool PermissionRevoked,
        bool SourceAuthenticated, string? ExpectedHeadSha,
        string? ObservedHeadSha, string? InternalCorrelationId);

    public readonly record struct Receipt(string Code, int ReviewedCount)
    {
        public bool GrantsInstitutionalWrite => false;
        public bool GrantsA5 => false;
        public bool CertifiesGlobalCoverage => false;
        public bool AuthorizesTenantRead => false;
        public bool AuthorizesQaOrProduction => false;
        public bool CertifiesDurableAudit => false;
    }

    public Receipt Record(C1501RequirementCoverageBoundedReviewWorker.Review? review,
        Evidence? evidence, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var trusted = ValidEvidence(evidence) && ValidReview(review);
        var outcome = trusted ? "REQUIREMENT_PREVIEW_NOT_CERTIFIED" : "REQUIREMENT_AUDIT_DENIED";
        var correlation = trusted ? evidence!.InternalCorrelationId! : "REDACTED";
        var count = trusted ? review!.Items.Count : 0;
        try
        {
            if (!_logger.IsEnabled(LogLevel.Information))
                return new("REQUIREMENT_AUDIT_LOGGER_UNAVAILABLE", 0);
            cancellationToken.ThrowIfCancellationRequested();
            // Fixed event and codes, canonical GUID and bounded count only:
            // never requirement, work ID, tenant, actor, JWT, SHA, error or PII.
            _logger.LogInformation(AuditEvent,
                "V5 requirements preview audit {Outcome} {CorrelationId} {ReviewedCount}",
                outcome, correlation, count);
            return new(trusted ? "SAFE_REQUIREMENT_PREVIEW_AUDIT_RECORDED"
                               : "SAFE_REQUIREMENT_DENIAL_AUDIT_RECORDED", count);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("REQUIREMENT_AUDIT_LOGGER_UNAVAILABLE", 0);
        }
    }

    private static bool ValidEvidence(Evidence? e) =>
        e is not null && e.VerifiedTenantId > 0 &&
        e.VerifiedTenantId == e.ResourceTenantId &&
        e.BackendIdentityVerified && e.MembershipActive &&
        e.ReviewerPermissionCurrent && !e.PermissionRevoked &&
        e.SourceAuthenticated && FullSha(e.ExpectedHeadSha) &&
        string.Equals(e.ExpectedHeadSha, e.ObservedHeadSha, StringComparison.Ordinal) &&
        Guid.TryParseExact(e.InternalCorrelationId, "N", out _);

    private static bool ValidReview(C1501RequirementCoverageBoundedReviewWorker.Review? r)
    {
        if (r is null ||
            !string.Equals(r.Code, "REVIEW_PREVIEW_REQUIRES_INDEPENDENT_EVIDENCE",
                StringComparison.Ordinal) || r.Items is null ||
            r.Items.Count is < 1 or > C1501RequirementCoverageBoundedReviewWorker.MaximumBatch)
            return false;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var items = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in r.Items)
            if (item is null || !SafeKey(item.RequirementKey) ||
                !SafeKey(item.WorkItemId) ||
                item.Code != "EXTERNAL_CAUSAL_EVIDENCE_REQUIRED" ||
                !keys.Add(item.RequirementKey) || !items.Add(item.WorkItemId))
                return false;
        return true;
    }

    private static bool FullSha(string? value) =>
        value is { Length: 40 } &&
        value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static bool SafeKey(string? value) =>
        value is { Length: >= 3 and <= 128 } &&
        value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-' or '_' or '.');
}
