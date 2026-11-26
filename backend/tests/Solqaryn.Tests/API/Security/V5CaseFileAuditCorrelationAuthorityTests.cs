using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.API.Security;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests.API.Security;

public sealed class V5CaseFileAuditCorrelationAuthorityTests
{
    [Theory]
    [InlineData(11, 21)]
    [InlineData(12, 22)]
    public async Task ExactScopedGrantUsesOnlySignedContext(int tenant, int branch)
    {
        var (http, grants, service) = Build(tenant, branch);
        grants.Setup(p => p.GetMisPermisosContextoAsync(7, 19, tenant, branch, 23,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Grant("Auditoria:ReadAuditCorrelation"));
        var actual = Assert.IsType<V5CaseFileAuditCorrelationAuthority.Session>(
            await service.ResolveAsync());
        Assert.Equal(tenant, actual.TenantId);
        Assert.Equal(branch, actual.BranchId);
        grants.Verify(p => p.GetMisPermisosContextoAsync(7, 19, tenant, branch, 23,
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        http.Request.Headers["X-Empresa-Id"] = (tenant + 1).ToString();
        Assert.False(await service.RevalidateAsync(actual));
    }

    [Theory]
    [InlineData("Auditoria:Ver")]
    [InlineData("OtroModulo:ReadAuditCorrelation")]
    public async Task GenericOrOtherGrantDoesNotAuthorize(string grant)
    {
        var (_, grants, service) = Build();
        grants.Setup(p => p.GetMisPermisosContextoAsync(7, 19, 11, 21, 23,
                It.IsAny<CancellationToken>())).ReturnsAsync(Grant(grant));
        Assert.Null(await service.ResolveAsync());
    }

    [Fact]
    public async Task RevocationDuringGrantIoFailsClosed()
    {
        var (_, grants, service) = Build();
        grants.SetupSequence(p => p.GetMisPermisosContextoAsync(7, 19, 11, 21, 23,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Grant("Auditoria:ReadAuditCorrelation"))
            .ReturnsAsync((MisPermisosDto?)null);
        Assert.Null(await service.ResolveAsync());
    }

    [Theory]
    [InlineData("Bearer", false, false)]
    [InlineData("TenantContextBearer", true, false)]
    [InlineData("TenantContextBearer", false, true)]
    public async Task WrongSchemeDuplicateClaimOrBranchOverrideDenied(
        string ticketScheme, bool duplicate, bool wrongBranch)
    {
        var (http, grants, service) = Build(ticketScheme: ticketScheme);
        if (duplicate) ((ClaimsIdentity)http.User.Identity!).AddClaim(new Claim("tenantId", "12"));
        if (wrongBranch) http.Request.Headers["X-Sucursal-Id"] = "22";
        Assert.Null(await service.ResolveAsync());
        grants.VerifyNoOtherCalls();
    }

    private static MisPermisosDto Grant(string value) => new()
    {
        Rol = "Fixture", EsAdministrador = true, Permisos = new List<string> { value }
    };

    private static (DefaultHttpContext, Mock<IPermisoService>,
        V5CaseFileAuditCorrelationAuthority) Build(int tenant = 11, int branch = 21,
            string ticketScheme = "TenantContextBearer")
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("id", "7"), new Claim("rolId", "19"),
            new Claim("tenantId", tenant.ToString(CultureInfo.InvariantCulture)),
            new Claim("branchId", branch.ToString(CultureInfo.InvariantCulture)),
            new Claim("tenantRoleId", "23"),
            new Claim("exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
                .ToString(CultureInfo.InvariantCulture))
        }, "Fixture");
        var principal = new ClaimsPrincipal(identity);
        var http = new DefaultHttpContext { User = principal };
        var auth = new Mock<IAuthenticationService>(MockBehavior.Strict);
        auth.Setup(a => a.AuthenticateAsync(http, TenantSessionToken.Scheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(principal, ticketScheme)));
        http.RequestServices = new ServiceCollection()
            .AddSingleton(auth.Object).BuildServiceProvider();
        var grants = new Mock<IPermisoService>(MockBehavior.Strict);
        return (http, grants, new V5CaseFileAuditCorrelationAuthority(
            new HttpContextAccessor { HttpContext = http }, grants.Object));
    }
}
