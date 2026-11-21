using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Solqaryn.API.Middleware;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Common;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0401PlanDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0401PlanApplicationService;

namespace Solqaryn.Tests.API;

/// <summary>
/// C04-01.M05.02: true ASP.NET Core TestServer HTTP + production plan
/// application contract and exception middleware, with test-only adapters.
/// This is NOT a live billing API or JWT/MySQL authorization certification.
/// </summary>
public sealed class C0401M0502ApiDtoSafeErrorsTests
{
    private const string Path = "/c0401-m0502-test";
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly DateTime Now =
        new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    private sealed class ActorSource : IC0401VerifiedPlanActorSource
    {
        public ValueTask<Service.AuthenticatedActor?> ReadCurrentAsync(
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return ValueTask.FromResult<Service.AuthenticatedActor?>(
                new Service.AuthenticatedActor(
                    new Rules.VerifiedActor(
                        100, 7, 5, 11, true, true, true, true, false),
                    true, true, true, true));
        }
    }

    private sealed class PlanSource(bool broken = false) : IC0401VerifiedPlanSnapshotSource
    {
        public ValueTask<Service.ServerPlanContext?> ReadForTenantAsync(
            int tenantId, int planId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (broken)
                throw new InvalidOperationException(
                    "private MySQL password=NEVER_EXPOSE_THIS");
            var snapshot = new Service.ServerPlanContext(
                new Rules.PlanSnapshot(42, "PLAN-BASICO", 3, true,
                    Now.AddDays(-10), Now.AddDays(10)),
                new Rules.SuscripcionSnapshot(100, 42, 8, true,
                    Now.AddDays(-2), Now.AddDays(2)),
                new Rules.CapacidadSnapshot("ventas.crear", 10, false, 2,
                    true, true, true, true),
                tenantId == 100, planId == 42, true);
            return ValueTask.FromResult<Service.ServerPlanContext?>(snapshot);
        }
    }

    private sealed record PreviewDto(
        string Code, bool RequiresBackendGate, bool AuthorizesWrite,
        bool ReservesQuota, bool AuthorizesPayment);

    private static async Task<IHost> HostAsync(bool failProvider = false)
    {
        var appService = new Service(new ActorSource(), new PlanSource(failProvider),
            new Clock());
        var host = new HostBuilder()
            .ConfigureWebHost(web => web.UseTestServer()
                .ConfigureServices(s => s.AddLogging())
                .Configure(app =>
                {
                    app.UseMiddleware<ExceptionHandlingMiddleware>();
                    app.Run(async ctx =>
                    {
                        if (ctx.Request.Path != Path)
                        {
                            ctx.Response.StatusCode = 404;
                            return;
                        }
                        if (ctx.Request.Query.ContainsKey("crash"))
                            throw new InvalidOperationException(
                                "private connection string Authorization=secret");

                        var tenant = int.TryParse(
                            ctx.Request.Query["tenant"].ToString(), out var t) ? t : 100;
                        var plan = int.TryParse(
                            ctx.Request.Query["plan"].ToString(), out var p) ? p : 42;
                        var preview = await appService.PrepareAsync(
                            new Service.ClientIntent(plan,
                                new Rules.SolicitudUso(tenant, 11, 8, 3,
                                    "ventas.crear", 3, false)), ctx.RequestAborted);
                        ctx.Response.ContentType = "application/json";
                        if (preview.Code == "CLIENT_INTENT_INVALID_OR_STALE")
                        {
                            ctx.Response.StatusCode = 400;
                            await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                                ApiResponse<PreviewDto>.Fail("invalid_request"), Web));
                            return;
                        }
                        if (preview.Code == "CLIENT_TENANT_HINT_CONFLICT")
                        {
                            ctx.Response.StatusCode = 403;
                            await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                                ApiResponse<PreviewDto>.Fail("forbidden"), Web));
                            return;
                        }
                        if (preview.Code == "SERVER_PLAN_SOURCE_UNAVAILABLE")
                        {
                            ctx.Response.StatusCode = 503;
                            await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                                ApiResponse<PreviewDto>.Fail("service_unavailable"), Web));
                            return;
                        }
                        if (!preview.RequiresLiveAtomicBackendGate)
                        {
                            ctx.Response.StatusCode = 403;
                            await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                                ApiResponse<PreviewDto>.Fail("not_authorized"), Web));
                            return;
                        }
                        await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                            ApiResponse<PreviewDto>.Ok(new PreviewDto(
                                preview.Code, preview.RequiresLiveAtomicBackendGate,
                                preview.AuthorizesReadOrWrite, preview.ReservesQuota,
                                preview.AuthorizesPayment)), Web));
                    });
                }))
            .Build();
        await host.StartAsync();
        return host;
    }

    [Fact]
    public async Task RealHttpDtoPositiveRemainsReadOnlyAndNeverGrantsBilling()
    {
        using var host = await HostAsync();
        using var client = host.GetTestServer().CreateClient();
        using var response = await client.GetAsync(Path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("REAL_JWT_RBAC_MYSQL_TRANSACTION_STILL_REQUIRED",
            root.GetProperty("data").GetProperty("code").GetString());
        Assert.True(root.GetProperty("data").GetProperty("requiresBackendGate").GetBoolean());
        Assert.False(root.GetProperty("data").GetProperty("authorizesWrite").GetBoolean());
        Assert.False(root.GetProperty("data").GetProperty("reservesQuota").GetBoolean());
        Assert.False(root.GetProperty("data").GetProperty("authorizesPayment").GetBoolean());
        Assert.Empty(root.GetProperty("errors").EnumerateArray());
    }

    [Theory]
    [InlineData("?plan=0", HttpStatusCode.BadRequest)]
    [InlineData("?tenant=0", HttpStatusCode.BadRequest)]
    [InlineData("?tenant=200", HttpStatusCode.Forbidden)]
    public async Task RealHttpDtoRejectsBadPlanOrCrossTenantWithoutDisclosure(
        string query, HttpStatusCode expected)
    {
        using var host = await HostAsync();
        using var client = host.GetTestServer().CreateClient();
        using var response = await client.GetAsync(Path + query);
        Assert.Equal(expected, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("data").ValueKind);
        Assert.DoesNotContain("PLAN-BASICO", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BrokenPlanProviderIsFailClosedWithSafe503()
    {
        using var host = await HostAsync(failProvider: true);
        using var client = host.GetTestServer().CreateClient();
        using var response = await client.GetAsync(Path);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("NEVER_EXPOSE_THIS", body, StringComparison.Ordinal);
        Assert.DoesNotContain("MySQL", body, StringComparison.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task UnhandledExceptionUsesActualSafeProductionMiddleware()
    {
        using var host = await HostAsync();
        using var client = host.GetTestServer().CreateClient();
        using var response = await client.GetAsync(Path + "?crash=1");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("Authorization=secret", body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
    }
}
