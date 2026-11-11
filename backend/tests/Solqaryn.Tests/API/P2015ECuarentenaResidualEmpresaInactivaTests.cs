using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.API.Middleware;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// P2-015.E: regresion causal de cuarentena cuando se crea una segunda empresa
/// y esta queda inactiva. La existencia de datos historicos impide reabrir
/// operaciones comerciales privadas por cabeceras suministradas por clientes.
/// Prueba aislada EF InMemory; no acredita HTTP real, JWT ni MySQL.
/// </summary>
public sealed class P2015ECuarentenaResidualEmpresaInactivaTests
{
    [Fact]
    public async Task SegundaEmpresaInactiva_Mantiene503AunqueHeaderApunteALaActiva()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2015e-quarantine-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);

        var empresaA = new Empresa("P2015E Empresa A");
        db.Set<Empresa>().Add(empresaA);
        await db.SaveChangesAsync();

        // Control positivo: con un solo tenant persiste el camino legacy.
        var antes = await EjecutarLecturaPrivadaAsync(db, empresaA.Id.ToString());
        Assert.True(antes.Forwarded);
        Assert.Equal(StatusCodes.Status204NoContent, antes.Status);
        Assert.Null(antes.Code);

        // El segundo tenant conserva identidad e historia aunque se desactive.
        // No es seguro elegir implicitamente A por ser la unica empresa activa.
        var empresaB = new Empresa("P2015E Empresa B");
        empresaB.Desactivar();
        db.Set<Empresa>().Add(empresaB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        foreach (var header in new[]
                 {
                     empresaA.Id.ToString(),
                     empresaB.Id.ToString(),
                     "999999"
                 })
        {
            var despues = await EjecutarLecturaPrivadaAsync(db, header);

            Assert.False(despues.Forwarded);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, despues.Status);
            Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED", despues.Code);
        }

        // El rechazo no modifica datos ni reactiva la empresa historica.
        Assert.Equal(2, await db.Set<Empresa>().IgnoreQueryFilters().CountAsync());
        Assert.False(await db.Set<Empresa>().IgnoreQueryFilters()
            .Where(e => e.Id == empresaB.Id)
            .Select(e => e.Activa)
            .SingleAsync());
    }

    private static async Task<(int Status, string? Code, bool Forwarded)>
        EjecutarLecturaPrivadaAsync(AppDbContext db, string header)
    {
        var forwarded = false;
        var guard = new UnscopedCommercialTenantGuardMiddleware(http =>
        {
            forwarded = true;
            http.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Get;
        http.Request.Path = "/ventas/19";
        http.Request.Headers["X-Empresa-Id"] = header;
        http.Response.Body = new MemoryStream();

        await guard.InvokeAsync(http, db);
        if (forwarded)
            return (http.Response.StatusCode, null, true);

        http.Response.Body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(http.Response.Body);
        var code = payload.RootElement.GetProperty("code").GetString();
        return (http.Response.StatusCode, code, false);
    }
}
