using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Common;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// C00-02.M05.02 verifies the real exception middleware and API DTO envelope over
/// TestServer HTTP. The route below is a test-only stub; it is NOT a live V5 API,
/// does not authorize a task/tenant, and does not access MySQL or Google Sheets.
/// </summary>
public sealed class V5C0002M0502SafeHttpErrorContractTests
{
    private const string Path = "/api/v5/claim-contract";
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SuccessfulHttpResponseContainsTypedEnvelopeWithoutClaimsMutation()
    {
        using var server = Host(async context =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(
                ApiResponse<string>.Ok("read-only", "contrato de prueba"), WebJson));
        });
        using var client = server.CreateClient();
        using var response = await client.GetAsync(Path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("read-only", json.RootElement.GetProperty("data").GetString());
        Assert.Equal("contrato de prueba", json.RootElement.GetProperty("message").GetString());
        Assert.Empty(json.RootElement.GetProperty("errors").EnumerateArray());
    }

    [Theory]
    [InlineData("Authorization: Bearer secret-token")]
    [InlineData("empresa-B: privileged admin")]
    [InlineData("sql: SELECT private from users")]
    [InlineData("password=never-log-this")]
    public async Task InvalidArgumentReturnsSafe400WithoutReflectingSensitiveInput(string untrustedDetail)
    {
        using var server = Host(_ => throw new ArgumentException(untrustedDetail));
        using var client = server.CreateClient();
        using var response = await client.GetAsync(Path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(Path, json.RootElement.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain(untrustedDetail, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OptimisticConcurrencyConflictReturnsSafe409WithoutTenantDetail()
    {
        const string secret = "tenant-other-company-id=hidden";
        using var server = Host(_ => throw new DbUpdateConcurrencyException(secret));
        using var client = server.CreateClient();
        using var response = await client.GetAsync(Path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(409, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Conflicto de concurrencia", json.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain(secret, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnexpectedFailureUsesSafe500EnvelopeWithoutImplementationDetail()
    {
        const string privateDetail = "authorizationKey=private";
        using var server = Host(_ => throw new InvalidOperationException(privateDetail));
        using var client = server.CreateClient();
        using var response = await client.GetAsync(Path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(body);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("data").ValueKind);
        Assert.Empty(json.RootElement.GetProperty("errors").EnumerateArray());
        Assert.DoesNotContain(privateDetail, body, StringComparison.Ordinal);
    }

    private static TestServer Host(RequestDelegate next) =>
        new(new WebHostBuilder()
            .ConfigureServices(services => services.AddLogging())
            .Configure(app =>
            {
                app.UseMiddleware<ExceptionHandlingMiddleware>();
                app.Run(next);
            }));
}
