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
/// Exportación binaria de una imagen o ZIP: whitelist exacta con Productos.Exportar.
/// Esta prueba de guard no sustituye una descarga Cloudinary física en DEV.
/// </summary>
public sealed class CommercialMediaExportPilotGuardTests
{
    [Fact]
    public async Task Descargas_imagenes_AB_exigen_Exportar_tenant_y_solo_rutas_GET_exactas()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-media-ab-" + Guid.NewGuid().ToString("N")).Options;
        await using var db = new AppDbContext(options);
        var a = new Empresa("Media A");
        var b = new Empresa("Media B");
        db.Set<Empresa>().AddRange(a, b, new Empresa("Tercer tenant existente"));
        await db.SaveChangesAsync();

        async Task<(int Http, bool Next, int? Owner)> Call(
            string path, int company, string user, bool exportGrant = true,
            bool flag = true, string method = "GET")
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivateMediaExportPilotEnabled"] = flag.ToString(),
                    ["MultiEmpresa:PrivateReadPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateWritePilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateDraftPilotEnabled"] = "false"
                }).Build();
            var permissions = new Mock<IPermisoService>(MockBehavior.Strict);
            permissions.Setup(x => x.TienePermisoAsync(
                    It.IsAny<int>(), It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
                .Returns((int empresa, ModuloSistema module, AccionPermiso action) =>
                    Task.FromResult(empresa == a.Id && user == "user-a" &&
                        exportGrant && module == ModuloSistema.Productos &&
                        action == AccionPermiso.Exportar));
            using var svc = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddSingleton<IPermisoService>(permissions.Object)
                .BuildServiceProvider();
            var http = new DefaultHttpContext { RequestServices = svc };
            http.Request.Method = method;
            http.Request.Path = path;
            http.Request.Headers["X-Empresa-Id"] = company.ToString();
            http.User = user.Length == 0
                ? new ClaimsPrincipal(new ClaimsIdentity())
                : new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user)
                }, "JWT"));
            http.Response.Body = new MemoryStream();
            bool called = false;
            var middleware = new UnscopedCommercialTenantGuardMiddleware(ctx =>
            {
                called = true;
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            });
            await middleware.InvokeAsync(http, db);
            return (http.Response.StatusCode, called, VerifiedCommercialTenant.Resolve(http));
        }

        foreach (var route in new[]
        {
            "/productos/1/imagenes/2/descargar",
            "/productos/19/imagenes/descargar-todas"
        })
        {
            var own = await Call(route, a.Id, "user-a");
            Assert.Equal(200, own.Http);
            Assert.True(own.Next);
            Assert.Equal(a.Id, own.Owner);

            var cross = await Call(route, b.Id, "user-a");
            Assert.Equal(503, cross.Http);
            Assert.False(cross.Next);
            Assert.Null(cross.Owner);

            var withoutExport = await Call(route, a.Id, "user-a", exportGrant: false);
            Assert.Equal(503, withoutExport.Http);
            Assert.Null(withoutExport.Owner);

            var off = await Call(route, a.Id, "user-a", flag: false);
            Assert.Equal(503, off.Http);
            Assert.False(off.Next);

            var anon = await Call(route, a.Id, "");
            Assert.Equal(503, anon.Http);
        }

        foreach (var route in new[]
        {
            "/productos/0/imagenes/descargar-todas",
            "/productos/-1/imagenes/descargar-todas",
            "/productos/1/imagenes/0/descargar",
            "/productos/1/imagenes/-2/descargar",
            "/productos/1/imagenes/2/descargar/",
            "/productos/1/imagenes/2/descargar-todas",
            "/productos/1/imagenes/descargar-todas/otro",
            "/productos/1/imagenes/2/editar",
            "/productos/1/imagenes/descargar",
            "/productos/1/imagenes/nada/descargar",
            "/productos/1/imagenes",
            "/productos/1/exportar",
            "/productos/1/imagenes/descargar-todas/download"
        })
        {
            var rejected = await Call(route, a.Id, "user-a");
            Assert.Equal(503, rejected.Http);
            Assert.Null(rejected.Owner);
        }
        foreach (var method in new[] { "POST", "PUT", "DELETE" })
        {
            var rejected = await Call("/productos/1/imagenes/descargar-todas", a.Id, "user-a",
                method: method);
            Assert.Equal(503, rejected.Http);
        }
    }
}
