using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.AutomationV5;

public interface IC0201TenantAuditPreviewReader
{
    ValueTask<C0201TenantPreferenceBoundedRevalidationWorker.Review> ReadAsync(
        IReadOnlyList<C0201TenantPreferenceBoundedRevalidationWorker.Input>? inputs,
        int authenticatedActorId, long revision, string? sourceSha,
        CancellationToken cancellationToken);
}

/// <summary>Read-only wrapper for the existing M03.05 bounded worker.</summary>
public sealed class C0201TenantAuditPreviewReader : IC0201TenantAuditPreviewReader
{
    private readonly C0201TenantPreferenceBoundedRevalidationWorker _worker;
    public C0201TenantAuditPreviewReader(
        C0201TenantPreferenceBoundedRevalidationWorker worker) =>
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));

    public ValueTask<C0201TenantPreferenceBoundedRevalidationWorker.Review> ReadAsync(
        IReadOnlyList<C0201TenantPreferenceBoundedRevalidationWorker.Input>? inputs,
        int authenticatedActorId, long revision, string? sourceSha,
        CancellationToken cancellationToken) =>
        _worker.PreviewAsync(inputs, authenticatedActorId, revision,
            sourceSha, cancellationToken);
}

/// <summary>Only fixed safe codes and counts, never actor/tenant/PII/cookies/JWT.</summary>
public sealed record C0201TenantAuditRecord(string Code, int ReviewedCount);

public interface IC0201TenantAuditSink
{
    ValueTask<bool> WriteAsync(C0201TenantAuditRecord record,
        CancellationToken cancellationToken);
}

/// <summary>Structured ILogger only; does not guarantee durable delivery.</summary>
public sealed class C0201TenantAuditLoggerSink : IC0201TenantAuditSink
{
    private readonly ILogger<C0201TenantAuditLoggerSink> _logger;
    public C0201TenantAuditLoggerSink(
        ILogger<C0201TenantAuditLoggerSink> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public ValueTask<bool> WriteAsync(C0201TenantAuditRecord record,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation(
            "Tenant context preview audit event={EventCode} count={ReviewedCount}",
            record.Code, record.ReviewedCount);
        return ValueTask.FromResult(true);
    }
}

/// <summary>
/// C02-01.M03.06: single-flight fail-closed synthetic tenant-context audit.
/// One instance per logical trusted session, never singleton across actors.
/// This neither switches tenant nor issues JWT, authorizes HTTP or writes DB.
/// </summary>
public sealed class C0201TenantPreferenceSafeAuditService
{
    public const int MaximumAttemptsPerInstance = 128;
    private readonly IC0201TenantAuditPreviewReader _reader;
    private readonly IC0201TenantAuditSink _sink;
    private readonly SemaphoreSlim _singleFlight = new(1, 1);
    private readonly HashSet<string> _attemptKeys = new(StringComparer.Ordinal);

    public C0201TenantPreferenceSafeAuditService(
        IC0201TenantAuditPreviewReader reader, IC0201TenantAuditSink sink)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public sealed record Result(string Code, int ReviewedCount)
    {
        public bool AuthorizesTenantAccess => false;
        public bool SwitchesTenantSession => false;
        public bool IssuesJwt => false;
        public bool WritesPreference => false;
        public bool PersistsAudit => false;
        public bool ReadsOrWritesMySql => false;
        public bool CertifiesProduction => false;
    }

    public async ValueTask<Result> ReviewAsync(
        string? operationKey,
        IReadOnlyList<C0201TenantPreferenceBoundedRevalidationWorker.Input>? inputs,
        int authenticatedActorId, long revision, string? sourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!SafeKey(operationKey) || authenticatedActorId <= 0 ||
            revision <= 0 || !FullSha(sourceSha))
            return await DenyAsync("AUDIT_INVALID_TRUSTED_CONTEXT", cancellationToken);

        if (!await _singleFlight.WaitAsync(0, cancellationToken))
            return await DenyAsync("AUDIT_CONCURRENT_SELECTION_DENIED", cancellationToken);

