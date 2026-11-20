using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Solqaryn.API.Security;
using Solqaryn.Application.Interfaces;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// C04-04.M03.03 adversarial checks on the new runtime binding.
/// These are negative DI tests, NOT full real MySQL+JWT+RBAC acceptance.
/// The separate MySQL TestServer fixture is also insufficient until combined.
/// </summary>
public sealed class C0404M0303VerifiedReaderTests
{
    private static (
        C0404PaymentVerifiedEvidenceReader Reader,
        Mock<IUsuarioScopeService> Scope,
        Mock<IPermisoService> Perm,
        AppDbContext Db,
        DefaultHttpContext Http) Create(bool enabled = true)
    {
        var http = new DefaultHttpContext();
        http.Request.RouteValues["tenantId"] = "401";
        http.Request.RouteValues["invoiceId"] = "901";
        var context = new HttpContextAccessor { HttpContext = http };
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(701);
        var scope = new Mock<IUsuarioScopeService>();
        var permission = new Mock<IPermisoService>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PaymentPlanning:EnableReadOnlyAuthorizationProbe"] =
                    enabled ? "true" : "false"
            }).Build();
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        var reader = new C0404PaymentVerifiedEvidenceReader(
            context, current.Object, scope.Object, permission.Object, db, config);
        return (reader, scope, permission, db, http);
    }

    [Fact]
    public async Task DisabledByDefaultNeverQueriesRbacOrInvoices()
    {
        var (reader, scope, perm, db, http) = Create(enabled: false);
        using (db)
        {
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("id", "701") }, "forged"));
            Assert.Null(await reader.ReadCurrentAsync(CancellationToken.None));
            scope.VerifyNoOtherCalls();
            perm.VerifyNoOtherCalls();
        }
    }

    [Fact]
    public async Task ForgedPrincipalAndClientTenantHeaderDoNotReplaceSignedJwt()
    {
        var (reader, scope, perm, db, http) = Create();
        using (db)
        {
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("id", "701"), new Claim("rolId", "1") }, "forged"));
            http.Request.Headers["X-Empresa-Id"] = "401";
            // No registered Bearer authentication handler: fail closed.
            Assert.Null(await reader.ReadCurrentAsync(CancellationToken.None));
            scope.VerifyNoOtherCalls();
            perm.VerifyNoOtherCalls();
        }
    }

    [Theory]
    [InlineData("0", "901")]
    [InlineData("401", "-1")]
    [InlineData("untrusted", "901")]
    public async Task InvalidRouteScopeDeniedBeforeAccess(string tenant, string invoice)
    {
        var (reader, scope, perm, db, http) = Create();
        using (db)
        {
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("id", "701") }, "forged"));
            http.Request.RouteValues["tenantId"] = tenant;
            http.Request.RouteValues["invoiceId"] = invoice;
            Assert.Null(await reader.ReadCurrentAsync(CancellationToken.None));
            scope.VerifyNoOtherCalls();
            perm.VerifyNoOtherCalls();
        }
    }

    [Fact]
    public async Task CancellationNeverBecomesPaymentAuthority()
    {
        var (reader, scope, perm, db, _) = Create();
        using (db)
        using (var canceled = new CancellationTokenSource())
        {
            canceled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await reader.ReadCurrentAsync(canceled.Token));
            scope.VerifyNoOtherCalls();
            perm.VerifyNoOtherCalls();
        }
    }
}
