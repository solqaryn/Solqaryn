using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Solqaryn.API.Security;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// C11-03.M03.03 negative regression checks of real-runtime adapter boundaries.
/// They do not substitute a signed-JWT/AppDbContext/MySQL84 E2E acceptance.
/// </summary>
public sealed class C1103M0303VerifiedMembershipReaderTests
{
    private static (
        C1103OutboxVerifiedMembershipReader Reader,
        Mock<IUsuarioScopeService> Scope,
        Mock<IPermisoService> Permission,
        DefaultHttpContext Http) Create(bool enabled = true)
    {
        var http = new DefaultHttpContext();
        http.Request.RouteValues["tenantId"] = "811";
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("id", "911") }, "synthetic"));
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(911);
        var scope = new Mock<IUsuarioScopeService>();
        var permission = new Mock<IPermisoService>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["OutboxPlanning:EnableReadOnlyAuthorizationProbe"] =
                    enabled ? "true" : "false"
            }).Build();
        var reader = new C1103OutboxVerifiedMembershipReader(
            new HttpContextAccessor { HttpContext = http }, current.Object,
            scope.Object, permission.Object, configuration);
        return (reader, scope, permission, http);
    }

    [Fact]
    public async Task DefaultOffNeverAsksRoleOrMembership()
    {
        var (reader, scope, permissions, _) = Create(false);
        Assert.Null(await reader.ReadCurrentAsync(911, 811, CancellationToken.None));
        scope.VerifyNoOtherCalls();
        permissions.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(911, 812, "811")]
    [InlineData(911, 811, "812")]
    [InlineData(911, 811, "untrusted")]
    [InlineData(0, 811, "811")]
    public async Task TenantMismatchOrInvalidActorDeniesBeforeDatabase(
        int actor, int tenant, string route)
    {
        var (reader, scope, permissions, http) = Create();
        http.Request.RouteValues["tenantId"] = route;
        Assert.Null(await reader.ReadCurrentAsync(actor, tenant, CancellationToken.None));
        scope.VerifyNoOtherCalls();
        permissions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ForgedPrincipalAndClientTenantHeaderCannotReplaceBearerSignature()
    {
        var (reader, scope, permissions, http) = Create();
        http.Request.Headers["X-Empresa-Id"] = "811";
        // No registered, successful Bearer handler: synthetic auth must fail.
        Assert.Null(await reader.ReadCurrentAsync(911, 811, CancellationToken.None));
        scope.VerifyNoOtherCalls();
        permissions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancelledProbeNeverReturnsPermission()
    {
        var (reader, scope, permissions, _) = Create();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await reader.ReadCurrentAsync(911, 811, cts.Token));
        scope.VerifyNoOtherCalls();
        permissions.VerifyNoOtherCalls();
    }
}
