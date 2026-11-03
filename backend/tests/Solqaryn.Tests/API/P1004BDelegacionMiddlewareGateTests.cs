using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.API.Middleware;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// P1-004.B: comprueba que la cuarentena multiempresa no suplanta el
/// control RBAC de delegación. Atravesar el middleware NO implica permiso:
/// JWT y grants se verifican en MVC y MySQL. No sustituye la prueba LIVE.
/// </summary>
public sealed class P1004BDelegacionMiddlewareGateTests
{
    [Theory]
    [InlineData("GET", "/plataforma/delegaciones/empresas")]
    [InlineData("POST", "/plataforma/delegaciones")]
    [InlineData("DELETE", "/plataforma/delegaciones/17")]
    [InlineData("DELETE", "/PLATAFORMA/DELEGACIONES/17")]
    public async Task RutaDelegacionExacta_ConJwt_PasaAlControlador(string metodo, string ruta)
    {
        var result = await EjecutarAsync(metodo, ruta, autenticado: true);
        Assert.True(result.Forwarded);
        Assert.Equal(StatusCodes.Status204NoContent, result.Status);
    }

    [Theory]
    [InlineData("GET", "/plataforma/delegaciones/empresas")]
    [InlineData("POST", "/plataforma/delegaciones")]
    [InlineData("DELETE", "/plataforma/delegaciones/17")]
    public async Task RutaDelegacionExacta_SinJwt_NoAtraviesaLaCuarentena(string metodo, string ruta)
    {
        var result = await EjecutarAsync(metodo, ruta, autenticado: false);
        Assert.False(result.Forwarded);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.Status);
        Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED", result.Code);
    }

    [Theory]
    [InlineData("GET", "/plataforma/delegaciones")]
    [InlineData("POST", "/plataforma/delegaciones/empresas")]
    [InlineData("GET", "/plataforma/delegaciones/17")]
    [InlineData("PUT", "/plataforma/delegaciones/17")]
    [InlineData("PATCH", "/plataforma/delegaciones/17")]
    [InlineData("DELETE", "/plataforma/delegaciones")]
    [InlineData("DELETE", "/plataforma/delegaciones/empresas")]
    [InlineData("DELETE", "/plataforma/delegaciones/0")]
    [InlineData("DELETE", "/plataforma/delegaciones/-1")]
    [InlineData("DELETE", "/plataforma/delegaciones/999999999999")]
    [InlineData("DELETE", "/plataforma/delegaciones/17/otra")]
    [InlineData("GET", "/plataforma/delegaciones/empresas/extra")]
    [InlineData("GET", "/plataforma/delegaciones/empresas/")]
    [InlineData("GET", "/plataforma/delegaciones-malicioso/empresas")]
    [InlineData("POST", "/plataforma/delegaciones/")]
    public async Task RutasVecinasYMetodosNoPermitidos_SiguenBloqueados(string metodo, string ruta)
    {
        var result = await EjecutarAsync(metodo, ruta, autenticado: true);
        Assert.False(result.Forwarded);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.Status);
        Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED", result.Code);
    }

    private static async Task<(int Status, bool Forwarded, string? Code)> EjecutarAsync(
        string metodo, string ruta, bool autenticado)
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p1004b-gate-" + Guid.NewGuid().ToString("N")).Options);
        db.Set<Empresa>().AddRange(new Empresa("Empresa A"), new Empresa("Empresa B"));
        await db.SaveChangesAsync();

        var forwarded = false;
        var middleware = new UnscopedCommercialTenantGuardMiddleware(http =>
        {
            forwarded = true;
            http.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Method = metodo;
        context.Request.Path = ruta;
        context.Request.Headers["X-Empresa-Id"] = "999";
        context.Response.Body = new MemoryStream();
        context.User = autenticado
            ? new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "17") }, "TestJwt"))
            : new ClaimsPrincipal(new ClaimsIdentity());

        await middleware.InvokeAsync(context, db);
        if (forwarded)
            return (context.Response.StatusCode, true, null);

        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, false,
            json.RootElement.GetProperty("code").GetString());
    }
}
