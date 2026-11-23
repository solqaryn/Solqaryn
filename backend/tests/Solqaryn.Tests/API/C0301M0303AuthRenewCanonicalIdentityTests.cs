using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// C03-01.M03.03: production refresh must never pick the first of ambiguous
/// actor claims. Pure controller tests; JWT signature, MySQL and tenant A/B
/// are verified by the separate genuine HTTP + Oracle MySQL 8.4 workflow tests.
/// </summary>
public sealed class C0301M0303AuthRenewCanonicalIdentityTests
{
    private static AuthController Subject(
        Mock<IAuthService> auth, bool authenticated, params string[] actorClaims)
    {
        var claims = actorClaims.Select(id => new Claim("id", id)).ToArray();
        var identity = new ClaimsIdentity(claims,
            authenticated ? "test-controller-authentication-only" : null);
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        http.Request.Headers["X-Empresa-Id"] = "9999";
        http.Request.Headers["X-Rol-Id"] = "1";
        return new AuthController(auth.Object, Mock.Of<IAuditoriaService>())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-805")]
    [InlineData("+805")]
    [InlineData("0805")]
    [InlineData(" 805")]
    [InlineData("805 ")]
    [InlineData("2147483648")]
    [InlineData("invalid")]
    public async Task RefusesNoncanonicalOrNonpositiveClaimBeforeCallingService(
        string signedLikeClaim)
    {
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        var action = await Subject(auth, authenticated: true, signedLikeClaim).Renovar();

        Assert.IsType<UnauthorizedObjectResult>(action);
        auth.Verify(x => x.RenovarAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RefusesMissingClaim()
    {
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        var action = await Subject(auth, authenticated: true).Renovar();

        Assert.IsType<UnauthorizedObjectResult>(action);
        auth.Verify(x => x.RenovarAsync(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData("805", "806")]
    [InlineData("805", "805")]
    public async Task RefusesDuplicateActorClaimsEvenWhenFirstIdLooksValid(
        string first, string second)
    {
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        var action = await Subject(auth, authenticated: true, first, second).Renovar();

        Assert.IsType<UnauthorizedObjectResult>(action);
        auth.Verify(x => x.RenovarAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RefusesUnauthenticatedPrincipalWithValidId()
    {
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        var action = await Subject(auth, authenticated: false, "805").Renovar();

        Assert.IsType<UnauthorizedObjectResult>(action);
        auth.Verify(x => x.RenovarAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SingleCanonicalActorIdStillCallsServiceExactlyOnce()
    {
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        auth.Setup(x => x.RenovarAsync(805)).ReturnsAsync((LoginResponseDto?)null);

        var action = await Subject(auth, authenticated: true, "805").Renovar();

        // The service can reject a revoked session. The important positive
        // assertion is that a canonical actor reaches the actual service.
        Assert.IsType<UnauthorizedObjectResult>(action);
        auth.Verify(x => x.RenovarAsync(805), Times.Once);
        auth.VerifyNoOtherCalls();
    }
}
