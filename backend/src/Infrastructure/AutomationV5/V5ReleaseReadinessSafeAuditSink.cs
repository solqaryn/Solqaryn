using Microsoft.Extensions.Logging;
using Worker = Solqaryn.Application.AutomationV5.V5ReleaseReadinessBoundedReviewWorker;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Fresh authenticated backend authority, not derived from browser or work-item payload.
/// No implementation, hosted worker or provider registration is included.
/// </summary>
public interface IV5ReleaseReadinessSafeAuditAuthority
{
    ValueTask<V5ReleaseReadinessSafeAuditSink.VerifiedContext?> CurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// C14-04.M03.06: bounded structured readiness audit. A logger receipt is NOT
/// an A5 approval, grant, restoration, deployment, release or durable audit.
/// </summary>
public sealed class V5ReleaseReadinessSafeAuditSink
{
    private static readonly EventId AuditEvent = new(14040306, "V5ReleaseReadinessReviewAudit");
    private static readonly HashSet<string> AllowedCodes = new(StringComparer.Ordinal)
    {
        "EXTERNAL_BROWSER_AND_A5_REVIEW_REQUIRED",
        "AUTHENTICATED_REVIEW_INBOX_UNAVAILABLE",
        "AUTHENTICATED_BOUNDED_DEV_INBOX_REQUIRED",
        "INVALID_READ_ONLY_WORK_ITEM",
        "DUPLICATE_WORK_ITEM_IN_BATCH",
        "READ_ONLY_SNAPSHOT_INSPECTION_UNAVAILABLE",
        "REVIEW_EVIDENCE_DENIED"
    };

    private readonly IV5ReleaseReadinessSafeAuditAuthority _authority;
    private readonly ILogger<V5ReleaseReadinessSafeAuditSink> _logger;

    public V5ReleaseReadinessSafeAuditSink(
        IV5ReleaseReadinessSafeAuditAuthority authority,
        ILogger<V5ReleaseReadinessSafeAuditSink> logger)
    {
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public sealed record VerifiedContext(
        int ActorId, int ActorTenantId, int ResourceTenantId,
        string? SourceSha, string? ObservedSha, string? Environment,
        bool BackendAuthenticated, bool MembershipCurrent,
        bool RoleCurrent, bool ExplicitReadPermissionCurrent,
        bool ReviewerDistinctFromAuthor, bool AuditFenceCurrent, bool Revoked);

    public readonly record struct Receipt(string Code)
    {
        public bool LoggerAccepted => Code == "SAFE_READINESS_PREVIEW_LOGGED";
        public bool GrantsAccess => false;
        public bool ApprovesA5 => false;
        public bool AuthorizesDeployment => false;
        public bool CertifiesRestore => false;
        public bool CertifiesDurableAudit => false;
        public bool WritesDatabase => false;
        public bool AcknowledgesQueue => false;
        public bool RunsProviderOrWorker => false;
        public bool ExposesTenantData => false;
    }

    private static bool CanonicalSha(string? sha) =>
        sha is { Length: 40 } &&
        sha.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    public async ValueTask<Receipt> RecordAsync(
        Worker.Outcome outcome, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Validate fixed vocabulary BEFORE requesting potentially sensitive authority.
        if (outcome.Code is null || !AllowedCodes.Contains(outcome.Code) ||
            outcome.ItemsInspected is < 0 or > 16)
            return new("UNRECOGNIZED_OR_UNTRUSTED_REVIEW_OUTCOME");

        var isReviewRequired = outcome.Code == "EXTERNAL_BROWSER_AND_A5_REVIEW_REQUIRED";
        var level = isReviewRequired ? LogLevel.Information : LogLevel.Warning;
        if (!_logger.IsEnabled(level))
            return new("AUDIT_LOGGER_UNAVAILABLE");

        VerifiedContext? current;
        try
        {
            current = await _authority.CurrentAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("AUDIT_AUTHORITY_UNAVAILABLE");
        }

        if (current is null ||
            current.ActorId <= 0 || current.ActorTenantId <= 0 ||
            current.ResourceTenantId != current.ActorTenantId ||
            !CanonicalSha(current.SourceSha) ||
            !string.Equals(current.SourceSha, current.ObservedSha, StringComparison.Ordinal) ||
            !string.Equals(current.Environment, "dev", StringComparison.Ordinal) ||
            !current.BackendAuthenticated || !current.MembershipCurrent ||
            !current.RoleCurrent || !current.ExplicitReadPermissionCurrent ||
            !current.ReviewerDistinctFromAuthor || !current.AuditFenceCurrent ||
            current.Revoked)
            return new("CURRENT_BACKEND_RELEASE_REVIEW_AUTHORIZATION_REQUIRED");

        // Only fixed code, event and severity can cross the logging boundary.
        try
        {
            _logger.Log(level, AuditEvent,
                "V5 release readiness review {OutcomeCode}", outcome.Code);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("AUDIT_LOGGER_UNAVAILABLE");
        }

        return new("SAFE_READINESS_PREVIEW_LOGGED");
    }
}
