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
/// Rutas privadas de PDF, reportes e inventario sólo bajo flags independientes,
/// usuario+Empresa verificados, permiso exacto y método GET. Prueba de los
/// bordes HTTP; los resultados SQL y PDF se ejercitan en MySQL real ephemeral.
/// </summary>
public sealed class CommercialReadIntegrationsPilotGuardTests
{
    [Fact]
    public async Task Rutas_exactas_PDF_reportes_inventario_EXIGEN_RBAC_y_no_abren_otros_flows()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-read-integrations-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);
        var a = new Empresa("Lectura integrada A");
        var b = new Empresa("Lectura integrada B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        async Task<(int Status, bool Next, int? Owner)> RequestAsync(
            string path, int header, string user, bool flags = true, string method = "GET")
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivatePdfPilotEnabled"] = flags.ToString(),
                    ["MultiEmpresa:PrivateReportPilotEnabled"] = flags.ToString(),
                    ["MultiEmpresa:PrivateInventoryReadPilotEnabled"] = flags.ToString(),
                    ["MultiEmpresa:PrivateReadPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateDraftPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateConfirmPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateWritePilotEnabled"] = "false"
                }).Build();

            var permissions = new Mock<IPermisoService>(MockBehavior.Strict);
            permissions.Setup(x => x.TienePermisoAsync(
                    It.IsAny<int>(), It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
                .Returns((int empresa, ModuloSistema modulo, AccionPermiso accion) =>
                    Task.FromResult(empresa == a.Id && user == "a" &&
                        ((modulo == ModuloSistema.Facturacion && accion == AccionPermiso.Exportar) ||
                         (modulo == ModuloSistema.Ventas && accion == AccionPermiso.Ver) ||
                         (modulo == ModuloSistema.Compras && accion == AccionPermiso.Ver) ||
                         (modulo == ModuloSistema.Inventario && accion == AccionPermiso.Ver) ||
                         (modulo == ModuloSistema.MovimientosInventario && accion == AccionPermiso.Ver))));
            using var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddSingleton<IPermisoService>(permissions.Object)
                .BuildServiceProvider();

            var http = new DefaultHttpContext { RequestServices = services };
            http.Request.Method = method;
            http.Request.Path = path;
            http.Request.Headers["X-Empresa-Id"] = header.ToString();
            http.Response.Body = new MemoryStream();
            http.User = user.Length == 0
                ? new ClaimsPrincipal(new ClaimsIdentity())
                : new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user)
                }, "TestJWT"));
            var called = false;
            var guard = new UnscopedCommercialTenantGuardMiddleware(ctx =>
            {
                called = true;
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            });
            await guard.InvokeAsync(http, db);
            return (http.Response.StatusCode, called, VerifiedCommercialTenant.Resolve(http));
        }

        var routes = new[]
        {
            "/facturas/7/pdf", "/ventas/reportes/resumen",
            "/ventas/reportes/detalle", "/compras/reportes/detalle",
            "/existencias-variante", "/existencias-variante/5",
            "/inventario/movimientos", "/inventario/movimientos/paged"
        };
        foreach (var path in routes)
        {
            var allowed = await RequestAsync(path, a.Id, "a");
            Assert.Equal(200, allowed.Status);
            Assert.True(allowed.Next);
            Assert.Equal(a.Id, allowed.Owner);

            var foreign = await RequestAsync(path, b.Id, "a");
            Assert.Equal(503, foreign.Status);
            Assert.False(foreign.Next);
            Assert.Null(foreign.Owner);

            var off = await RequestAsync(path, a.Id, "a", flags: false);
            Assert.Equal(503, off.Status);
            Assert.Null(off.Owner);
        }

        foreach (var path in new[]
        {
            "/facturas/0/pdf", "/facturas/-1/pdf", "/facturas/7/pdf/compartir",
            "/facturas/7/pagos", "/facturas/7/compartir/correo",
            "/ventas/reportes/detalle/exportar", "/ventas/reportes/otro",
            "/compras/reportes", "/compras/reportes/detalle/pdf",
            "/inventario/movimientos/exportar", "/existencias-variante/-1",
            "/inventario/ajustes", "/productos/1/imagenes/descargar-todas"
        })
        {
            var blocked = await RequestAsync(path, a.Id, "a");
            Assert.Equal(503, blocked.Status);
            Assert.False(blocked.Next);
            Assert.Null(blocked.Owner);
        }

        var unauthorized = await RequestAsync("/facturas/7/pdf", a.Id, "");
        Assert.Equal(503, unauthorized.Status);
        var badMethod = await RequestAsync("/facturas/7/pdf", a.Id, "a", method: "POST");
        Assert.Equal(503, badMethod.Status);
    }
}
