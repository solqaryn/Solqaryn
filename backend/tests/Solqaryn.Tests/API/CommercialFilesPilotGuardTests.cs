using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
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
/// Sólo habilitar administración de archivos comerciales con doble autoridad
/// empresarial; Cloudinary y repositorios conservan validaciones tenant.
/// Este test NO sustituye descargas/altas físicas contra proveedor externo.
/// </summary>
public sealed class CommercialFilesPilotGuardTests
{
    [Fact]
    public async Task ArchivosComprasYProducto_A_B_SoloConRutaMetodoFormatoPermisoYBandera()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-files-ab-" + Guid.NewGuid().ToString("N")).Options;
        await using var db = new AppDbContext(options);
        var a = new Empresa("Archivos A");
        var b = new Empresa("Archivos B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        async Task<(int Code, bool Next, int? Owner)> RequestAsync(
            string path, string method, int empresa, string user,
            bool enabled = true, bool form = false, int fileCount = 0,
            long? length = 100)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivateCommercialFilesPilotEnabled"] = enabled.ToString(),
                    ["MultiEmpresa:PrivateFinancialPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateMediaExportPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateReadPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateDraftPilotEnabled"] = "false"
                }).Build();
            var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
            permisos.Setup(p => p.TienePermisoAsync(
                It.IsAny<int>(), It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
                .Returns((int id, ModuloSistema module, AccionPermiso action) =>
                    Task.FromResult(
                        ((user == "a" && id == a.Id) || (user == "b" && id == b.Id)) &&
                        ((module == ModuloSistema.Compras &&
                          action is AccionPermiso.Ver or AccionPermiso.Editar or AccionPermiso.Exportar) ||
                         (module == ModuloSistema.Productos && action == AccionPermiso.Editar))));
            using var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddSingleton<IPermisoService>(permisos.Object).BuildServiceProvider();

            var http = new DefaultHttpContext { RequestServices = services };
            http.Request.Path = path;
            http.Request.Method = method;
            http.Request.Headers["X-Empresa-Id"] = empresa.ToString();
            http.Request.ContentLength = length;
            http.Request.ContentType = form
                ? "multipart/form-data; boundary=p4-fixture" : "application/json";
            http.Response.Body = new MemoryStream();
            http.User = user.Length == 0
                ? new ClaimsPrincipal(new ClaimsIdentity())
                : new ClaimsPrincipal(new ClaimsIdentity(new[]
                { new Claim(ClaimTypes.NameIdentifier, user) }, "JWT"));

            if (form)
            {
                var files = new FormFileCollection();
                for (var i = 0; i < fileCount; i++)
                    files.Add(new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3,
                        "archivo", "file" + i + ".png"));
                http.Features.Set<IFormFeature>(new FormFeature(new FormCollection(
                    new Dictionary<string, StringValues> { ["Nombre"] = "P4 A-B" }, files)));
            }

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

        foreach (var (path, method, form, files) in new[]
        {
            ("/compras/1/documentos", "GET", false, 0),
            ("/compras/1/documentos/2/descargar", "GET", false, 0),
            ("/compras/1/documentos", "POST", true, 1),
            ("/compras/1/documentos/2", "DELETE", false, 0),
            ("/productos/1", "PUT", true, 0)
        })
        {
            foreach (var (user, id, other) in new[]
            {
                ("a", a.Id, b.Id),
                ("b", b.Id, a.Id)
            })
            {
                var own = await RequestAsync(path, method, id, user, form: form, fileCount: files);
                Assert.Equal(200, own.Code);
                Assert.True(own.Next);
                Assert.Equal(id, own.Owner);

                var cross = await RequestAsync(path, method, other, user,
                    form: form, fileCount: files);
                Assert.Equal(503, cross.Code);
                Assert.False(cross.Next);
                Assert.Null(cross.Owner);

                var off = await RequestAsync(path, method, id, user,
                    enabled: false, form: form, fileCount: files);
                Assert.Equal(503, off.Code);
                Assert.False(off.Next);
            }
        }

        foreach (var (path, method) in new[]
        {
            ("/compras/0/documentos", "GET"),
            ("/compras/-1/documentos", "POST"),
            ("/compras/1/documentos/0/descargar", "GET"),
            ("/compras/1/documentos/2/descargar/extra", "GET"),
            ("/compras/1/documentos/2/extra", "DELETE"),
            ("/compras/1/documentos/2/descargar", "POST"),
            ("/compras/1/documentos", "PUT"),
            ("/productos/1/imagenes", "PUT"),
            ("/productos/1/activar", "PUT"),
            ("/productos/1", "POST"),
            ("/facturas/1/pagos", "POST"),
            ("/ventas/1/anular", "POST")
        })
        {
            var denied = await RequestAsync(path, method, a.Id, "a");
            Assert.Equal(503, denied.Code);
            Assert.False(denied.Next);
        }

        foreach (var denied in new[]
        {
            await RequestAsync("/compras/1/documentos", "POST", a.Id, "a"),
            await RequestAsync("/compras/1/documentos", "POST", a.Id, "a",
                form: true, fileCount: 0),
            await RequestAsync("/compras/1/documentos", "POST", a.Id, "a",
                form: true, fileCount: 2),
            await RequestAsync("/productos/1", "PUT", a.Id, "a"),
            await RequestAsync("/productos/1", "PUT", a.Id, "a",
                form: true, length: 17 * 1024 * 1024),
            await RequestAsync("/productos/1", "PUT", a.Id, ""),
            await RequestAsync("/productos/1", "PUT", a.Id, "viewer", form: true)
        })
        {
            Assert.Equal(503, denied.Code);
            Assert.False(denied.Next);
            Assert.Null(denied.Owner);
        }
    }
}
