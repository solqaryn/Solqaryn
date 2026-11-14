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
/// El piloto de confirmación no puede ampliar rutas/métodos ni conceder
/// acciones de borrador por accidente cuando dos flags están activos.
/// La autorización física del documento se verifica en MySQL/HTTP aparte.
/// </summary>
public sealed class CommercialConfirmPilotGuardTests
{
    [Fact]
    public async Task Confirmar_AB_Solo_PositiveId_RBAC_Confirmar_Y_Flag_Explicito()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-confirm-guard-" + Guid.NewGuid().ToString("N")).Options;
        await using var db = new AppDbContext(options);
        var a = new Empresa("Confirmación A");
        var b = new Empresa("Confirmación B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        async Task<(int Status, bool Next, int? Owner)> CallAsync(
            string route, int empresa, string user, bool confirmFlag = true,
            bool draftFlag = true, string method = "POST")
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivateConfirmPilotEnabled"] = confirmFlag.ToString(),
                    ["MultiEmpresa:PrivateDraftPilotEnabled"] = draftFlag.ToString(),
                    ["MultiEmpresa:PrivateWritePilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateReadPilotEnabled"] = "false"
                }).Build();
            var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
            permisos.Setup(x => x.TienePermisoAsync(
                It.IsAny<int>(), It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
                .Returns((int id, ModuloSistema module, AccionPermiso action) =>
                    Task.FromResult(id == a.Id && user == "a" &&
                                    action == AccionPermiso.Confirmar &&
                                    (module == ModuloSistema.Ventas ||
                                     module == ModuloSistema.Compras)));

            using var svc = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddSingleton<IPermisoService>(permisos.Object)
                .BuildServiceProvider();
            var ctx = new DefaultHttpContext { RequestServices = svc };
            ctx.Request.Path = route;
            ctx.Request.Method = method;
            ctx.Request.Headers["X-Empresa-Id"] = empresa.ToString();
            ctx.Response.Body = new MemoryStream();
            ctx.User = user.Length == 0
                ? new ClaimsPrincipal(new ClaimsIdentity())
                : new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user)
                }, "JWT-test"));
            // Confirmación no requiere cuerpo; no hereda límite de Draft.
            ctx.Request.ContentLength = 0;
            bool next = false;
            var guard = new UnscopedCommercialTenantGuardMiddleware(nextCtx =>
            {
                next = true;
                nextCtx.Response.StatusCode = 200;
                return Task.CompletedTask;
            });
            await guard.InvokeAsync(ctx, db);
            return (ctx.Response.StatusCode, next, VerifiedCommercialTenant.Resolve(ctx));
        }

        foreach (var route in new[] { "/ventas/1/confirmar", "/compras/28/confirmar" })
        {
            var granted = await CallAsync(route, a.Id, "a");
            Assert.Equal(200, granted.Status);
            Assert.True(granted.Next);
            Assert.Equal(a.Id, granted.Owner);

            var forbiddenMember = await CallAsync(route, b.Id, "a");
            Assert.Equal(503, forbiddenMember.Status);
            Assert.Null(forbiddenMember.Owner);

            var disabled = await CallAsync(route, a.Id, "a", confirmFlag: false);
            Assert.Equal(503, disabled.Status);
            Assert.False(disabled.Next);

            var anonymous = await CallAsync(route, a.Id, "");
            Assert.Equal(503, anonymous.Status);
            Assert.Null(anonymous.Owner);
        }

        foreach (var route in new[]
        {
            "/ventas/0/confirmar", "/ventas/-1/confirmar", "/ventas/1/confirmar/",
            "/ventas/1/confirmar/pdf", "/ventas/1/anular",
            "/compras/1/anular", "/compras/1/documentos",
            "/facturas/1/pagos", "/ventas/abcd/confirmar",
            "/compras/1/confirmar-todo", "/inventario/movimientos"
        })
        {
            var result = await CallAsync(route, a.Id, "a");
            Assert.Equal(503, result.Status);
            Assert.False(result.Next);
            Assert.Null(result.Owner);
        }

        var wrongMethod = await CallAsync("/ventas/1/confirmar", a.Id, "a",
            method: "PUT");
        Assert.Equal(503, wrongMethod.Status);
        Assert.Null(wrongMethod.Owner);
    }
}
