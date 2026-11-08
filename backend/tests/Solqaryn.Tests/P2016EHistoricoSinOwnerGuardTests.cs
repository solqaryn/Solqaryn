using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-016.E: un historico sintetico sin EmpresaId debe permanecer sin owner,
/// mientras la cuarentena HTTP rechaza accesos sin tenant verificado en A/B.
/// EF InMemory y middleware aislado: no acredita JWT/HTTP real ni MySQL.
/// </summary>
public sealed class P2016EHistoricoSinOwnerGuardTests
{
    [Fact]
    public async Task Historico_sin_owner_no_sale_por_http_sin_tenant_verificado_ni_header_falsificado()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2016e-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);
        var empresaA = new Empresa("P2016E Empresa A");
        var empresaB = new Empresa("P2016E Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var historica = new Categoria { Nombre = "P2016E-HISTORICO-SIN-OWNER" };
        db.Categorias.Add(historica);
        await db.SaveChangesAsync();
        Assert.Null(db.Entry(historica).Property<int?>("EmpresaId").CurrentValue);

        foreach (var idFalsificado in new[] { empresaA.Id, empresaB.Id })
        {
            var siguienteEjecutado = false;
            var gate = new UnscopedCommercialTenantGuardMiddleware(context =>
            {
                siguienteEjecutado = true;
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            });
            var http = new DefaultHttpContext();
            http.Request.Method = "GET";
            http.Request.Path = "/productos";
            http.Request.Headers["X-Empresa-Id"] = idFalsificado.ToString();
            http.Response.Body = new MemoryStream();

            await gate.InvokeAsync(http, db);

            Assert.False(siguienteEjecutado);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, http.Response.StatusCode);
            Assert.Null(VerifiedCommercialTenant.Resolve(http));
            http.Response.Body.Position = 0;
            using var json = await JsonDocument.ParseAsync(http.Response.Body);
            Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED",
                json.RootElement.GetProperty("code").GetString());
            Assert.DoesNotContain("P2016E-HISTORICO-SIN-OWNER",
                json.RootElement.ToString(), StringComparison.Ordinal);
        }

        // La cuarentena no debe borrar ni asignar propietario al legado.
        Assert.Null(db.Entry(historica).Property<int?>("EmpresaId").CurrentValue);
        Assert.Equal(1, await db.Categorias.IgnoreQueryFilters().CountAsync());
    }
}
