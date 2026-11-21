using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C01-03.M03.06: bounded, structured audit boundary for dynamic configuration.
/// No authorization, tenant grant, configuration value, JWT, SQL, Sheets write,
/// production sink or live logger registration is implemented here.
/// </summary>
public sealed class C0103DynamicConfigurationAuditLogger
{
    private readonly ILogger<C0103DynamicConfigurationAuditLogger> _logger;
    private static readonly EventId AuditEventId = new(10306, "C0103ConfigAudit");
    private static readonly HashSet<string> Operations = new(StringComparer.Ordinal)
    {
        "CONFIG_READ_PREFLIGHT", "CONFIG_WRITE_PREFLIGHT", "CATALOG_REVIEW",
        "CONFIG_REJECTED"
    };
    private static readonly HashSet<string> Outcomes = new(StringComparer.Ordinal)
    {
        "CHECKED", "DENIED", "ERROR"
    };
    private static readonly HashSet<string> SafeErrors = new(StringComparer.Ordinal)
    {
        "ACTOR_NOT_VERIFIED", "TENANT_MISMATCH", "RBAC_REVOKED",
        "STALE_VERSION", "CATALOG_UNAVAILABLE", "INVALID_KEY",
        "PROVIDER_UNAVAILABLE", "CANCELLED_OPERATION"
    };
    private static readonly string[] SensitivePrefixes =
    {
        "secret", "secrets.", "jwt.", "auth.", "security.",
        "database.", "connection.", "password", "token."
    };

    public C0103DynamicConfigurationAuditLogger(
        ILogger<C0103DynamicConfigurationAuditLogger> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public sealed record AuditEvent(
        int VerifiedActorId, int VerifiedTenantId,
        string? CanonicalKey, string? SourceSha, long StoredVersion,
        string? Operation, string? Outcome, Guid CorrelationId,
        string? ErrorCode = null);

    /// <summary>
    /// Only bounded allowlisted fields are accepted. The raw configuration
    /// key is never logged: a short SHA-256 digest provides safe correlation.
    /// Any missing or malformed identity/tenant proof is rejected before logging.
    /// Returning true means emitted to ILogger, never an authorization grant.
    /// </summary>
    public bool TryWrite(AuditEvent? record)
    {
        if (record is null || record.VerifiedActorId <= 0 ||
            record.VerifiedTenantId <= 0 || record.StoredVersion <= 0 ||
            record.CorrelationId == Guid.Empty ||
            !SafeKey(record.CanonicalKey) || !CanonicalSha(record.SourceSha) ||
            record.Operation is null || !Operations.Contains(record.Operation) ||
            record.Outcome is null || !Outcomes.Contains(record.Outcome) ||
            (record.Outcome == "CHECKED"
                ? record.ErrorCode is not null
                : record.ErrorCode is null || !SafeErrors.Contains(record.ErrorCode)))
            return false;

        var hashedKey = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(record.CanonicalKey!)))[..16];
        try
        {
            // ILogger can silently suppress disabled levels. A discarded audit
            // event must never be reported as emitted to the caller.
            var level = record.Outcome == "CHECKED" ? LogLevel.Information : LogLevel.Warning;
            if (!_logger.IsEnabled(level))
                return false;

            if (record.Outcome == "CHECKED")
                _logger.LogInformation(AuditEventId,
                    "C0103 config audit {Operation} {Outcome} {TenantId} {ActorId} {KeyDigest} {SourceSha} {Version} {CorrelationId}",
                    record.Operation, record.Outcome, record.VerifiedTenantId,
                    record.VerifiedActorId, hashedKey, record.SourceSha,
                    record.StoredVersion, record.CorrelationId);
            else
                _logger.LogWarning(AuditEventId,
                    "C0103 config audit {Operation} {Outcome} {TenantId} {ActorId} {KeyDigest} {SourceSha} {Version} {CorrelationId} {ErrorCode}",
                    record.Operation, record.Outcome, record.VerifiedTenantId,
                    record.VerifiedActorId, hashedKey, record.SourceSha,
                    record.StoredVersion, record.CorrelationId, record.ErrorCode);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // No logger exception or sensitive payload escapes this boundary.
            return false;
        }
        return true;
    }

    private static bool SafeKey(string? key) =>
        key is { Length: > 0 and <= 64 } &&
        !key.Contains("..", StringComparison.Ordinal) &&
        !SensitivePrefixes.Any(p => key.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
        key.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '.' or '_');

    private static bool CanonicalSha(string? sha) =>
        sha is { Length: 40 } &&
        sha.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
