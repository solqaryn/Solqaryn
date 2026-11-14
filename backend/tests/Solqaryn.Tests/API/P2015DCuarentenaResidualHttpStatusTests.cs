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
/// P2-015.D: matriz causal de cuarentena residual con dos empresas.
/// Enumera rutas que deben mantener 503 aun ante cabeceras de tenant falsificadas
/// y claims administrativos no respaldados por RBAC; separa las excepciones
/// de seguridad/health que solo atraviesan el middleware.
/// Prueba aislada EF InMemory, no equivale a una prueba HTTP/JWT/MySQL real.
/// </summary>
public sealed class P2015DCuarentenaResidualHttpStatusTests
{
    [Theory]
    [InlineData("GET", "/ventas/19")]
    [InlineData("POST", "/compras")]
    [InlineData("GET", "/facturas/19/pdf")]
    [InlineData("GET", "/reportes/ventas")]
    [InlineData("GET", "/inventario/productos")]
    public async Task Multiempresa_RutasComercialesConHeaderYRolFalsos_SiguenEn503(
        string metodo, string ruta)
    {
        await using var db = NuevoContexto();
        db.Set<Empresa>().AddRange(
            new Empresa("P2015D Empresa A"),
            new Empresa("P2015D Empresa B"));
        await db.SaveChangesAsync();

        foreach (var header in new[] { "1", "2", "99999" })
        {
            var (status, codigo, forwarded) =
                await EjecutarAsync(db, metodo, ruta, header);

            Assert.False(forwarded);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
            Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED", codigo);
        }
    }

    [Theory]
    [InlineData("POST", "/auth/login")]
    [InlineData("GET", "/health")]
    public async Task Multiempresa_ExcepcionesDeSeguridadSoloAtraviesanMiddleware(
        string metodo, string ruta)
    {
        await using var db = NuevoContexto();
        db.Set<Empresa>().AddRange(
            new Empresa("P2015D Empresa A"),
            new Empresa("P2015D Empresa B"));
        await db.SaveChangesAsync();

        var (status, codigo, forwarded) =
            await EjecutarAsync(db, metodo, ruta, "99999");

        // El paso no concede permisos: auth/health responden en sus controles
        // posteriores. Aqui solo se verifica la allowlist de cuarentena.
        Assert.True(forwarded);
        Assert.Equal(StatusCodes.Status204NoContent, status);
        Assert.Null(codigo);
    }

    private static AppDbContext NuevoContexto()
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2015d-quarantine-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(opciones);
    }

    private static async Task<(int Status, string? Code, bool Forwarded)> EjecutarAsync(
        AppDbContext db, string metodo, string ruta, string empresaHeader)
    {
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
        context.Request.Headers["X-Empresa-Id"] = empresaHeader;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "7"),
                new Claim(ClaimTypes.Role, "ADMINISTRADOR")
            }, authenticationType: "TestJwt"));
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, db);
        if (forwarded)
            return (context.Response.StatusCode, null, true);

        context.Response.Body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(context.Response.Body);
        return (
            context.Response.StatusCode,
            payload.RootElement.GetProperty("code").GetString(),
            false);
    }
}
