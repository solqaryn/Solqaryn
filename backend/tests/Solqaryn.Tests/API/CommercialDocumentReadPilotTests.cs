using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// Cobertura de la frontera HTTP RBAC de lectura comercial A/B. Las llamadas
/// HTTP y los permisos del mock NO sustituyen aceptación de JWT ni repositorios
/// end-to-end con membresías reales.
/// </summary>
public sealed class CommercialDocumentReadPilotTests
{
    [Fact]
    public async Task GET_Ventas_Compras_Facturas_Exige_Ver_Y_Esconde_Documentos_De_B()
    {
        var accessor = new HttpContextAccessor();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-commercial-reads-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor);

        var companyA = new Empresa("Comercial A");
        var companyB = new Empresa("Comercial B");
        db.Set<Empresa>().AddRange(companyA, companyB, new Empresa("Tercer tenant existente"));
        await db.SaveChangesAsync();

        async Task<(int VentaId, int CompraId, int FacturaId)> SeedAsync(int empresaId, string suffix)
        {
            var trusted = new DefaultHttpContext();
            trusted.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaId;
            accessor.HttpContext = trusted;

            var venta = new Venta
            {
                EmpresaId = empresaId, NumeroVenta = "P4-V-" + suffix,
                CreadoPorUsuarioId = 1
            };
            var compra = new Compra
            {
                EmpresaId = empresaId, NumeroCompra = "P4-C-" + suffix,
                CreadoPorUsuarioId = 1
            };
            db.Ventas.Add(venta);
            db.Compras.Add(compra);
            await db.SaveChangesAsync();

            var factura = new Factura
            {
                EmpresaId = empresaId, VentaId = venta.Id,
                NumeroFactura = "P4-F-" + suffix,
                EmpresaNombre = suffix, ClienteNombre = "Cliente prueba",
                VendedorUsuarioId = 1
            };
            db.Facturas.Add(factura);
            await db.SaveChangesAsync();
            return (venta.Id, compra.Id, factura.Id);
        }

