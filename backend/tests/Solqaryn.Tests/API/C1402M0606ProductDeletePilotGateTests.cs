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
/// C14-02.M06.06: the standalone product DELETE flag must reach the real
/// commercial tenant guard; other pilot flags must remain off.
/// This guard test does not replace signed-JWT HTTP or Oracle MySQL integration.
/// </summary>
public sealed class C1402M0606ProductDeletePilotGateTests
{
    [Fact]
    public async Task DeleteWithDedicatedFlagRequiresRealTenantPermissionAndExactRoute()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("c1402-delete-gate-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);
        var empresaA = new Empresa("C14 Delete A");
        var empresaB = new Empresa("C14 Delete B");
        db.Set<Empresa>().AddRange(empresaA, empresaB, new Empresa("C14 Delete C"));
        await db.SaveChangesAsync();

        async Task<(int Status, bool Reached, int? Authorized)> SendAsync(
            string user, int empresaId, string path = "/productos/101",
            string method = "DELETE", bool enabled = true, bool permit = true)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivateProductDeletePilotEnabled"] = enabled.ToString(),
                    ["MultiEmpresa:PrivateCommercialFilesPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateReadPilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateWritePilotEnabled"] = "false",
                    ["MultiEmpresa:PrivateDraftPilotEnabled"] = "false"
                }).Build();
            var grants = new Mock<IPermisoService>(MockBehavior.Strict);
            grants.Setup(x => x.TienePermisoAsync(
                It.IsAny<int>(), It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
                .Returns((int id, ModuloSistema module, AccionPermiso action) =>
                    Task.FromResult(permit && id == empresaId &&
                        ((user == "A" && id == empresaA.Id) ||
                         (user == "B" && id == empresaB.Id)) &&
                        module == ModuloSistema.Productos &&
                        action == AccionPermiso.EliminarLogico));
            using var provider = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddSingleton<IPermisoService>(grants.Object)
                .BuildServiceProvider();
            var context = new DefaultHttpContext { RequestServices = provider };
            context.Request.Path = path;
            context.Request.Method = method;
            context.Request.Headers["X-Empresa-Id"] = empresaId.ToString();
            context.Response.Body = new MemoryStream();
            context.User = user.Length == 0
                ? new ClaimsPrincipal(new ClaimsIdentity())
                : new ClaimsPrincipal(new ClaimsIdentity(new[]
                  { new Claim(ClaimTypes.NameIdentifier, user) }, "JWT"));
            var reached = false;
            var guard = new UnscopedCommercialTenantGuardMiddleware(http =>
            {
                reached = true;
                http.Response.StatusCode = 200;
                return Task.CompletedTask;
            });
            await guard.InvokeAsync(context, db);
            return (context.Response.StatusCode, reached,
                VerifiedCommercialTenant.Resolve(context));
        }

        foreach (var (user, id, foreign) in new[]
        {
            ("A", empresaA.Id, empresaB.Id),
            ("B", empresaB.Id, empresaA.Id)
        })
        {
            var allowed = await SendAsync(user, id);
            Assert.Equal(200, allowed.Status);
            Assert.True(allowed.Reached);
            Assert.Equal(id, allowed.Authorized);

            var off = await SendAsync(user, id, enabled: false);
            Assert.Equal(503, off.Status);
            Assert.False(off.Reached);
            Assert.Null(off.Authorized);

            var cross = await SendAsync(user, foreign);
            Assert.Equal(503, cross.Status);
            Assert.False(cross.Reached);
            Assert.Null(cross.Authorized);

            var revoked = await SendAsync(user, id, permit: false);
            Assert.Equal(503, revoked.Status);
            Assert.False(revoked.Reached);
            Assert.Null(revoked.Authorized);
        }
        foreach (var (method, path) in new[]
        {
            ("DELETE", "/productos"),
            ("DELETE", "/productos/0"),
            ("DELETE", "/productos/-1"),
            ("DELETE", "/productos/101/variantes"),
            ("DELETE", "/productos/101/imagenes/1"),
            ("POST", "/productos/101"),
            ("PUT", "/productos/101")
        })
        {
            var denied = await SendAsync("A", empresaA.Id, path, method);
            Assert.Equal(503, denied.Status);
            Assert.False(denied.Reached);
            Assert.Null(denied.Authorized);
        }
        var anonymous = await SendAsync("", empresaA.Id);
        Assert.Equal(503, anonymous.Status);
        Assert.False(anonymous.Reached);
    }
}
