using System.Globalization;
using System.Text.Json;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Reads the directed GitHub Actions job from the fixed public QA provider.
/// Returns only corroborated build identity and status. This is not a
/// tenant/case-file authorization source, CI certification, or A5.
/// </summary>
public sealed class V5UniversalCaseFileQaGitHubJobReader(IHttpClientFactory clients)
{
    private const string Repository = "solqaryn/Solqaryn";
    private const string Microtask = "C01-04.M03.06";
    private const string WorkflowPath = ".github/workflows/v23-c01-04-m03-06-durable-qa-mysql84.yml";
    private const string Root = "repos/solqaryn/Solqaryn/";
    private const string CheckPrefix = "https://api.github.com/repos/solqaryn/Solqaryn/check-runs/";
    private const int MaximumBytes = 512_000;
    private static readonly Uri Provider = new("https://api.github.com/");

    public sealed record VerifiedJob(
        string MicrotaskId, string SourceSha, long WorkflowId, long RunId,
        int Attempt, long JobId, long CheckRunId, long CheckSuiteId,
        string CheckName, string Status, string Conclusion)
    {
        public V5UniversalCaseFileQaEvidenceAudit.ProviderJobIdentity ToAuditIdentity() =>
            new(MicrotaskId, SourceSha, WorkflowId, RunId, Attempt, JobId,
                CheckRunId, CheckSuiteId, CheckName, Status, Conclusion);
    }

    /// <summary>
    /// Discover the immutable attempt from the canonical run/job link supplied by
    /// the check list. The job and check IDs need not be equal. All discovery and
    /// corroboration share a single deadline and use fixed provider paths.
    /// </summary>
    public async Task<VerifiedJob?> ReadDirectedFromCheckAsync(
        string? microtaskId, string? sha, long checkRunId, long checkSuiteId,
        string? detailsUrl, string? conclusion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (microtaskId != Microtask || sha is not { Length: 40 } ||
            !sha.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f') ||
            checkRunId <= 0 || checkSuiteId <= 0 ||
            conclusion is not ("success" or "failure" or "cancelled" or "skipped") ||
            !TryJobDetails(detailsUrl, out var runId, out var jobId))
            return null;

        using var client = clients.CreateClient("SolqarynPublicGitHubQa");
        if (client.BaseAddress != Provider) return null;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var budget = TimeSpan.FromSeconds(12);
        deadline.CancelAfter(client.Timeout != Timeout.InfiniteTimeSpan && client.Timeout < budget
            ? client.Timeout : budget);
        var ct = deadline.Token;

        try
        {
            var runNumber = runId.ToString(CultureInfo.InvariantCulture);
            using var jobsDoc = await ReadAsync(client, Root + "actions/runs/" +
                runNumber + "/jobs?filter=all&per_page=100", ct);
            if (jobsDoc is null) return null;
            var jobs = Prop(jobsDoc.RootElement, "jobs");
            var count = Number(jobsDoc.RootElement, "total_count");
            if (count is < 1 or > 100 || jobs.ValueKind != JsonValueKind.Array ||
                jobs.GetArrayLength() != count)
                return null;
            var ids = new HashSet<long>();
            foreach (var candidate in jobs.EnumerateArray())
                if (Number(candidate, "id") is not > 0 || !ids.Add(Number(candidate, "id")))
                    return null;
            var matched = jobs.EnumerateArray().Where(x => Number(x, "id") == jobId).ToArray();
            if (matched.Length != 1) return null;
            var job = matched[0];
            var attempt = Number(job, "run_attempt");
            if (attempt is < 1 or > int.MaxValue || Number(job, "run_id") != runId ||
                Str(job, "head_sha") != sha || Str(job, "head_branch") != "dev" ||
                Str(job, "name") != V5UniversalCaseFileQaEvidenceAudit.DirectedDurableCheckName ||
                Str(job, "status") != "completed" || Str(job, "conclusion") != conclusion ||
                Str(job, "html_url") != detailsUrl ||
                !TryCheckId(Str(job, "check_run_url"), out var discoveredCheck) ||
                discoveredCheck != checkRunId)
                return null;

            // Pass the linked token so the original deadline also bounds the
            // three existing immutable-attempt requests.
            var verified = await ReadDirectedAsync(microtaskId, sha, runId, (int)attempt, jobId, ct);
            ct.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();
            return verified is not null && verified.CheckRunId == checkRunId &&
                verified.CheckSuiteId == checkSuiteId && verified.Conclusion == conclusion
                ? verified : null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return null; }
        catch (HttpRequestException) { return null; }
        catch (IOException) { return null; }
        catch (JsonException) { return null; }
    }

