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
/// Cierre privado de pagos y anulaciones A/B: no introduce un permiso nuevo
/// ni sustituye las verificaciones relacionales y transaccionales existentes.
/// </summary>
public sealed class CommercialFinancialPilotGuardTests
{
    [Fact]
    public async Task Pagos_y_anulaciones_AB_exigen_RBAC_exacto_JSON_y_flag()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-financial-ab-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);
        var empresaA = new Empresa("P4 A");
        var empresaB = new Empresa("P4 B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        async Task<(int Code, bool Next, int? Owner)> Request(
            string ruta, int id, string user = "user-a", bool enabled = true,
            string method = "POST", string? contentType = "application/json",
            long? length = 24)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivateFinancialPilotEnabled"] = enabled.ToString(),
                    ["MultiEmpresa:PrivateConfirmPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateDraftPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateReadPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateWritePilotEnabled"] = "false"
                }).Build();
            var permissions = new Mock<IPermisoService>(MockBehavior.Strict);
            permissions.Setup(p => p.TienePermisoAsync(
                    It.IsAny<int>(), It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
                .Returns((int empresaId, ModuloSistema module, AccionPermiso action) =>
                    Task.FromResult(
                        ((user == "user-a" && empresaId == empresaA.Id) ||
                         (user == "user-b" && empresaId == empresaB.Id)) &&
                        ((module == ModuloSistema.Facturacion &&
                          action is AccionPermiso.Aplicar or AccionPermiso.Anular) ||
                         (module is ModuloSistema.Ventas or ModuloSistema.Compras &&
                          action == AccionPermiso.Anular))));
            using var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddSingleton<IPermisoService>(permissions.Object)
                .BuildServiceProvider();
            var http = new DefaultHttpContext { RequestServices = services };
            http.Request.Path = ruta;
            http.Request.Method = method;
            http.Request.Headers["X-Empresa-Id"] = id.ToString();
            http.Request.ContentType = contentType;
            http.Request.ContentLength = length;
            http.Response.Body = new MemoryStream();
            http.User = user.Length == 0
                ? new ClaimsPrincipal(new ClaimsIdentity())
                : new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user)
                }, "TestJWT"));
            bool next = false;
            var middleware = new UnscopedCommercialTenantGuardMiddleware(ctx =>
            {
                next = true;
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            });
            await middleware.InvokeAsync(http, db);
            return (http.Response.StatusCode, next, VerifiedCommercialTenant.Resolve(http));
        }

        foreach (var ruta in new[]
        {
            "/facturas/1/pagos",
            "/facturas/1/pagos/9/anular",
            "/ventas/2/anular",
            "/compras/2/anular"
        })
        {
            foreach (var (user, own, other) in new[]
            {
                ("user-a", empresaA.Id, empresaB.Id),
                ("user-b", empresaB.Id, empresaA.Id)
            })
            {
                var ok = await Request(ruta, own, user);
                Assert.Equal(200, ok.Code);
                Assert.True(ok.Next);
                Assert.Equal(own, ok.Owner);

                var spoof = await Request(ruta, other, user);
                Assert.Equal(503, spoof.Code);
                Assert.False(spoof.Next);
                Assert.Null(spoof.Owner);

                var off = await Request(ruta, own, user, enabled: false);
                Assert.Equal(503, off.Code);
                Assert.False(off.Next);
                Assert.Null(off.Owner);
            }
        }

        foreach (var route in new[]
        {
            "/facturas/0/pagos", "/facturas/-1/pagos", "/facturas/1/pagos/0/anular",
            "/facturas/1/pagos/-3/anular", "/facturas/1/pagos/2/anular/",
            "/facturas/1/pagos/2/anular/otra", "/facturas/1/pagos/2/editar",
            "/facturas/1/estado", "/facturas/1/compartir/correo",
            "/ventas/0/anular", "/ventas/1/anular/", "/ventas/1/confirmar",
            "/compras/1/anular/otra", "/compras/1/confirmar",
            "/inventario/movimientos", "/productos/1/imagenes/descargar-todas"
        })
        {
            var denied = await Request(route, empresaA.Id);
            Assert.Equal(503, denied.Code);
            Assert.False(denied.Next);
        }

        foreach (var denied in new[]
        {
            await Request("/facturas/1/pagos", empresaA.Id, ""),
            await Request("/facturas/1/pagos", empresaA.Id, "viewer"),
            await Request("/facturas/1/pagos", empresaA.Id, method: "PUT"),
            await Request("/facturas/1/pagos", empresaA.Id, contentType: "multipart/form-data"),
            await Request("/facturas/1/pagos", empresaA.Id, length: null),
            await Request("/facturas/1/pagos", empresaA.Id, length: 33000),
            await Request("/facturas/1/pagos", empresaA.Id, length: 0)
        })
        {
            Assert.Equal(503, denied.Code);
            Assert.False(denied.Next);
            Assert.Null(denied.Owner);
        }
    }
}
