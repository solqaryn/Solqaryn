using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using Solqaryn.API.Filters;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// V5 C00-01.M03.03: seleccionar tenant no autoriza; la membresía viva
/// debe validar el ID y ninguna cabecera inválida puede invocar fallback.
/// </summary>
public sealed class V5C0001M0303TenantHeaderFailClosedTests
{
    [Fact]
    public async Task SelectorUnicoYAutorizado_PermiteLaMembresiaA()
    {
        Assert.True(await EjecutarAsync(["17"]));
    }

    [Fact]
    public async Task SinSelector_PermiteFallbackSoloConMembresiaUnicaAutorizada()
    {
        Assert.True(await EjecutarAsync(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("17,18")]
    [InlineData("999999999999999999999")]
    public async Task SelectorExplicitoInvalido_NoSeConvierteEnMembresiaUnica(string raw)
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () =>
        {
            await EjecutarAsync([raw]);
        });
    }

    [Theory]
    [InlineData("17", "18")]
    [InlineData("18", "17")]
    [InlineData("17", "17")]
    [InlineData("18", "18")]
    public async Task ValoresMultiples_InclusoDuplicados_NoSeAceptaSelectorAmbiguo(
        string primero, string segundo)
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () =>
        {
            await EjecutarAsync([primero, segundo]);
        });
    }

    [Fact]
    public async Task SelectorEmpresaB_SinMembresiaB_FallaCerrado()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () =>
        {
            await EjecutarAsync(["18"]);
        });
    }

    private static async Task<bool> EjecutarAsync(string[]? selector)
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var autorizado = new UsuarioTenantScopeActual(9, 17, 2, "Operador A", false);
        scope.Setup(s => s.ObtenerUnicoActualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(autorizado);
        scope.Setup(s => s.ObtenerActualAsync(17, It.IsAny<CancellationToken>()))
            .ReturnsAsync(autorizado);
        scope.Setup(s => s.ObtenerActualAsync(18, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);

        using var services = new ServiceCollection()
            .AddSingleton(scope.Object)
            .BuildServiceProvider();

        var http = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "9")], "testjwt"))
        };
        if (selector is not null)
            http.Request.Headers["X-Empresa-Id"] = new StringValues(selector);

        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var controller = new object();
        var executing = new ActionExecutingContext(actionContext, filters,
            new Dictionary<string, object?>(), controller);
        var nextWasCalled = false;
        await new RequiereMembresiaEmpresaAttribute().OnActionExecutionAsync(executing, () =>
        {
            nextWasCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, filters, controller));
        });
        return nextWasCalled;
    }
}