        var rowsA = await SeedAsync(companyA.Id, "A");
        var rowsB = await SeedAsync(companyB.Id, "B");
        async Task<(int Status, int? Marca, int[] Ids)> RequestAsync(
            string user, int header, string method, string route, bool flag = true)
        {
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivateReadPilotEnabled"] = flag.ToString(),
                    ["MultiEmpresa:PrivateWritePilotEnabled"] = "false"
                }).Build();
            var http = new DefaultHttpContext();
            http.Request.Method = method;
            http.Request.Path = route;
            http.Request.Headers["X-Empresa-Id"] = header.ToString();
            http.Response.Body = new MemoryStream();
            http.User = user.Length == 0
                ? new ClaimsPrincipal(new ClaimsIdentity())
                : new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user)
                }, "TestJWT"));
            var perms = new Mock<IPermisoService>(MockBehavior.Strict);
            perms.Setup(p => p.TienePermisoAsync(
                    It.IsAny<int>(), It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
                .Returns((int empresa, ModuloSistema module, AccionPermiso action) =>
                    Task.FromResult(
                        ((user == "user-a" && empresa == companyA.Id) ||
                         (user == "user-b" && empresa == companyB.Id)) &&
                        (module == ModuloSistema.Ventas ||
                         module == ModuloSistema.Compras ||
                         module == ModuloSistema.Facturacion) &&
                        action == AccionPermiso.Ver));
            using var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(cfg)
                .AddSingleton<IPermisoService>(perms.Object)
                .BuildServiceProvider();
            http.RequestServices = services;
            accessor.HttpContext = http;
            int[] results = Array.Empty<int>();
            var called = false;
            var guard = new UnscopedCommercialTenantGuardMiddleware(async ctx =>
            {
                called = true;
                var path = ctx.Request.Path.Value ?? string.Empty;
                var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (path.StartsWith("/ventas", StringComparison.OrdinalIgnoreCase))
                {
                    IQueryable<Venta> query = db.Ventas.AsNoTracking();
                    if (parts.Length == 2 && int.TryParse(parts[1], out var id))
                        query = query.Where(v => v.Id == id);
                    results = await query.OrderBy(v => v.Id).Select(v => v.Id).ToArrayAsync();
                }
                else if (path.StartsWith("/compras", StringComparison.OrdinalIgnoreCase))
                {
                    IQueryable<Compra> query = db.Compras.AsNoTracking();
                    if (parts.Length == 2 && int.TryParse(parts[1], out var id))
                        query = query.Where(c => c.Id == id);
                    results = await query.OrderBy(c => c.Id).Select(c => c.Id).ToArrayAsync();
                }
                else if (path.StartsWith("/facturas", StringComparison.OrdinalIgnoreCase))
                {
                    IQueryable<Factura> query = db.Facturas.AsNoTracking();
                    if (parts.Length == 2 && int.TryParse(parts[1], out var id))
                        query = query.Where(f => f.Id == id);
                    if (parts.Length == 3 && int.TryParse(parts[2], out var saleId))
                        query = query.Where(f => f.VentaId == saleId);
                    results = await query.OrderBy(f => f.Id).Select(f => f.Id).ToArrayAsync();
                }
                ctx.Response.StatusCode = StatusCodes.Status200OK;
            });
            await guard.InvokeAsync(http, db);
            if (http.Response.StatusCode == 503)
                Assert.False(called);
            return (http.Response.StatusCode,
                VerifiedCommercialTenant.Resolve(http), results);
        }

        foreach (var (path, own, foreign) in new[]
        {
            ("/ventas", rowsA.VentaId, rowsB.VentaId),
            ($"/ventas/{rowsA.VentaId}", rowsA.VentaId, rowsB.VentaId),
            ("/compras", rowsA.CompraId, rowsB.CompraId),
            ($"/compras/{rowsA.CompraId}", rowsA.CompraId, rowsB.CompraId),
            ("/facturas", rowsA.FacturaId, rowsB.FacturaId),
            ($"/facturas/{rowsA.FacturaId}", rowsA.FacturaId, rowsB.FacturaId),
            ($"/facturas/venta/{rowsA.VentaId}", rowsA.FacturaId, rowsB.FacturaId)
        })
        {
            var a = await RequestAsync("user-a", companyA.Id, "GET", path);
            Assert.Equal(200, a.Status);
            Assert.Equal(companyA.Id, a.Marca);
            Assert.Equal(new[] { own }, a.Ids);
            Assert.DoesNotContain(foreign, a.Ids);
        }
        // La misma cuenta B puede consultar SUS documentos, no los de A.
        var b = await RequestAsync("user-b", companyB.Id, "GET", "/facturas");
        Assert.Equal(new[] { rowsB.FacturaId }, b.Ids);
        var foreignSale = await RequestAsync(
            "user-a", companyA.Id, "GET", $"/facturas/venta/{rowsB.VentaId}");
        Assert.Equal(200, foreignSale.Status);
        Assert.Empty(foreignSale.Ids);

        foreach (var invalid in new[]
        {
            await RequestAsync("user-a", companyB.Id, "GET", "/ventas"),
            await RequestAsync("", companyA.Id, "GET", "/facturas"),
            await RequestAsync("user-a", companyA.Id, "GET", "/facturas", false),
            await RequestAsync("user-a", companyA.Id, "POST", "/ventas"),
            await RequestAsync("user-a", companyA.Id, "POST", "/facturas/1/pagos"),
            await RequestAsync("user-a", companyA.Id, "GET", "/facturas/1/pdf"),
            await RequestAsync("user-a", companyA.Id, "GET", "/facturas/formatos-pdf"),
            await RequestAsync("user-a", companyA.Id, "GET", "/compras/1/documentos"),
            await RequestAsync("user-a", companyA.Id, "GET", "/ventas/productos/buscar"),
            await RequestAsync("user-a", companyA.Id, "GET", "/facturas/venta/1/pdf")
        })
        {
            Assert.Equal(503, invalid.Status);
            Assert.Null(invalid.Marca);
            Assert.Empty(invalid.Ids);
        }
    }
}
