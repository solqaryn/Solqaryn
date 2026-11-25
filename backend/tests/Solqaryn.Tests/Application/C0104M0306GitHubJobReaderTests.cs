using System.Net;
using System.Text;
using System.Text.Json;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0104M0306GitHubJobReaderTests
{
    private const string Sha = "0d59a836bff9f4ae491d1ad714b48b9822a9fb4b";
    private const string Id = "C01-04.M03.06";
    private const string Name = "C01-04.M03.06 - directed durable MySQL 8.4 QA proof";
    private const long Run = 45577552132L;
    private const long Job = 139079124950L;
    // Deliberately distinct job and check IDs: the linked API URL, not equality, is the proof.
    private const long Check = 139079124951L;
    private const long Suite = 123321053186L;
    private const string DetailsPrefix = "https://github.com/solqaryn/Solqaryn/actions/runs/45577552132/job/";

    private sealed class Handler(string fault, int attempt = 1) : HttpMessageHandler
    {
        public List<string> Paths { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Assert.Equal("api.github.com", request.RequestUri!.Host);
            Paths.Add(request.RequestUri.PathAndQuery);
            var n = Paths.Count;
            var jobId = Job + (attempt - 1) * 10L;
            var checkId = Check + (attempt - 1) * 10L;
            var details = DetailsPrefix + jobId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            object body;
            if (n == 1)
            {
                Assert.Equal($"/repos/solqaryn/Solqaryn/actions/runs/{Run}/attempts/{attempt}", Paths[^1]);
                body = new
                {
                    id = Run, run_attempt = fault == "attempt" ? attempt + 1 : attempt,
                    head_sha = Sha, head_branch = "dev", @event = "push",
                    path = ".github/workflows/v23-c01-04-m03-06-durable-qa-mysql84.yml",
                    repository = new { full_name = "solqaryn/Solqaryn" },
                    head_repository = new { full_name = fault == "fork" ? "other/Solqaryn" : "solqaryn/Solqaryn" },
                    workflow_id = 426472921L, check_suite_id = Suite,
                    status = fault == "pending" ? "in_progress" : "completed",
                    conclusion = fault == "failure" ? "failure" : "success"
                };
            }
            else if (n == 2)
            {
                Assert.Contains($"/attempts/{attempt}/jobs?", Paths[^1]);
                body = new
                {
                    total_count = fault == "partial" ? 2 : 1,
                    jobs = new[]
                    {
                        new { id = jobId, run_id = Run, run_attempt = attempt, head_sha = Sha, head_branch = "dev",
                            name = Name, status = "completed", conclusion = fault == "failure" ? "failure" : "success",
                            html_url = details,
                            check_run_url = fault == "foreign_url" ? "https://example.org/check-runs/1" :
                                $"https://api.github.com/repos/solqaryn/Solqaryn/check-runs/{checkId}" }
                    }
                };
            }
            else
            {
                Assert.Contains("/commits/" + Sha + "/check-runs?", Paths[^1]);
                var check = new
                {
                    id = checkId, name = Name, head_sha = fault == "wrong_sha" ? new string('a', 40) : Sha,
                    status = "completed", conclusion = fault == "failure" ? "failure" : "success",
                    details_url = details, check_suite = new { id = fault == "suite" ? Suite + 1 : Suite },
                    app = new { slug = "github-actions" }
                };
                body = new { total_count = fault == "duplicate" ? 2 : 1,
                    check_runs = fault == "duplicate" ? new[] { check, check } : new[] { check } };
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class Factory(Handler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal("SolqarynPublicGitHubQa", name);
            return new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://api.github.com/"),
                Timeout = TimeSpan.FromSeconds(12)
            };
        }
    }

    [Theory]
    [InlineData("", "success")]
    [InlineData("failure", "failure")]
    public async Task ReturnsOnlyCorroboratedBuildIdentityAndActualConclusion(string fault, string conclusion)
    {
        using var handler = new Handler(fault);
        var result = await new V5UniversalCaseFileQaGitHubJobReader(new Factory(handler))
            .ReadDirectedAsync(Id, Sha, Run, 1, Job);
        Assert.NotNull(result);
        Assert.Equal((Run, 1, Job, Check, Suite), (result.RunId, result.Attempt, result.JobId, result.CheckRunId, result.CheckSuiteId));
        Assert.Equal(Sha, result.SourceSha);
        Assert.Equal(conclusion, result.Conclusion);
        Assert.Equal(3, handler.Paths.Count);
    }

    [Theory]
    [InlineData("attempt", 1)]
    [InlineData("fork", 1)]
    [InlineData("pending", 1)]
    [InlineData("partial", 2)]
    [InlineData("foreign_url", 2)]
    [InlineData("wrong_sha", 3)]
    [InlineData("suite", 3)]
    [InlineData("duplicate", 3)]
    public async Task RejectsUnlinkedOrIncompleteProviderEvidence(string fault, int requests)
    {
        using var handler = new Handler(fault);
        var result = await new V5UniversalCaseFileQaGitHubJobReader(new Factory(handler))
            .ReadDirectedAsync(Id, Sha, Run, 1, Job);
        Assert.Null(result);
        Assert.Equal(requests, handler.Paths.Count);
    }

    [Fact]
    public async Task DistinctAttemptsOfSameRunAndShaRetainTheirOwnEvidence()
    {
        using var first = new Handler("failure", 1);
        using var second = new Handler("", 2);
        var old = await new V5UniversalCaseFileQaGitHubJobReader(new Factory(first))
            .ReadDirectedAsync(Id, Sha, Run, 1, Job);
        var latest = await new V5UniversalCaseFileQaGitHubJobReader(new Factory(second))
            .ReadDirectedAsync(Id, Sha, Run, 2, Job + 10);
        Assert.NotNull(old);
        Assert.NotNull(latest);
        Assert.Equal((Run, 1, Job, Check, Sha, "failure"),
            (old.RunId, old.Attempt, old.JobId, old.CheckRunId, old.SourceSha, old.Conclusion));
        Assert.Equal((Run, 2, Job + 10, Check + 10, Sha, "success"),
            (latest.RunId, latest.Attempt, latest.JobId, latest.CheckRunId, latest.SourceSha, latest.Conclusion));
        Assert.Equal(3, first.Paths.Count);
        Assert.Equal(3, second.Paths.Count);
    }

    [Fact]
    public async Task InvalidScopeAndCancellationFailBeforeProviderIo()
    {
        using var handler = new Handler("");
        var reader = new V5UniversalCaseFileQaGitHubJobReader(new Factory(handler));
        Assert.Null(await reader.ReadDirectedAsync("C01-04.M03.05", Sha, Run, 1, Job));
        Assert.Null(await reader.ReadDirectedAsync(Id, Sha + "\n", Run, 1, Job));
        Assert.Null(await reader.ReadDirectedAsync(Id, Sha, Run, 0, Job));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            reader.ReadDirectedAsync(Id, Sha, Run, 1, Job, new CancellationToken(true)));
        Assert.Empty(handler.Paths);
    }
}
