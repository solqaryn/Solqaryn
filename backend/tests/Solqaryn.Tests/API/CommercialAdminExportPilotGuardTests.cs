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
/// La salida XLSX/CSV/PDF reutiliza el servicio administrativo ya acotado
/// por EmpresaId; aquí se comprueba la frontera RBAC y rutas HTTP exactas.
/// </summary>
public sealed class CommercialAdminExportPilotGuardTests
{
    [Fact]
    public async Task Reportes_y_exportaciones_empresariales_exigen_RBAC_y_whitelist_estricta()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-admin-report-ab-" + Guid.NewGuid().ToString("N")).Options;
        await using var db = new AppDbContext(options);
        var empresaA = new Empresa("Administración A");
        var empresaB = new Empresa("Administración B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var exportRoutes = new[]
        {
            "/reportes-administrativos/exportar/usuarios",
            "/reportes-administrativos/exportar/usuarios-accesos",
            "/reportes-administrativos/exportar/roles",
            "/reportes-administrativos/exportar/roles-permisos",
            "/reportes-administrativos/exportar/auditoria"
        };
        var readRoutes = new[]
        {
            "/reportes-administrativos/resumen",
            "/reportes-administrativos/usuarios-accesos",
            "/reportes-administrativos/roles-permisos",
            "/reportes-administrativos/auditoria-resumen"
        };

        async Task<(int Status, int? Tenant, bool Called)> CallAsync(
            string route, int empresa, string user = "a",
            string method = "GET", bool enabled = true,
            bool exportAllowed = true)
        {
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivateAdminReportPilotEnabled"] = enabled.ToString(),
                    ["MultiEmpresa:PrivateReadPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateMediaExportPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateReportPilotEnabled"] = "false"
                }).Build();
            var perms = new Mock<IPermisoService>(MockBehavior.Strict);
            perms.Setup(s => s.TienePermisoAsync(
                    It.IsAny<int>(), It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
                .Returns((int empresaId, ModuloSistema modulo, AccionPermiso accion) =>
                    Task.FromResult(
                        ((user == "a" && empresaId == empresaA.Id) ||
                         (user == "b" && empresaId == empresaB.Id)) &&
                        modulo == ModuloSistema.ReportesAdministrativos &&
                        (accion == AccionPermiso.Ver ||
                         (accion == AccionPermiso.Exportar && exportAllowed))));
            using var svc = new ServiceCollection()
                .AddSingleton<IConfiguration>(cfg)
                .AddSingleton<IPermisoService>(perms.Object).BuildServiceProvider();
            var context = new DefaultHttpContext { RequestServices = svc };
            context.Request.Method = method;
            context.Request.Path = route;
            context.Request.Headers["X-Empresa-Id"] = empresa.ToString();
            context.Response.Body = new MemoryStream();
            context.User = user.Length == 0
                ? new ClaimsPrincipal(new ClaimsIdentity())
                : new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user) }, "TestJWT"));
            bool called = false;
            var gate = new UnscopedCommercialTenantGuardMiddleware(req =>
            {
                called = true;
                req.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            });
            await gate.InvokeAsync(context, db);
            return (context.Response.StatusCode,
                VerifiedCommercialTenant.Resolve(context), called);
        }

        foreach (var path in readRoutes.Concat(exportRoutes))
        {
            foreach (var (id, user) in new[] { (empresaA.Id, "a"), (empresaB.Id, "b") })
            {
                var own = await CallAsync(path, id, user);
                Assert.Equal(204, own.Status);
                Assert.Equal(id, own.Tenant);
                Assert.True(own.Called);
            }
            var cross = await CallAsync(path, empresaB.Id);
            Assert.Equal(503, cross.Status);
            Assert.Null(cross.Tenant);
            Assert.False(cross.Called);

            var off = await CallAsync(path, empresaA.Id, enabled: false);
            Assert.Equal(503, off.Status);
            Assert.False(off.Called);
        }

        var noExport = await CallAsync(exportRoutes[0], empresaA.Id, exportAllowed: false);
        Assert.Equal(503, noExport.Status);
        Assert.Null(noExport.Tenant);
        Assert.Equal(204, (await CallAsync(readRoutes[0], empresaA.Id, exportAllowed: false)).Status);

        foreach (var path in new[]
        {
            "/reportes-administrativos/exportar",
            "/reportes-administrativos/exportar/otro",
            "/reportes-administrativos/exportar/auditoria/otro",
            "/reportes-administrativos/exportar/roles%2Fpermisos",
            "/reportes-administrativos/usuarios-accesos/exportar",
            "/reportes-administrativos/roles",
            "/reportes-administrativos/exportar/auditoria/1"
        })
        {
            var invalid = await CallAsync(path, empresaA.Id);
            Assert.Equal(503, invalid.Status);
            Assert.Null(invalid.Tenant);
        }

        Assert.Equal(503, (await CallAsync(readRoutes[0], empresaA.Id, method: "POST")).Status);
        Assert.Equal(503, (await CallAsync(exportRoutes[0], empresaA.Id, method: "POST")).Status);
        Assert.Equal(503, (await CallAsync(exportRoutes[0], empresaA.Id, user: "")).Status);
    }
}
