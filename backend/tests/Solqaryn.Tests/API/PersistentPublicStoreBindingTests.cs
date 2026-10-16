using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class PersistentPublicStoreBindingTests
{
    private const string Secret = "TEST_ONLY_PLATFORM_STORE_REGISTRY_SECRET_LONG_2026";

    [Fact]
    public async Task SignedDatabaseStoreLinksRemainIsolatedAndSuspensionRevokesImmediately()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("persistent-store-" + Guid.NewGuid().ToString("N")).Options);
        var a = new Empresa("A persistent");
        var b = new Empresa("B persistent");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        const string host = "solqaryn-dev.vercel.app";
        db.PublicStoreBindings.AddRange(
            new PublicStoreBinding { EmpresaId = a.Id, Host = host, StoreKey = "tienda-a", Activa = true },
            new PublicStoreBinding { EmpresaId = b.Id, Host = host, StoreKey = "tienda-b", Activa = true });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["PublicStore:EnableMultiEmpresaBinding"] = "true",
            ["PublicStore:EnableDatabaseBindings"] = "true",
            ["PublicStore:RequireVerifiedProxyProof"] = "true",
            ["PublicStore:ProxySigningKey"] = Secret
        }).Build();
        var resolver = new ConfiguredPublicStoreHostTenantResolver(config);
        var services = new ServiceCollection().AddSingleton<IConfiguration>(config).BuildServiceProvider();
        var selected = new List<int>();
        var middleware = new UnscopedCommercialTenantGuardMiddleware(ctx => {
            var tenant = VerifiedPublicStoreTenant.Resolve(ctx);
            Assert.True(tenant is > 0);
            selected.Add(tenant.Value);
            ctx.Response.StatusCode = 204;
            return Task.CompletedTask;
        });

        async Task<int> Call(string key, string path = "/tienda/binding", bool signed = true)
        {
            var ctx = new DefaultHttpContext { RequestServices = services };
            ctx.Request.Method = "GET";
            ctx.Request.Path = path;
            ctx.Request.Host = new HostString("solqaryn-api-dev-fxx8.onrender.com");
            ctx.Request.Headers["X-Empresa-Id"] = b.Id.ToString();
            ctx.Request.Headers["X-Forwarded-Host"] = "attacker.example";
            if (signed)
            {
                var at = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var canonical = $"v1\n{at}\nGET\n{path}\n{host}\n{key}";
                ctx.Request.Headers[PublicStoreProxyProof.OriginHeader] = host;
                ctx.Request.Headers[PublicStoreProxyProof.KeyHeader] = key;
                ctx.Request.Headers[PublicStoreProxyProof.AtHeader] = at.ToString();
                ctx.Request.Headers[PublicStoreProxyProof.ProofHeader] =
                    Convert.ToHexString(HMACSHA256.HashData(
                        Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(canonical)))
                        .ToLowerInvariant();
            }
            ctx.Response.Body = new MemoryStream();
            await middleware.InvokeAsync(ctx, db, resolver);
            return ctx.Response.StatusCode;
        }

        Assert.True(resolver.IsEnabled);
        Assert.Equal(204, await Call("tienda-a"));
        Assert.Equal(204, await Call("tienda-b"));
        Assert.Equal(503, await Call("desconocida"));
        Assert.Equal(503, await Call("tienda-a", signed: false));
        Assert.Equal(503, await Call("tienda-a", "/tienda/checkout"));
        b.Desactivar();
        await db.SaveChangesAsync();
        Assert.Equal(503, await Call("tienda-b"));
        Assert.Equal(204, await Call("tienda-a"));
        Assert.Equal(new[] { a.Id, b.Id, a.Id }, selected);
    }

    [Fact]
    public async Task InactiveDraftDoesNotDisableTheExistingLegacyPublicStore()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("draft-legacy-" + Guid.NewGuid().ToString("N")).Options);
        var a = new Empresa("Tienda original");
        var b = new Empresa("Borrador");
        b.Desactivar();
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();
        var resolver = new ConfiguredPublicStoreHostTenantResolver(new ConfigurationBuilder().Build());
        var middleware = new UnscopedCommercialTenantGuardMiddleware(ctx => {
            Assert.Equal(a.Id, VerifiedPublicStoreTenant.Resolve(ctx));
            ctx.Response.StatusCode = 204;
            return Task.CompletedTask;
        });
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Request.Path = "/tienda/bootstrap";
        ctx.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(ctx, db, resolver);
        Assert.Equal(204, ctx.Response.StatusCode);

        a.Desactivar();
        b.Activar();
        await db.SaveChangesAsync();
        var second = new DefaultHttpContext();
        second.Request.Method = "GET";
        second.Request.Path = "/tienda/bootstrap";
        second.Response.Body = new MemoryStream();
        // A revoked owner must not be selected by its old ordering.
        var called = false;
        var routed = new UnscopedCommercialTenantGuardMiddleware(x => {
            called = true;
            Assert.Equal(b.Id, VerifiedPublicStoreTenant.Resolve(x));
            x.Response.StatusCode = 204;
            return Task.CompletedTask;
        });
        await routed.InvokeAsync(second, db, resolver);
        Assert.True(called);
    }
}
