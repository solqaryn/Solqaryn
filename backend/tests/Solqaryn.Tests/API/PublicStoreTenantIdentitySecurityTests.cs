using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class PublicStoreTenantIdentitySecurityTests
{
    private const string Secret = "TEST_ONLY_TENANT_PUBLIC_IDENTITY_HMAC_32_BYTES";

    [Fact]
    public async Task SoloProyectaLaEmpresaVerificada_YRechazaAusenciaYSuspension()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("public-identity-" + Guid.NewGuid().ToString("N")).Options);
        var a = new Empresa("Identidad Comercial A");
        var b = new Empresa("Identidad Comercial B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();
        db.Set<ConfigEmpresa>().AddRange(
            new ConfigEmpresa(a.Id) { Moneda = "HNL" },
            new ConfigEmpresa(b.Id) { Moneda = "USD" });
        await db.SaveChangesAsync();

        async Task<IActionResult> ConsultarAsync(int? empresaId)
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = "/tienda/configuracion";
            if (empresaId.HasValue)
                ctx.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = empresaId.Value;
            var controller = new TiendaConfiguracionPublicaController(db) {
                ControllerContext = new ControllerContext { HttpContext = ctx }
            };
            return await controller.Consultar(CancellationToken.None);
        }

        Assert.Equal(503, Assert.IsType<ObjectResult>(
            await ConsultarAsync(null)).StatusCode);
        var aResult = Assert.IsType<OkObjectResult>(await ConsultarAsync(a.Id));
        var aJson = JsonSerializer.Serialize(aResult.Value);
        Assert.Contains("Identidad Comercial A", aJson);
        Assert.Contains("HNL", aJson);
        Assert.DoesNotContain("Identidad Comercial B", aJson);
        Assert.DoesNotContain("USD", aJson);
        var bResult = Assert.IsType<OkObjectResult>(await ConsultarAsync(b.Id));
        var bJson = JsonSerializer.Serialize(bResult.Value);
        Assert.Contains("Identidad Comercial B", bJson);
        Assert.Contains("USD", bJson);
        Assert.DoesNotContain("Identidad Comercial A", bJson);

        b.Desactivar();
        await db.SaveChangesAsync();
        Assert.Equal(503, Assert.IsType<ObjectResult>(
            await ConsultarAsync(b.Id)).StatusCode);
        Assert.IsType<OkObjectResult>(await ConsultarAsync(a.Id));
    }

    [Fact]
    public async Task FirmaHostClaveReplayYSuspension_FailClosed_SinAbrirComercio()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("public-identity-guard-" + Guid.NewGuid().ToString("N")).Options);
        var a = new Empresa("A");
        var b = new Empresa("B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();
        const string host = "solqaryn-dev.vercel.app";
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["PublicStore:EnableMultiEmpresaBinding"] = "true",
            ["PublicStore:RequireVerifiedProxyProof"] = "true",
            ["PublicStore:ProxySigningKey"] = Secret,
            ["PublicStore:HostBindings:0:Host"] = host,
            ["PublicStore:HostBindings:0:StoreKey"] = "comercio-a",
            ["PublicStore:HostBindings:0:EmpresaId"] = a.Id.ToString(),
            ["PublicStore:HostBindings:1:Host"] = host,
            ["PublicStore:HostBindings:1:StoreKey"] = "comercio-b",
            ["PublicStore:HostBindings:1:EmpresaId"] = b.Id.ToString()
        }).Build();
        var provider = new ServiceCollection().AddSingleton<IConfiguration>(config).BuildServiceProvider();
        var replay = new TestPublicStoreProofReplayStore();
        var selected = new List<int>();
        var middleware = new UnscopedCommercialTenantGuardMiddleware(ctx => {
            selected.Add(VerifiedPublicStoreTenant.Resolve(ctx) ?? 0);
            ctx.Response.StatusCode = 204;
            return Task.CompletedTask;
        });
        async Task<int> Call(string key, string path = "/tienda/configuracion",
            bool signed = true, string signedHost = host, string? nonce = null,
            string method = "GET", string? signingKey = null)
        {
            var ctx = new DefaultHttpContext { RequestServices = provider };
            ctx.Request.Path = path;
            ctx.Request.Method = method;
            ctx.Request.Host = new HostString("solqaryn-api-dev-fxx8.onrender.com");
            ctx.Request.Headers["X-Forwarded-Host"] = host;
            ctx.Request.Headers["X-Empresa-Id"] = b.Id.ToString();
            ctx.Response.Body = new MemoryStream();
            if (signed)
            {
                var at = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var token = nonce ?? Guid.NewGuid().ToString("N");
                var message = $"v2\n{at}\n{method}\n{path}\n{signedHost}\n{signingKey ?? key}\n{token}";
                var proof = Convert.ToHexString(HMACSHA256.HashData(
                    Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(message)))
                    .ToLowerInvariant();
                ctx.Request.Headers[PublicStoreProxyProof.OriginHeader] = signedHost;
                ctx.Request.Headers[PublicStoreProxyProof.KeyHeader] = key;
                ctx.Request.Headers[PublicStoreProxyProof.AtHeader] = at.ToString();
                ctx.Request.Headers[PublicStoreProxyProof.NonceHeader] = token;
                ctx.Request.Headers[PublicStoreProxyProof.ProofHeader] = proof;
            }
            await middleware.InvokeAsync(ctx, db,
                new ConfiguredPublicStoreHostTenantResolver(config), replay);
            return ctx.Response.StatusCode;
        }

        Assert.Equal(204, await Call("comercio-a"));
        Assert.Equal(204, await Call("comercio-b"));
        Assert.Equal(new[] { a.Id, b.Id }, selected);
        Assert.Equal(503, await Call("comercio-a", signed: false));
        Assert.Equal(503, await Call("no-existe"));
        Assert.Equal(503, await Call("comercio-a", signedHost: "evil.example.test"));
        Assert.Equal(503, await Call("comercio-a", signingKey: "comercio-b"));
        Assert.Equal(503, await Call("comercio-a", method: "POST"));
        Assert.Equal(503, await Call("comercio-a", path: "/tienda/productos"));
        Assert.Equal(503, await Call("comercio-a", path: "/empresa-configuracion/publica"));
        Assert.Equal(new[] { a.Id, b.Id }, selected);

        // Una prueba firmada no puede usarse dos veces; el ledger es compartido.
        var replayNonce = Guid.NewGuid().ToString("N");
        Assert.Equal(204, await Call("comercio-a", nonce: replayNonce));
        Assert.Equal(503, await Call("comercio-a", nonce: replayNonce));
        b.Desactivar();
        await db.SaveChangesAsync();
        Assert.Equal(503, await Call("comercio-b"));
        Assert.Equal(204, await Call("comercio-a"));
    }
}
