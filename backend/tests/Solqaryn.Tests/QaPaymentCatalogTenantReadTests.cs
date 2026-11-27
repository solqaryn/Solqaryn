using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class QaPaymentCatalogTenantReadTests
{
    private static readonly UsuarioTenantScopeActual Membership =
        new(7, 31, 5, "Operador", false);

    private static ActionExecutingContext Context(IServiceProvider services, string? header,
        string method = "GET", bool authenticated = true)
    {
        var http = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(authenticated
                ? new ClaimsIdentity(new[] { new Claim("id", "7") }, "qa")
                : new ClaimsIdentity())
        };
        http.Request.Method = method;
        if (header is not null)
            http.Request.Headers["X-Empresa-Id"] = header;
        return new ActionExecutingContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
    }

    private static ActionExecutedContext Executed(ActionExecutingContext context) =>
        new(context, new List<IFilterMetadata>(), context.Controller);

    [Theory]
    [InlineData(nameof(MetodosPagoController.GetActivos))]
    [InlineData(nameof(MetodosPagoController.GetBancosActivos))]
    public void OperativeLookupsRequireVerifiedReadMembership(string action)
    {
        var method = typeof(MetodosPagoController).GetMethod(action);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<RequiereMembresiaEmpresaLecturaAttribute>());
        Assert.NotNull(typeof(MetodosPagoController).GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>());
    }

    [Theory]
    [InlineData("31")]
    [InlineData(null)]
    public async Task ValidMembershipAuthorizesOnlyRead(string? header)
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        if (header is null)
            scope.Setup(x => x.ObtenerUnicoActualAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Membership);
        else
            scope.Setup(x => x.ObtenerActualAsync(31, It.IsAny<CancellationToken>())).ReturnsAsync(Membership);
        using var services = new ServiceCollection().AddSingleton(scope.Object).BuildServiceProvider();
        var context = Context(services, header);
        var accessor = new HttpContextAccessor { HttpContext = context.HttpContext };
        var filter = new RequiereMembresiaEmpresaLecturaAttribute();
        var invoked = false;
        try
        {
            await filter.OnActionExecutionAsync(context, () =>
            {
                invoked = true;
                using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().Options,
                    httpContextAccessor: accessor);
                Assert.Equal(31, db.CommercialReadEmpresaId);
                Assert.Null(db.AuthorizedEmpresaId);
                Assert.Null(db.CommercialWriteEmpresaId);
                Assert.False(db.CommercialPrivateWriteAuthorized);
                return Task.FromResult(Executed(context));
            });
            Assert.True(invoked);
            Assert.False(context.HttpContext.Items.ContainsKey(AppDbContext.AuthorizedCommercialReadEmpresaItemKey));
            scope.VerifyAll();
        }
        finally { accessor.HttpContext = null; }
    }

    [Theory]
    [InlineData("99")]
    [InlineData(null)]
    public async Task MissingOrForeignMembershipRejectsBeforeLookup(string? header)
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        if (header is null)
            scope.Setup(x => x.ObtenerUnicoActualAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((UsuarioTenantScopeActual?)null);
        else
            scope.Setup(x => x.ObtenerActualAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UsuarioTenantScopeActual?)null);
        using var services = new ServiceCollection().AddSingleton(scope.Object).BuildServiceProvider();
        var context = Context(services, header);
        var invoked = false;
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new RequiereMembresiaEmpresaLecturaAttribute().OnActionExecutionAsync(context, () =>
            {
                invoked = true;
                return Task.FromResult(Executed(context));
            }));
        Assert.False(invoked);
        Assert.False(context.HttpContext.Items.ContainsKey(AppDbContext.AuthorizedCommercialReadEmpresaItemKey));
        scope.VerifyAll();
    }

    [Theory]
    [InlineData("0", "GET", true)]
    [InlineData("invalid", "GET", true)]
    [InlineData("31", "POST", true)]
    [InlineData("31", "GET", false)]
    public async Task InvalidRequestNeverObtainsReadAuthority(string header, string method, bool authenticated)
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        using var services = new ServiceCollection().AddSingleton(scope.Object).BuildServiceProvider();
        var context = Context(services, header, method, authenticated);
        var invoked = false;
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new RequiereMembresiaEmpresaLecturaAttribute().OnActionExecutionAsync(context, () =>
            {
                invoked = true;
                return Task.FromResult(Executed(context));
            }));
        Assert.False(invoked);
        Assert.False(context.HttpContext.Items.ContainsKey(AppDbContext.AuthorizedCommercialReadEmpresaItemKey));
        scope.VerifyNoOtherCalls();
    }
}
