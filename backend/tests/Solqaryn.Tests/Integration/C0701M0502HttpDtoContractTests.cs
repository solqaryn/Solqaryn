using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0701CatalogDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0701CatalogApplicationService;

namespace Solqaryn.Tests.Integration;

// Test-assembly-only route. It never creates a production endpoint.
[ApiController]
[Route("__v22/c0701-m0502/contract")]
public sealed class C0701M0502ContractTestController(Service service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Prepare([FromBody] Service.ClientIntent intent, CancellationToken ct)
    {
        var value = await service.PrepareAsync(intent, ct);
        if (value.Code == "LIVE_ATOMIC_AUTHORIZATION_AND_VERSION_RECHECK_REQUIRED")
            return Ok(new C0701M0502Wire(value.Code, true, false, false));
        if (value.Code == "CATALOG_INTENT_INVALID_OR_STALE")
            return BadRequest(new C0701M0502Wire(value.Code, false, false, false));
        return StatusCode(StatusCodes.Status403Forbidden,
            new C0701M0502Wire("CATALOG_PRECONDITION_DENIED", false, false, false));
    }
}

public sealed record C0701M0502Wire(
    string Code, bool RequiresLiveAuthorizedTransaction, bool AuthorizesWrite, bool PersistsData);

/// <summary>Real TestServer HTTP/DTO contract, with explicit deterministic test doubles.
/// NOT a signed JWT, real role grant or MySQL runtime certification.</summary>
public sealed class C0701M0502HttpDtoContractTests
{
    private sealed class Actor : IC0701VerifiedCatalogActorReader
    {
        public ValueTask<Service.VerifiedActor?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult<Service.VerifiedActor?>(new(
                new Rules.VerifiedServerScope(100, 17, 9, 11, true, true, true, true, false),
                true, true, true, true));
        }
    }
    private sealed class Product : IC0701VerifiedCatalogResourceReader
    {
        public ValueTask<Service.VerifiedResource?> ReadOwnedAsync(int tenant, int product, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult<Service.VerifiedResource?>(
                tenant == 100 && product == 42
                    ? new(new Rules.ProductSnapshot(100, 42, 10, 11, 100, 100, true, false), true, true)
                    : null);
        }
    }
    private static Service.ClientIntent Intent() => new(100, 42, 10,
        new Rules.Mutation(42, 10, "PRODUCTO", "SKU-42", "Producto A",
            120m, 50m, 1500, true, true, true, true));

    private static async Task<(IHost, HttpClient)> StartAsync()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(web => web.UseTestServer()
                .ConfigureServices(s =>
                {
                    s.AddControllers().AddApplicationPart(typeof(C0701M0502ContractTestController).Assembly);
                    s.AddScoped<IC0701VerifiedCatalogActorReader, Actor>();
                    s.AddScoped<IC0701VerifiedCatalogResourceReader, Product>();
                    s.AddScoped<Service>();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(routes => routes.MapControllers());
                }))
            .StartAsync();
        return (host, host.GetTestClient());
    }

    [Fact]
    public async Task ValidDtoOverHttpIsPreparatoryAndCannotAuthorizeWrite()
    {
        var (host, client) = await StartAsync();
        using (host)
        using (client)
        using (var response = await client.PostAsJsonAsync("/__v22/c0701-m0502/contract", Intent()))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var body = payload.RootElement;
            Assert.Equal("LIVE_ATOMIC_AUTHORIZATION_AND_VERSION_RECHECK_REQUIRED",
                body.GetProperty("code").GetString());
            Assert.True(body.GetProperty("requiresLiveAuthorizedTransaction").GetBoolean());
            Assert.False(body.GetProperty("authorizesWrite").GetBoolean());
            Assert.False(body.GetProperty("persistsData").GetBoolean());
        }
    }

    [Fact]
    public async Task InvalidDtoIdsAndVersionYieldSafe400()
    {
        var (host, client) = await StartAsync();
        using (host)
        using (client)
        {
            foreach (var input in new[]
            {
                Intent() with { ProductoId = 0 },
                Intent() with { VersionEsperada = 0 },
                Intent() with { Propuesta = Intent().Propuesta! with { ProductoId = 43 } },
                Intent() with { Propuesta = Intent().Propuesta! with { ExpectedVersion = 9 } }
            })
            {
                using var response = await client.PostAsJsonAsync("/__v22/c0701-m0502/contract", input);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var json = await response.Content.ReadAsStringAsync();
                Assert.Contains("CATALOG_INTENT_INVALID_OR_STALE", json, StringComparison.Ordinal);
                Assert.DoesNotContain("MySql", json, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task ForeignTenantAndClientAuthEscalationYieldSanitized403()
    {
        var (host, client) = await StartAsync();
        using (host)
        using (client)
        {
            foreach (var input in new[]
            {
                Intent() with { EmpresaHint = 200 },
                Intent() with { IncluyePermisoORolDelCliente = true },
                Intent() with { SolicitaEscrituraDirecta = true }
            })
            {
                using var response = await client.PostAsJsonAsync("/__v22/c0701-m0502/contract", input);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                var json = await response.Content.ReadAsStringAsync();
                Assert.Contains("CATALOG_PRECONDITION_DENIED", json, StringComparison.Ordinal);
                Assert.DoesNotContain("CLIENT_TENANT_MISMATCH", json, StringComparison.Ordinal);
                Assert.DoesNotContain("MySql", json, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task MalformedJson400AndUnsupportedMethod405NeverGrant()
    {
        var (host, client) = await StartAsync();
        using (host)
        using (client)
        {
            using var invalid = await client.PostAsync("/__v22/c0701-m0502/contract",
                new StringContent("{ invalid-json", System.Text.Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            using var get = await client.GetAsync("/__v22/c0701-m0502/contract");
            Assert.Equal(HttpStatusCode.MethodNotAllowed, get.StatusCode);
        }
    }
}
