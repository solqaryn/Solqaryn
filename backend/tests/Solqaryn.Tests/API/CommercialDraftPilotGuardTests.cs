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
/// Borradores multiempresa: sólo rutas POST raíz, body pequeño y RBAC Crear.
/// No certifica confirmación, stock, factura, pagos, archivos ni Cloudinary.
/// </summary>
public sealed class CommercialDraftPilotGuardTests
{
    [Fact]
    public async Task PilotoBorradores_AB_Flag_Crear_Formato_Y_Archivos_FallanCerrados()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-draft-guard-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);
        var a = new Empresa("A Borrador");
        var b = new Empresa("B Borrador");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var configs = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MultiEmpresa:PrivateDraftPilotEnabled"] = "true",
                ["MultiEmpresa:PrivateWritePilotEnabled"] = "false",
                ["MultiEmpresa:PrivateReadPilotEnabled"] = "false"
            }).Build();
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(x => x.TienePermisoAsync(
                It.IsAny<int>(), It.IsAny<ModuloSistema>(), AccionPermiso.Crear))
            .Returns((int empresa, ModuloSistema modulo, AccionPermiso _) =>
                Task.FromResult(empresa == a.Id &&
                                (modulo == ModuloSistema.Productos ||
                                 modulo == ModuloSistema.Ventas ||
                                 modulo == ModuloSistema.Compras)));
        async Task<(int Status, bool Next, int? Owner)> TryAsync(
            string ruta, int empresa, string usuario, bool form = false,
            bool withFile = false, bool flag = true, bool badBody = false)
        {
            var config = flag ? configs : new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivateDraftPilotEnabled"] = "false"
                }).Build();
            using var perRequest = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddSingleton<IPermisoService>(permisos.Object)
                .BuildServiceProvider();
            var ctx = new DefaultHttpContext { RequestServices = perRequest };
            ctx.Request.Path = ruta;
            ctx.Request.Method = "POST";
            ctx.Request.Headers["X-Empresa-Id"] = empresa.ToString();
            ctx.Request.ContentLength = badBody ? 0 : 120;
            ctx.Request.ContentType = form
                ? "multipart/form-data; boundary=boundary"
                : "application/json; charset=utf-8";
            ctx.Response.Body = new MemoryStream();
            ctx.User = usuario.Length > 0
                ? new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario)
                }, "TestJWT"))
                : new ClaimsPrincipal(new ClaimsIdentity());

            if (form)
            {
                var files = new FormFileCollection();
                if (withFile)
                    files.Add(new FormFile(new MemoryStream(new byte[] { 1 }), 0, 1,
                        "Imagenes", "a.jpg"));
                ctx.Features.Set<IFormFeature>(new FormFeature(
                    new FormCollection(new Dictionary<string, StringValues>
                    {
                        ["Nombre"] = "Producto A sin archivos"
                    }, files)));
            }

            var called = false;
            var middleware = new UnscopedCommercialTenantGuardMiddleware(request =>
            {
                called = true;
                Assert.Equal(a.Id, VerifiedCommercialTenant.Resolve(request));
                request.Response.StatusCode = StatusCodes.Status201Created;
                return Task.CompletedTask;
            });
            await middleware.InvokeAsync(ctx, db);
            return (ctx.Response.StatusCode, called, VerifiedCommercialTenant.Resolve(ctx));
        }

        foreach (var route in new[] { "/productos", "/ventas", "/compras" })
        {
            var allowed = await TryAsync(route, a.Id, "user-a", form: route == "/productos");
            Assert.Equal(201, allowed.Status);
            Assert.True(allowed.Next);
            Assert.Equal(a.Id, allowed.Owner);
            var cross = await TryAsync(route, b.Id, "user-a", form: route == "/productos");
            Assert.Equal(503, cross.Status);
            Assert.False(cross.Next);
            Assert.Null(cross.Owner);
            var off = await TryAsync(route, a.Id, "user-a", form: route == "/productos",
                flag: false);
            Assert.Equal(503, off.Status);
            Assert.Null(off.Owner);
        }

        foreach (var blocked in new[]
        {
            await TryAsync("/productos", a.Id, "user-a", form: true, withFile: true),
            await TryAsync("/productos", a.Id, "user-a", form: false),
            await TryAsync("/ventas", a.Id, "user-a", form: true),
            await TryAsync("/compras", a.Id, "user-a", badBody: true),
            await TryAsync("/ventas/1/confirmar", a.Id, "user-a"),
            await TryAsync("/compras/1/anular", a.Id, "user-a"),
            await TryAsync("/facturas/1/pagos", a.Id, "user-a"),
            await TryAsync("/productos/1/imagenes", a.Id, "user-a"),
            await TryAsync("/ventas", a.Id, "")
        })
        {
            Assert.Equal(503, blocked.Status);
            Assert.False(blocked.Next);
            Assert.Null(blocked.Owner);
        }
    }
}