        try
        {
            if (_attemptKeys.Count >= MaximumAttemptsPerInstance)
                return await DenyAsync("AUDIT_CAPACITY_REAUTH_REQUIRED", cancellationToken);
            if (!_attemptKeys.Add(operationKey!))
                return await DenyAsync("AUDIT_DUPLICATE_SELECTION_DENIED", cancellationToken);

            C0201TenantPreferenceBoundedRevalidationWorker.Review preview;
            try
            {
                preview = await _reader.ReadAsync(inputs, authenticatedActorId,
                    revision, sourceSha, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return await DenyAsync("AUDIT_AUTHORITY_READER_UNAVAILABLE", cancellationToken);
            }

            if (preview is null || preview.Code !=
                "PREVIEW_ONLY_AUTHENTICATED_BACKEND_REISSUE_REQUIRED" ||
                inputs is null || preview.Items is null ||
                preview.Items.Count != inputs.Count ||
                preview.Items.Count is < 1 or >
                    C0201TenantPreferenceBoundedRevalidationWorker.MaximumBatch ||
                preview.Items.Any(x => x is null || !Allowed(x.DecisionCode)))
                return await DenyAsync("AUDIT_AUTHORITY_DENIED", cancellationToken);

            // A5 P2: never trust a preview reader's count or row identity.
            // A forged, truncated, reordered or duplicate attempt must not
            // produce a success audit even when the decision code looks safe.
            var seenAttempts = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < inputs.Count; i++)
            {
                var entry = inputs[i];
                var item = preview.Items[i];
                if (entry is null || entry.Request is null ||
                    !SafeKey(entry.AttemptKey) ||
                    !seenAttempts.Add(entry.AttemptKey) ||
                    item is null ||
                    !string.Equals(item.AttemptKey, entry.AttemptKey,
                        StringComparison.Ordinal))
                    return await DenyAsync("AUDIT_AUTHORITY_DENIED",
                        cancellationToken);
            }

            bool isSwitch = inputs.All(x => x.Request!.SwitchingEnterprise);
            // An unverified switch decision must never be reported as a
            // verified switch, nor may a switch decision appear for a
            // non-switch input. This remains preview-only, never access.
            if (inputs.Any(x => x.Request!.SwitchingEnterprise) != isSwitch ||
                preview.Items.Any(x =>
                    (x.DecisionCode == "SWITCH_REISSUE_REQUIRED") != isSwitch))
                return await DenyAsync("AUDIT_AUTHORITY_DENIED",
                    cancellationToken);
            string code = isSwitch ? "AUDIT_SWITCH_REISSUE_PREVIEW" :
                preview.Items.Any(x => x.DecisionCode == "SELECT_VERIFIED_TENANT")
                    ? "AUDIT_SELECTION_CHOICE_PREVIEW"
                    : "AUDIT_SELECTION_REISSUE_PREVIEW";

            if (!await EmitAsync(code, preview.Items.Count, cancellationToken))
                return new Result("AUDIT_SINK_UNAVAILABLE", 0);
            return new Result(code, preview.Items.Count);
        }
        finally { _singleFlight.Release(); }
    }

    private async ValueTask<Result> DenyAsync(string code,
        CancellationToken cancellationToken)
    {
        if (!await EmitAsync(code, 0, cancellationToken))
            return new Result("AUDIT_SINK_UNAVAILABLE", 0);
        return new Result(code, 0);
    }

    private async ValueTask<bool> EmitAsync(string code, int count,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _sink.WriteAsync(new C0201TenantAuditRecord(code, count),
                cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Never reveal exception text, potentially sensitive.
            return false;
        }
    }

    private static bool Allowed(string? code) =>
        code is "ONE_TENANT_REISSUE_REQUIRED" or
            "PREFERENCE_REISSUE_REQUIRED" or
            "CHOICE_REISSUE_REQUIRED" or
            "SWITCH_REISSUE_REQUIRED" or
            "SELECT_VERIFIED_TENANT" or
            "CURRENT_SESSION_BACKEND_CHECK_REQUIRED";

    private static bool SafeKey(string? key) =>
        key is { Length: >= 3 and <= 96 } &&
        key.All(c => (c >= 'a' && c <= 'z') ||
                     (c >= 'A' && c <= 'Z') ||
                     (c >= '0' && c <= '9') || c is '-' or '_' or '.');

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } &&
        sha.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
}
