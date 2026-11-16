using Microsoft.Extensions.Logging;
using Worker = Solqaryn.Infrastructure.AutomationV5.C0501DesignTokenBoundedReviewWorker;

namespace Solqaryn.Infrastructure.AutomationV5;

public sealed record C0501DesignTokenAuditRecord(string Code, int ReviewedCount);

public interface IC0501DesignTokenAuditSink
{
    ValueTask<bool> WriteAsync(C0501DesignTokenAuditRecord record, CancellationToken ct);
}

public sealed class C0501DesignTokenLoggerAuditSink : IC0501DesignTokenAuditSink
{
    private readonly ILogger<C0501DesignTokenLoggerAuditSink> _logger;

    public C0501DesignTokenLoggerAuditSink(ILogger<C0501DesignTokenLoggerAuditSink> logger) =>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public ValueTask<bool> WriteAsync(C0501DesignTokenAuditRecord record, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Never log tenant, actor, token key, CSS, SHA, JWT or provider exceptions.
        _logger.LogInformation("Design token preview audit code={EventCode} reviewed={ReviewedCount}",
            record.Code, record.ReviewedCount);
        return ValueTask.FromResult(true);
    }
}

public interface IC0501DesignTokenAuditPreviewReader
{
    ValueTask<Worker.Result> ReadAsync(
        IReadOnlyList<Worker.Input> batch, int actorId, int tenantId,
        long revision, int sessionEpoch, string sourceSha, CancellationToken ct);
}

public sealed class C0501DesignTokenBoundedAuditReader : IC0501DesignTokenAuditPreviewReader
{
    private readonly Worker _worker;

    public C0501DesignTokenBoundedAuditReader(Worker worker) =>
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));

    public ValueTask<Worker.Result> ReadAsync(
        IReadOnlyList<Worker.Input> batch, int actorId, int tenantId,
        long revision, int sessionEpoch, string sourceSha, CancellationToken ct) =>
        _worker.PreviewAsync(batch, actorId, tenantId, revision, sessionEpoch, sourceSha, ct);
}

/// <summary>
/// C05-01.M03.06: bounded in-process audit of a SYNTHETIC theme review.
/// No hosted worker, durable audit, CSS effect, identity grant or live provider.
/// Instantiate per logical review session, never as a multi-tenant singleton.
/// </summary>
public sealed class C0501DesignTokenSafeAuditService
{
    public const int MaximumOperations = 128;
    private readonly IC0501DesignTokenAuditPreviewReader _reader;
    private readonly IC0501DesignTokenAuditSink _sink;
    private readonly SemaphoreSlim _singleFlight = new(1, 1);
    private readonly HashSet<string> _attempts = new(StringComparer.Ordinal);

    public C0501DesignTokenSafeAuditService(
        IC0501DesignTokenAuditPreviewReader reader, IC0501DesignTokenAuditSink sink)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public sealed record Result(string Code, int ReviewedCount)
    {
        public bool AuthorizesThemeMutation => false;
        public bool AuthorizesTenantAccess => false;
        public bool GrantsRbacOrJwt => false;
        public bool AppliesCss => false;
        public bool ReadsOrWritesMysql => false;
        public bool PersistsAudit => false;
        public bool StartsHostedWorker => false;
        public bool CertifiesAccessibility => false;
        public bool CertifiesProduction => false;
    }

    public async ValueTask<Result> ReviewAsync(
        string? operationKey, IReadOnlyList<Worker.Input>? batch,
        int actorId, int tenantId, long observedRevision,
        int sessionEpoch, string? observedSha, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!SafeOperationKey(operationKey) || actorId <= 0 || tenantId <= 0 ||
            observedRevision < 0 || sessionEpoch <= 0 || !CanonicalSha(observedSha) ||
            batch is null || batch.Count is < 1 or > Worker.MaximumBatch)
            return await DeniedAsync("THEME_AUDIT_TRUSTED_CONTEXT_REQUIRED", ct);

        // Take the caller's collection snapshot before any delegated call.
        var captured = batch.ToArray();
        if (captured.Any(item => item is null || item.Intent is null ||
            item.Intent.DirectWrite || item.Intent.PlatformCatalogMutation ||
            item.Intent.ClaimedTenantId != tenantId ||
            item.Intent.SessionEpoch != sessionEpoch ||
            item.Intent.ExpectedRevision != observedRevision ||
            !SafeOperationKey(item.AttemptKey) ||
            string.IsNullOrWhiteSpace(item.Intent.IdempotencyKey)))
            return await DeniedAsync("THEME_AUDIT_MIXED_SCOPE_DENIED", ct);

        if (!await _singleFlight.WaitAsync(0, ct))
            return await DeniedAsync("THEME_AUDIT_CONCURRENT_DENIED", ct);

        try
        {
            if (_attempts.Count >= MaximumOperations)
                return await DeniedAsync("THEME_AUDIT_CAPACITY_REAUTH_REQUIRED", ct);
            if (!_attempts.Add(operationKey!))
                return await DeniedAsync("THEME_AUDIT_DUPLICATE_OPERATION_DENIED", ct);

            Worker.Result preview;
            try
            {
                preview = await _reader.ReadAsync(
                    captured, actorId, tenantId, observedRevision,
                    sessionEpoch, observedSha!, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return await DeniedAsync("THEME_AUDIT_READER_UNAVAILABLE", ct);
            }

            if (preview is null ||
                preview.Code != "THEME_PREVIEW_ONLY_REAUTH_REQUIRED" ||
                preview.Items is null || preview.Items.Count != captured.Length ||
                !ExactOrderedPreview(preview.Items, captured.Length) ||
                preview.AuthorizesThemeChange || preview.AuthorizesTenantAccess ||
                preview.PersistsAnything || preview.ReadsOrWritesMysql ||
                preview.AppliesCss || preview.StartsHostedWorker ||
                preview.CertifiesAccessibility || preview.CertifiesProduction)
                return await DeniedAsync("THEME_AUDIT_AUTHORITY_DENIED", ct);

            const string code = "THEME_AUDIT_EXTERNAL_AUTH_REQUIRED";
            if (!await WriteAsync(code, captured.Length, ct))
                return new Result("THEME_AUDIT_SINK_UNAVAILABLE", 0);
            return new Result(code, captured.Length);
        }
        finally { _singleFlight.Release(); }
    }

    private async ValueTask<Result> DeniedAsync(string code, CancellationToken ct)
    {
        if (!await WriteAsync(code, 0, ct))
            return new Result("THEME_AUDIT_SINK_UNAVAILABLE", 0);
        return new Result(code, 0);
    }

    private async ValueTask<bool> WriteAsync(string code, int count, CancellationToken ct)
    {
        try { return await _sink.WriteAsync(new C0501DesignTokenAuditRecord(code, count), ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return false; }
    }

    // Every response must match the exact zero-based ordinal of its request.
    // Matching only count and decision codes allows forged, duplicated or reordered items.
    private static bool ExactOrderedPreview(IReadOnlyList<Worker.Item> items, int expectedCount)
    {
        if (items.Count != expectedCount) return false;
        for (var index = 0; index < expectedCount; index++)
        {
            var item = items[index];
            if (item is null || item.Ordinal != index ||
                !string.Equals(item.DecisionCode,
                    "AUTHENTICATED_THEME_ADAPTER_REQUIRED", StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static bool CanonicalSha(string? value) =>
        value is { Length: 40 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static bool SafeOperationKey(string? value) =>
        value is { Length: >= 3 and <= 96 } &&
        value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or
            >= '0' and <= '9' or '-' or '_' or '.');
}