    private static bool TryJobDetails(string? url, out long runId, out long jobId)
    {
        runId = jobId = 0;
        const string prefix = "https://github.com/" + Repository + "/actions/runs/";
        if (url is null || !url.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var parts = url[prefix.Length..].Split("/job/", StringSplitOptions.None);
        return parts.Length == 2 &&
            long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out runId) &&
            long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out jobId) &&
            runId > 0 && jobId > 0 &&
            parts[0] == runId.ToString(CultureInfo.InvariantCulture) &&
            parts[1] == jobId.ToString(CultureInfo.InvariantCulture);
    }

    public async Task<VerifiedJob?> ReadDirectedAsync(
        string? microtaskId, string? sha, long runId, int attempt, long jobId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (microtaskId != Microtask || sha is not { Length: 40 } ||
            !sha.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f') ||
            runId <= 0 || attempt <= 0 || jobId <= 0)
            return null;

        var runNumber = runId.ToString(CultureInfo.InvariantCulture);
        var attemptNumber = attempt.ToString(CultureInfo.InvariantCulture);
        var jobNumber = jobId.ToString(CultureInfo.InvariantCulture);
        var details = "https://github.com/" + Repository + "/actions/runs/" +
                      runNumber + "/job/" + jobNumber;
        const string name = V5UniversalCaseFileQaEvidenceAudit.DirectedDurableCheckName;

        using var client = clients.CreateClient("SolqarynPublicGitHubQa");
        if (client.BaseAddress != Provider) return null;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var budget = TimeSpan.FromSeconds(12);
        deadline.CancelAfter(client.Timeout != Timeout.InfiniteTimeSpan && client.Timeout < budget
            ? client.Timeout : budget);
        var ct = deadline.Token;

        try
        {
            // The fixed named client disables redirects. Never fetch a URL supplied
            // by the response. All three requests share a single bounded deadline.
            // Read the requested immutable attempt, never the mutable latest-run metadata.
            using var runDoc = await ReadAsync(client, Root + "actions/runs/" +
                runNumber + "/attempts/" + attemptNumber, ct);
            if (runDoc is null) return null;
            var run = runDoc.RootElement;
            var workflowId = Number(run, "workflow_id");
            var suiteId = Number(run, "check_suite_id");
            var conclusion = Str(run, "conclusion");
            if (Number(run, "id") != runId || Number(run, "run_attempt") != attempt ||
                Str(run, "head_sha") != sha || Str(run, "head_branch") != "dev" ||
                Str(run, "event") != "push" || Str(run, "path") != WorkflowPath ||
                Str(Prop(run, "repository"), "full_name") != Repository ||
                Str(Prop(run, "head_repository"), "full_name") != Repository ||
                workflowId <= 0 || suiteId <= 0 || Str(run, "status") != "completed" ||
                conclusion is not ("success" or "failure" or "cancelled" or "skipped"))
                return null;

            using var jobsDoc = await ReadAsync(client, Root + "actions/runs/" +
                runNumber + "/attempts/" + attemptNumber + "/jobs?per_page=100", ct);
            if (jobsDoc is null) return null;
            var jobs = Prop(jobsDoc.RootElement, "jobs");
            // This directed workflow has exactly one job. Reject partial pages,
            // unexpected additional jobs and ambiguous names rather than guessing.
            if (Number(jobsDoc.RootElement, "total_count") != 1 ||
                jobs.ValueKind != JsonValueKind.Array || jobs.GetArrayLength() != 1)
                return null;
            var job = jobs[0];
            var checkUrl = Str(job, "check_run_url");
            if (Number(job, "id") != jobId || Number(job, "run_id") != runId ||
                Number(job, "run_attempt") != attempt || Str(job, "head_sha") != sha ||
                Str(job, "head_branch") != "dev" || Str(job, "name") != name ||
                Str(job, "status") != "completed" || Str(job, "conclusion") != conclusion ||
                Str(job, "html_url") != details ||
                !TryCheckId(checkUrl, out var checkId))
                return null;

            using var checksDoc = await ReadAsync(client, Root + "commits/" + sha +
                "/check-runs?per_page=100&filter=all", ct);
            if (checksDoc is null) return null;
            var checks = Prop(checksDoc.RootElement, "check_runs");
            var count = Number(checksDoc.RootElement, "total_count");
            if (count is < 1 or > 100 || checks.ValueKind != JsonValueKind.Array ||
                checks.GetArrayLength() != count)
                return null;
            var matched = checks.EnumerateArray().Where(x => Number(x, "id") == checkId).ToArray();
            if (matched.Length != 1) return null;
            var check = matched[0];
            if (Str(check, "name") != name || Str(check, "head_sha") != sha ||
                Str(check, "status") != "completed" || Str(check, "conclusion") != conclusion ||
                Str(check, "details_url") != details ||
                Number(Prop(check, "check_suite"), "id") != suiteId ||
                Number(Prop(check, "app"), "id") != 15368 ||
                Str(Prop(check, "app"), "slug") != "github-actions")
                return null;
            ct.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();
            return new VerifiedJob(Microtask, sha, workflowId, runId, attempt, jobId,
                checkId, suiteId, name, "completed", conclusion);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return null; }
        catch (HttpRequestException) { return null; }
        catch (IOException) { return null; }
        catch (JsonException) { return null; }
    }

    private static async Task<JsonDocument?> ReadAsync(HttpClient client, string path, CancellationToken ct)
    {
        using var response = await client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength is > MaximumBytes)
            return null;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var bounded = new MemoryStream();
        var buffer = new byte[16_384];
        while (true)
        {
            var length = Math.Min(buffer.Length, MaximumBytes + 1 - (int)bounded.Length);
            var n = await stream.ReadAsync(buffer.AsMemory(0, length), ct);
            if (n == 0) break;
            if (bounded.Length + n > MaximumBytes) return null;
            await bounded.WriteAsync(buffer.AsMemory(0, n), ct);
        }
        bounded.Position = 0;
        var document = await JsonDocument.ParseAsync(bounded,
            new JsonDocumentOptions { MaxDepth = 20 }, ct);
        if (document.RootElement.ValueKind == JsonValueKind.Object && Unique(document.RootElement))
            return document;
        document.Dispose();
        return null;
    }

    private static bool Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in value.EnumerateObject())
                if (!names.Add(p.Name) || !Unique(p.Value)) return false;
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray())
                if (!Unique(item)) return false;
        return true;
    }

    private static JsonElement Prop(JsonElement value, string key) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(key, out var item)
            ? item : default;

    private static string? Str(JsonElement value, string key)
    {
        var item = Prop(value, key);
        return item.ValueKind == JsonValueKind.String ? item.GetString() : null;
    }

    private static long Number(JsonElement value, string key)
    {
        var item = Prop(value, key);
        return item.ValueKind == JsonValueKind.Number && item.TryGetInt64(out var n) ? n : 0;
    }

    private static bool TryCheckId(string? url, out long id)
    {
        id = 0;
        if (url is null || !url.StartsWith(CheckPrefix, StringComparison.Ordinal)) return false;
        var suffix = url[CheckPrefix.Length..];
        return long.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out id) &&
            id > 0 && suffix == id.ToString(CultureInfo.InvariantCulture);
    }
}
