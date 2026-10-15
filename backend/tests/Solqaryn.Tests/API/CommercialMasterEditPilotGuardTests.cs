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
/// Matriz de control de PUT de clientes/proveedores: permiso Editar,
/// empresa verificada, JSON acotado y cuarentena de otras rutas.
/// La prueba de persistencia y rechazo de IDs extranjeros corre con MySQL/JWT.
/// </summary>
public sealed class CommercialMasterEditPilotGuardTests
{
    [Fact]
    public async Task EditarMaestros_A_B_SoloRutasValidasConFlagYPermiso()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-master-edit-" + Guid.NewGuid().ToString("N")).Options;
        await using var db = new AppDbContext(options);
        var a = new Empresa("Edicion A");
        var b = new Empresa("Edicion B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        async Task<(int Code, bool Next, int? Owner)> RequestAsync(
            string path, string method, int company, string user,
            bool enabled = true, bool allowEdit = true,
            string contentType = "application/json", long? length = 128)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivateMasterEditPilotEnabled"] = enabled.ToString()
                }).Build();
            var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
            permisos.Setup(x => x.TienePermisoAsync(
                It.IsAny<int>(), It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
                .Returns((int id, ModuloSistema module, AccionPermiso action) =>
                    Task.FromResult(allowEdit && action == AccionPermiso.Editar &&
                        (module == ModuloSistema.Clientes || module == ModuloSistema.Proveedores) &&
                        ((user == "a" && id == a.Id) || (user == "b" && id == b.Id))));
            using var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddSingleton<IPermisoService>(permisos.Object)
                .BuildServiceProvider();

            var http = new DefaultHttpContext { RequestServices = services };
            http.Request.Path = path;
            http.Request.Method = method;
            http.Request.Headers["X-Empresa-Id"] = company.ToString();
            http.Request.ContentType = contentType;
            http.Request.ContentLength = length;
            http.Response.Body = new MemoryStream();
            http.User = string.IsNullOrEmpty(user)
                ? new ClaimsPrincipal(new ClaimsIdentity())
                : new ClaimsPrincipal(new ClaimsIdentity(new[]
                { new Claim(ClaimTypes.NameIdentifier, user) }, "JWT"));
            var nextCalled = false;
            var guard = new UnscopedCommercialTenantGuardMiddleware(context =>
            {
                nextCalled = true;
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            });
            await guard.InvokeAsync(http, db);
            return (http.Response.StatusCode, nextCalled,
                VerifiedCommercialTenant.Resolve(http));
        }

        foreach (var route in new[] { "/clientes/9", "/proveedores/9" })
        {
            foreach (var (user, own, foreign) in new[]
            {
                ("a", a.Id, b.Id), ("b", b.Id, a.Id)
            })
            {
                var accepted = await RequestAsync(route, "PUT", own, user);
                Assert.Equal(200, accepted.Code);
                Assert.True(accepted.Next);
                Assert.Equal(own, accepted.Owner);

                var cross = await RequestAsync(route, "PUT", foreign, user);
                Assert.Equal(503, cross.Code);
                Assert.False(cross.Next);
                Assert.Null(cross.Owner);

                var disabled = await RequestAsync(route, "PUT", own, user, enabled: false);
                Assert.Equal(503, disabled.Code);
                Assert.False(disabled.Next);

                var viewer = await RequestAsync(route, "PUT", own, user, allowEdit: false);
                Assert.Equal(503, viewer.Code);
                Assert.False(viewer.Next);
            }
            Assert.Equal(503, (await RequestAsync(route, "PUT", a.Id, "")).Code);
            Assert.Equal(503, (await RequestAsync(route, "PUT", a.Id, "a",
                contentType: "multipart/form-data")).Code);
            Assert.Equal(503, (await RequestAsync(route, "PUT", a.Id, "a",
                length: 0)).Code);
            Assert.Equal(503, (await RequestAsync(route, "PUT", a.Id, "a",
                length: 33 * 1024)).Code);
        }

        foreach (var (path, method) in new[]
        {
            ("/clientes", "PUT"), ("/clientes/0", "PUT"),
            ("/clientes/-1", "PUT"), ("/clientes/9/activar", "PUT"),
            ("/clientes/9", "PATCH"), ("/clientes/9", "DELETE"),
            ("/proveedores/0", "PUT"), ("/proveedores/9/otro", "PUT"),
            ("/proveedores/9", "POST"), ("/ventas/9", "PUT"),
            ("/productos/9", "PUT")
        })
        {
            var rejected = await RequestAsync(path, method, a.Id, "a");
            Assert.Equal(503, rejected.Code);
            Assert.False(rejected.Next);
            Assert.Null(rejected.Owner);
        }
    }
}
