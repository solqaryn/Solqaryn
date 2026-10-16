using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// Microfase 1: Vercel firma Host+StoreKey, Render verifica, consulta allowlist
/// y revisa estado activo en MySQL. No lee datos reales ni afecta Aiven.
/// </summary>
public sealed class PublicStoreProxyBindingSecurityTests
{
    private const string Secret = "TEST_ONLY_STORE_PROOF_SECRET_32_BYTES_MINIMUM_2026";

    private static string Firmar(long at, string method, string ruta, string host, string key)
    {
        var canonical = $"v1\n{at}\n{method}\n{ruta}\n{host}\n{key}";
        return Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    [Fact]
    public async Task MismoHostTiendasAB_UsaPruebaFirmadaYRechazaFalsificaciones()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("signed-store-ab-" + Guid.NewGuid().ToString("N")).Options);
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        const string publicHost = "solqaryn-dev.vercel.app";
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> {
                ["PublicStore:EnableMultiEmpresaBinding"] = "true",
                ["PublicStore:RequireVerifiedProxyProof"] = "true",
                ["PublicStore:ProxySigningKey"] = Secret,
                ["PublicStore:HostBindings:0:Host"] = publicHost,
                ["PublicStore:HostBindings:0:StoreKey"] = "comercio-a",
                ["PublicStore:HostBindings:0:EmpresaId"] = a.Id.ToString(),
                ["PublicStore:HostBindings:1:Host"] = publicHost,
                ["PublicStore:HostBindings:1:StoreKey"] = "comercio-b",
                ["PublicStore:HostBindings:1:EmpresaId"] = b.Id.ToString(),
                ["PublicStore:HostBindings:2:Host"] = publicHost,
                ["PublicStore:HostBindings:2:EmpresaId"] = a.Id.ToString()
            }).Build();
        var resolver = new ConfiguredPublicStoreHostTenantResolver(config);
        var provider = new ServiceCollection().AddSingleton<IConfiguration>(config)
            .BuildServiceProvider();
        var selected = new List<int>();
        var middleware = new UnscopedCommercialTenantGuardMiddleware(http => {
            var tenant = VerifiedPublicStoreTenant.Resolve(http);
            Assert.NotNull(tenant);
            selected.Add(tenant!.Value);
            http.Response.StatusCode = 204;
            return Task.CompletedTask;
        });

        async Task<int> RequestAsync(string routeKey, string? signedKey = null,
            string signedHost = publicHost, bool includeProof = true,
            bool spoofedHeaders = false, long skew = 0, string method = "GET",
            string? signedMethod = null, string path = "/tienda/binding")
        {
            var http = new DefaultHttpContext { RequestServices = provider };
            http.Request.Method = method;
            http.Request.Path = path;
            http.Request.Host = new HostString("solqaryn-api-dev-fxx8.onrender.com");
            http.Response.Body = new MemoryStream();
            if (includeProof)
            {
                var at = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + skew;
                var key = signedKey ?? routeKey;
                http.Request.Headers[PublicStoreProxyProof.OriginHeader] = signedHost;
                http.Request.Headers[PublicStoreProxyProof.KeyHeader] = routeKey;
                http.Request.Headers[PublicStoreProxyProof.AtHeader] = at.ToString();
                http.Request.Headers[PublicStoreProxyProof.ProofHeader] =
                    Firmar(at, signedMethod ?? method, path, signedHost, key);
            }
            if (spoofedHeaders)
            {
                http.Request.Headers["X-Forwarded-Host"] = "evil.example.test";
                http.Request.Headers["X-Empresa-Id"] = b.Id.ToString();
                http.Request.Headers["Origin"] = "https://evil.example.test";
            }

            await middleware.InvokeAsync(http, db, resolver);
            return http.Response.StatusCode;
        }

        Assert.Equal(204, await RequestAsync("comercio-a", spoofedHeaders: true));
        Assert.Equal(204, await RequestAsync("comercio-b"));
        Assert.Equal(204, await RequestAsync("-")); // /tienda legacy explícita
        Assert.Equal(new[] { a.Id, b.Id, a.Id }, selected.ToArray());

        // Ningún header de cliente sustituye firma ni allowlist.
        Assert.Equal(503, await RequestAsync("comercio-a", includeProof: false, spoofedHeaders: true));
        Assert.Equal(503, await RequestAsync("desconocida"));
        Assert.Equal(503, await RequestAsync("comercio-a", signedKey: "comercio-b"));
        Assert.Equal(503, await RequestAsync("comercio-a", signedHost: "evil.example.test"));
        Assert.Equal(503, await RequestAsync("comercio-a", skew: -120));
        Assert.Equal(503, await RequestAsync("comercio-a", skew: 120));
        Assert.Equal(503, await RequestAsync("comercio-a", method: "POST", signedMethod: "GET"));
        // Incluso con firma correcta, esta microfase no autoriza catálogo,
        // checkout, cuenta ni escrituras de tiendas secundarias.
        Assert.Equal(503, await RequestAsync("comercio-a", path: "/tienda/categorias"));
        Assert.Equal(503, await RequestAsync("comercio-a", path: "/tienda/carrito"));
        Assert.Equal(503, await RequestAsync("comercio-a", path: "/tienda/checkout"));
        Assert.Equal(503, await RequestAsync("comercio-b", path: "/tienda/cuenta"));
        Assert.Equal(503, await RequestAsync("comercio-a", method: "POST", path: "/tienda/binding"));
        Assert.Equal(3, selected.Count);

        b.Desactivar();
        await db.SaveChangesAsync();
        Assert.Equal(503, await RequestAsync("comercio-b"));
        Assert.Equal(204, await RequestAsync("comercio-a"));
        Assert.Equal(4, selected.Count);
    }

    [Fact]
    public async Task LegacyMonoEmpresaSinFirmaSeConservaHastaActivacion()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("signed-store-legacy-" + Guid.NewGuid().ToString("N")).Options);
        var company = new Empresa("Empresa única existente");
        db.Set<Empresa>().Add(company);
        await db.SaveChangesAsync();
        var resolver = new ConfiguredPublicStoreHostTenantResolver(
            new ConfigurationBuilder().Build());
        var middleware = new UnscopedCommercialTenantGuardMiddleware(http => {
            Assert.Equal(company.Id, VerifiedPublicStoreTenant.Resolve(http));
            http.Response.StatusCode = 204;
            return Task.CompletedTask;
        });
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Request.Path = "/tienda/bootstrap";
        ctx.Request.Host = new HostString("solqaryn-api-dev-fxx8.onrender.com");
        ctx.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(ctx, db, resolver);
        Assert.Equal(204, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task LegacyRootPorEmpresaExplicitaSigueDisponibleDuranteElRollout()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("signed-store-compat-" + Guid.NewGuid().ToString("N")).Options);
        var a = new Empresa("Empresa histórica A");
        var b = new Empresa("Empresa nueva B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> {
                ["PublicStore:EnableMultiEmpresaBinding"] = "true",
                ["PublicStore:RequireVerifiedProxyProof"] = "true",
                ["PublicStore:ProxySigningKey"] = Secret,
                ["PublicStore:LegacyUpstreamHost"] = "solqaryn-api-dev-fxx8.onrender.com",
                ["PublicStore:LegacyRootEmpresaId"] = a.Id.ToString(),
                ["PublicStore:HostBindings:0:Host"] = "solqaryn-dev.vercel.app",
                ["PublicStore:HostBindings:0:StoreKey"] = "comercio-a",
                ["PublicStore:HostBindings:0:EmpresaId"] = a.Id.ToString(),
                ["PublicStore:HostBindings:1:Host"] = "solqaryn-dev.vercel.app",
                ["PublicStore:HostBindings:1:StoreKey"] = "comercio-b",
                ["PublicStore:HostBindings:1:EmpresaId"] = b.Id.ToString()
            }).Build();
        var services = new ServiceCollection().AddSingleton<IConfiguration>(cfg).BuildServiceProvider();
        var middleware = new UnscopedCommercialTenantGuardMiddleware(ctx => {
            Assert.Equal(a.Id, VerifiedPublicStoreTenant.Resolve(ctx));
            ctx.Response.StatusCode = 204;
            return Task.CompletedTask;
        });
        async Task<int> GetAsync(string host)
        {
            var ctx = new DefaultHttpContext { RequestServices = services };
            ctx.Request.Host = new HostString(host);
            ctx.Request.Path = "/tienda/bootstrap";
            ctx.Request.Method = "GET";
            ctx.Request.Headers["X-Forwarded-Host"] = "solqaryn-dev.vercel.app";
            ctx.Request.Headers["X-Empresa-Id"] = b.Id.ToString();
            ctx.Response.Body = new MemoryStream();
            await middleware.InvokeAsync(ctx, db, new ConfiguredPublicStoreHostTenantResolver(cfg));
            return ctx.Response.StatusCode;
        }
        Assert.Equal(204, await GetAsync("solqaryn-api-dev-fxx8.onrender.com"));
        Assert.Equal(503, await GetAsync("evil.example.test"));
        a.Desactivar();
        await db.SaveChangesAsync();
        Assert.Equal(503, await GetAsync("solqaryn-api-dev-fxx8.onrender.com"));
    }

    [Fact]
    public void ProofNoPermiteCabecerasDuplicadasNiAusentes()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["PublicStore:ProxySigningKey"] = Secret }).Build();
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Request.Path = "/tienda/binding";
        var at = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        ctx.Request.Headers[PublicStoreProxyProof.OriginHeader] = "solqaryn-dev.vercel.app";
        ctx.Request.Headers[PublicStoreProxyProof.KeyHeader] = "comercio-a";
        ctx.Request.Headers[PublicStoreProxyProof.AtHeader] = at.ToString();
        ctx.Request.Headers[PublicStoreProxyProof.ProofHeader] =
            Firmar(at, "GET", "/tienda/binding", "solqaryn-dev.vercel.app", "comercio-a");
        Assert.True(PublicStoreProxyProof.TryReadVerified(ctx.Request, config, out _, out var key));
        Assert.Equal("comercio-a", key);
        ctx.Request.Headers[PublicStoreProxyProof.KeyHeader] =
            new Microsoft.Extensions.Primitives.StringValues(new[] { "comercio-a", "comercio-b" });
        Assert.False(PublicStoreProxyProof.TryReadVerified(ctx.Request, config, out _, out _));
        ctx.Request.Headers.Remove(PublicStoreProxyProof.KeyHeader);
        Assert.False(PublicStoreProxyProof.TryReadVerified(ctx.Request, config, out _, out _));
    }
}
