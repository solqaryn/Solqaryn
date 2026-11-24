using System.Text.RegularExpressions;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C01-04.M03.06: in-process, non-authorizing QA evidence audit boundary.
/// A future authenticated backend adapter must supply verifiable observations.
/// Neither this writer nor an injected sink can certify CI, A5, tenancy or access.
/// </summary>
public interface IV5UniversalCaseFileQaEvidenceSource
{
    ValueTask<V5UniversalCaseFileQaEvidenceAudit.Observation?> ReadAsync(
        string microtaskId, string routeKey, CancellationToken cancellationToken);
}

public interface IV5UniversalCaseFileQaAuditSink
{
    ValueTask WriteAsync(
        V5UniversalCaseFileQaEvidenceAudit.SafeAuditEntry entry,
        CancellationToken cancellationToken);
}

/// <summary>
/// Optional numeric storage-scope binding for durable QA sinks. This comparison
/// prevents accidental A/B audit misrouting; it is NOT JWT/role authorization.
/// </summary>
public interface IV5UniversalCaseFileQaScopeBoundSink : IV5UniversalCaseFileQaAuditSink
{
    bool MatchesCanonicalScope(string tenantScope, string caseFileScope);
}

public sealed class V5UniversalCaseFileQaEvidenceAudit
{
    private readonly IV5UniversalCaseFileQaEvidenceSource _source;
    private readonly IV5UniversalCaseFileQaAuditSink _sink;
    private readonly TimeProvider _timeProvider;

