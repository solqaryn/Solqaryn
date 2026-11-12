using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-018.E: smoke del guard HTTP real combinado con filtros EF A/B sintéticos.
/// Invoca middleware con DefaultHttpContext y EF InMemory: no es un request firmado
/// de TestServer ni una certificación de JWT, MySQL o endpoints de producción.
/// </summary>
public sealed class P2018ESmokeApiTenantsABTests
{
    [Theory]
    [InlineData("GET", "/ventas")]
    [InlineData("GET", "/compras")]
    [InlineData("GET", "/facturas")]
    [InlineData("GET", "/productos")]
    [InlineData("GET", "/inventario/movimientos")]
    [InlineData("GET", "/reportes-administrativos/resumen")]
    [InlineData("POST", "/ventas")]
    [InlineData("GET", "/compras/documentos")]
    public async Task Sin_opt_in_privado_ni_pertenencia_verificada_ninguna_ruta_cruza_tenants(
        string method, string route)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        http.Request.Path = route;
        http.Response.Body = new MemoryStream();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "41") }, "SyntheticAuthenticated"));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MultiEmpresa:PrivateReadPilotEnabled"] = "false",
                ["MultiEmpresa:PrivateWritePilotEnabled"] = "false",
                ["MultiEmpresa:PrivateAdminReportPilotEnabled"] = "false"
            })
            .Build();
        using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config)
            .BuildServiceProvider();
        http.RequestServices = services;

        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p2018e-api-" + Guid.NewGuid().ToString("N"))
                .Options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
        var companyA = new Empresa("P2018E_PRIVADA_A");
        var companyB = new Empresa("P2018E_PRIVADA_B");
        db.Set<Empresa>().AddRange(companyA, companyB);
        await db.SaveChangesAsync();

        // La cabecera elegida por el cliente no es prueba de pertenencia,
        // aunque el request contenga una identidad sintética autenticada.
        http.Request.Headers["X-Empresa-Id"] = companyB.Id.ToString();
        var passedToEndpoint = false;
        var middleware = new UnscopedCommercialTenantGuardMiddleware(ctx =>
        {
            passedToEndpoint = true;
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(http, db);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, http.Response.StatusCode);
        Assert.False(passedToEndpoint);
        Assert.Equal("private, no-store, max-age=0",
            http.Response.Headers.CacheControl.ToString());
        http.Response.Body.Position = 0;
        using var reader = new StreamReader(http.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("TENANT_DATA_ISOLATION_REQUIRED", body, StringComparison.Ordinal);
        Assert.DoesNotContain(companyA.Nombre, body, StringComparison.Ordinal);
        Assert.DoesNotContain(companyB.Nombre, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Filtro_EF_respeta_marca_verificada_ABA_y_no_cabecera_falsificada()
    {
        var http = new DefaultHttpContext();
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p2018e-ef-" + Guid.NewGuid().ToString("N"))
                .Options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
        var companyA = new Empresa("P2018E_A");
        var companyB = new Empresa("P2018E_B");
        db.Set<Empresa>().AddRange(companyA, companyB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = companyA.Id;
        var productA = new Producto { EmpresaId = companyA.Id, Nombre = "P2018E item A" };
        db.Productos.Add(productA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = companyB.Id;
        var productB = new Producto { EmpresaId = companyB.Id, Nombre = "P2018E item B" };
        db.Productos.Add(productB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        async Task<int> ReadAsAsync(int trustedId, int forgedId)
        {
            http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = trustedId;
            http.Request.Headers["X-Empresa-Id"] = forgedId.ToString();
            return (await db.Productos.AsNoTracking().SingleAsync()).Id;
        }

        Assert.Equal(productA.Id, await ReadAsAsync(companyA.Id, companyB.Id));
        Assert.Equal(productB.Id, await ReadAsAsync(companyB.Id, companyA.Id));
        Assert.Equal(productA.Id, await ReadAsAsync(companyA.Id, companyB.Id));
    }
}
