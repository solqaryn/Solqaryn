using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Solqaryn.API.Middleware;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Common;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// V5 C00-03.M05.02. Real TestServer, production projection and error middleware.
/// Test-only route: does not certify live JWT, RBAC, MySQL, Sheets or A5.
/// </summary>
public sealed class V5C0003M0502CoverageHttpContractTests
{
    private const string Route = "/api/v5/coverage-contract-test";
    private const string Actor = "6adc2e4bd8a48191ac7d8641ed95a1a3";
    private const string Tenant = "tenant-a";
    private const string Item = "C00-03.M03.02";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task AllowedCoverageUsesHttp200TypedDtoButNeverCertifiesEvidence()
    {
        using var server = Host(Actor, Tenant, Sha);
        using var client = server.CreateClient();
        using var response = await client.GetAsync(Route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Empty(json.RootElement.GetProperty("errors").EnumerateArray());
        var row = Assert.Single(json.RootElement.GetProperty("data").GetProperty("rows").EnumerateArray());
        Assert.Equal(Item, row.GetProperty("workItemId").GetString());
        Assert.Equal(Sha, row.GetProperty("observedHeadSha").GetString());
        Assert.Contains(row.GetProperty("outstanding").EnumerateArray(),
            x => x.GetString() == "EVIDENCE_NOT_VERIFIED");
    }

    [Theory]
    [InlineData("unlisted-task", Tenant, Sha, "R-A")]
    [InlineData(Actor, "tenant-b", Sha, "R-A")]
    [InlineData(Actor, Tenant, "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", "R-A")]
    [InlineData(Actor, Tenant, Sha, "UNKNOWN-REQ")]
    public async Task DeniedActorTenantShaOrRequirementIsSafeHttp400(
        string actor, string tenant, string sha, string requirement)
    {
        using var server = Host(actor, tenant, sha, requirement);
        using var client = server.CreateClient();
        using var response = await client.GetAsync(Route);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(Route, json.RootElement.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("private-tenant-b-token", body, StringComparison.Ordinal);
        Assert.DoesNotContain("TENANT_MISMATCH", body, StringComparison.Ordinal);
        Assert.DoesNotContain("SOURCE_REF_MOVED", body, StringComparison.Ordinal);
        Assert.DoesNotContain(Item, body, StringComparison.Ordinal);
    }

    private static TestServer Host(string actor, string tenant, string sha, string requirement = "R-A") =>
        new(new WebHostBuilder()
            .ConfigureServices(s => s.AddLogging())
            .Configure(app =>
            {
                app.UseMiddleware<ExceptionHandlingMiddleware>();
                app.Run(async context =>
                {
                    var result = new V5CoverageTenantProjectionService().Project(
                        actor, "scope-a", tenant,
                        new[] { new V5RequirementCoverageRules.Entry(
                            requirement, Item, "V5 FINAL", "contract", "xunit", "unverified") },
                        new Dictionary<string, V5RequirementCoverageProjectionService.WorkItemReadModel>
                        {
                            [Item] = new(Item, "LISTO", Actor, "Matriz integrada", Sha)
                        },
                        new HashSet<string> { "R-A" },
                        new HashSet<string> { Item },
                        sha,
                        new HashSet<string> { Actor },
                        new Dictionary<string, IReadOnlySet<string>>
                        {
                            [Actor] = new HashSet<string> { "scope-a" }
                        },
                        new Dictionary<string, string> { ["scope-a"] = Tenant },
                        new Dictionary<string, string> { [Item] = "scope-a" },
                        new Dictionary<string, string> { ["R-A"] = "scope-a" });
                    if (!result.Verdict.Accepted)
                        throw new ArgumentException("private-tenant-b-token; " + result.Verdict.Code);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(
                        ApiResponse<object>.Ok(new
                        {
                            rows = result.Rows.Select(row => new
                            {
                                row.WorkItemId,
                                row.ObservedHeadSha,
                                row.Outstanding
                            })
                        }, "read-only coverage"), Json));
                });
            }));
}