    private static readonly Regex MicrotaskPattern = new(
        @"\AC[0-9]{2}-[0-9]{2}\.M[0-9]{2}\.[0-9]{2}\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ShaPattern = new(
        @"\A[0-9a-f]{40}\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> Routes = new(StringComparer.Ordinal)
    {
        "casefile.timeline", "casefile.authorization",
        "casefile.attachment", "casefile.qa-evidence"
    };
    private static readonly HashSet<string> Checks = new(StringComparer.Ordinal)
    {
        "verify-solqaryn-project-scope",
        "SOLQARYN - Seguridad multiempresa",
        "Backend Release y pruebas",
        "Authorization, files, secrets and tenant gate"
    };
    private static readonly HashSet<string> States = new(StringComparer.Ordinal)
    {
        "queued", "in_progress", "completed"
    };
    private static readonly HashSet<string> Conclusions = new(StringComparer.Ordinal)
    {
        "success", "failure", "cancelled", "skipped", "pending"
    };

    public sealed record Observation(
        string? MicrotaskId, string? RouteKey, string? SourceSha,
        string? VerifiedTenantScope, string? VerifiedCaseFileScope,
        bool SourceAuthenticated, bool SourceOwnedByTenant,
        string? CheckName, string? CheckStatus, string? CheckConclusion,
        DateTimeOffset ObservedAtUtc, string? RawProviderDiagnostic = null);

    // Intentionally excludes tenant, case ID, user, token, file name,
    // provider exception and any raw evidence payload.
    public sealed record SafeAuditEntry(
        string MicrotaskId, string RouteKey, string SourceSha,
        string CheckName, string CheckStatus, string CheckConclusion,
        DateTimeOffset ObservedAtUtc, string VerificationClass);

    public readonly record struct Verdict(string Code)
    {
        public bool CertifiesCI => false;
        public bool CertifiesA5 => false;
        public bool GrantsTenantAccess => false;
        public bool AuthorizesCaseWrite => false;
        public bool AuthorizesAttachmentRead => false;
        public bool AllowsProduction => false;
    }

    public V5UniversalCaseFileQaEvidenceAudit(
        IV5UniversalCaseFileQaEvidenceSource source,
        IV5UniversalCaseFileQaAuditSink sink,
        TimeProvider? timeProvider = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<Verdict> CaptureAsync(
        string? microtaskId, string? routeKey, string? sourceSha,
        string? trustedTenantScope, string? trustedCaseFileScope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (microtaskId is null || !MicrotaskPattern.IsMatch(microtaskId) ||
            routeKey is null || !Routes.Contains(routeKey) ||
            sourceSha is null || !ShaPattern.IsMatch(sourceSha) ||
            !SafeScope(trustedTenantScope) || !SafeScope(trustedCaseFileScope))
            return new("CASEFILE_QA_SCOPE_INVALID");

        // Durable sinks are fixed to canonical SQL tenant/case IDs. Refuse a
        // different or opaque caller scope before consulting any evidence source.
        // An authenticated case-file reader and real membership/ownership remain required.
        if (_sink is IV5UniversalCaseFileQaScopeBoundSink boundSink &&
            !boundSink.MatchesCanonicalScope(trustedTenantScope!, trustedCaseFileScope!))
            return new("CASEFILE_QA_DURABLE_SCOPE_MISMATCH");

        Observation? observation;
        try
        {
            observation = await _source.ReadAsync(microtaskId, routeKey, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("CASEFILE_QA_SOURCE_UNAVAILABLE");
        }
        // A source can ignore a cancellation and still return a valid envelope.
        // Enforce cancellation again before evaluating or writing any audit record.
        cancellationToken.ThrowIfCancellationRequested();
        if (observation is null || !observation.SourceAuthenticated ||
            !observation.SourceOwnedByTenant ||
            !string.Equals(observation.MicrotaskId, microtaskId, StringComparison.Ordinal) ||
            !string.Equals(observation.RouteKey, routeKey, StringComparison.Ordinal) ||
            !string.Equals(observation.SourceSha, sourceSha, StringComparison.Ordinal) ||
            !string.Equals(observation.VerifiedTenantScope, trustedTenantScope, StringComparison.Ordinal) ||
            !string.Equals(observation.VerifiedCaseFileScope, trustedCaseFileScope, StringComparison.Ordinal))
            return new("CASEFILE_QA_TRUSTED_EVIDENCE_REQUIRED");

        // Do not recycle an old or future-dated provider observation as a fresh QA receipt.
        // An authenticated source and exact-SHA CI proof remain separate requirements.
        var nowUtc = _timeProvider.GetUtcNow();
        if (observation.CheckName is null || !Checks.Contains(observation.CheckName) ||
            observation.CheckStatus is null || !States.Contains(observation.CheckStatus) ||
            observation.CheckConclusion is null || !Conclusions.Contains(observation.CheckConclusion) ||
            observation.ObservedAtUtc == default ||
            observation.ObservedAtUtc.Offset != TimeSpan.Zero ||
            observation.ObservedAtUtc < nowUtc.AddMinutes(-10) ||
            observation.ObservedAtUtc > nowUtc.AddSeconds(5) ||
            (observation.CheckStatus == "completed" && observation.CheckConclusion == "pending") ||
            (observation.CheckStatus != "completed" && observation.CheckConclusion != "pending"))
            return new("CASEFILE_QA_CHECK_OR_TIME_INVALID");

        var entry = new SafeAuditEntry(
            microtaskId, routeKey, sourceSha, observation.CheckName,
            observation.CheckStatus, observation.CheckConclusion,
            observation.ObservedAtUtc, "UNVERIFIED_EXTERNAL_REVIEW_REQUIRED");
        try
        {
            await _sink.WriteAsync(entry, cancellationToken);
            // An async sink may swallow cancellation and return without a durable write.
            // Never produce a successful receipt once the caller cancelled during sink I/O.
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("CASEFILE_QA_AUDIT_SINK_UNAVAILABLE");
        }

        // The result is only a sanitized, structured, internal audit receipt.
        // A source-supplied 'success' cannot prove external CI or grant A5.
        return new("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING");
    }

    private static bool SafeScope(string? scope) =>
        // Business tenant and case-file scopes may use a canonical positive
        // single-digit SQL ID; a single opaque letter and zero stay invalid.
        // Reject 01/001 aliases to avoid mismatched audit/idempotency scope keys.
        scope is { Length: >= 1 and <= 128 } &&
        (scope.Length > 1 || scope[0] is >= '1' and <= '9') &&
        // A leading minus is never a canonical scope: -1 must fail before source I/O.
        // Hyphens inside opaque identifiers like tenant-A remain permitted.
        scope[0] != '-' &&
        scope.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_') &&
        (!scope.All(char.IsAsciiDigit) || scope[0] is >= '1' and <= '9');
}
