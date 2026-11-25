using Microsoft.Extensions.DependencyInjection;
using Solqaryn.Infrastructure.AutomationV5;

if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true" ||
    Environment.GetEnvironmentVariable("RUNNER_ENVIRONMENT") != "github-hosted")
    throw new InvalidOperationException("GITHUB_HOSTED_REQUIRED");

var services = new ServiceCollection();
services.AddHttpClient("SolqarynPublicGitHubQa", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Solqaryn-Global-QA/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

using var provider = services.BuildServiceProvider();
var reader = new V5UniversalCaseFileQaGitHubJobReader(provider.GetRequiredService<IHttpClientFactory>());
const string microtask = "C01-04.M03.06";
const string sha = "ee5fd6e3b6f7f12febc66d926c36ce503571a693";
const long job = 139373797603;
const long suite = 123548903681;
const string details = "https://github.com/solqaryn/Solqaryn/actions/runs/45661942628/job/139373797603";

var actual = await reader.ReadDirectedFromCheckAsync(microtask, sha, job, suite, details, "success");
if (actual is null || actual.MicrotaskId != microtask || actual.SourceSha != sha ||
    actual.WorkflowId != 426472921 || actual.RunId != 45661942628 || actual.Attempt != 1 ||
    actual.JobId != job || actual.CheckRunId != job || actual.CheckSuiteId != suite ||
    actual.CheckName != "C01-04.M03.06 - directed durable MySQL 8.4 QA proof" ||
    actual.Status != "completed" || actual.Conclusion != "success")
    throw new InvalidOperationException("LIVE_GITHUB_IDENTITY_NOT_VERIFIED");

var wrongCheck = await reader.ReadDirectedFromCheckAsync(microtask, sha, job + 1, suite, details, "success");
if (wrongCheck is not null)
    throw new InvalidOperationException("LIVE_GITHUB_MISMATCH_ACCEPTED");

Console.WriteLine($"LIVE_GITHUB_READER_PASS workflow={actual.WorkflowId} run={actual.RunId} attempt={actual.Attempt} job={actual.JobId} check={actual.CheckRunId} suite={actual.CheckSuiteId} source_sha={actual.SourceSha} wrong_check=denied");
