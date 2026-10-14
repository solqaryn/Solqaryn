using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class MetodosPagoTenantMembershipGuardTests
{
    [Theory]
    [InlineData(nameof(MetodosPagoController.GetActivos))]
    [InlineData(nameof(MetodosPagoController.GetBancosActivos))]
    public void LecturasAuxiliares_DeclaranGateDeMembresia(string accion)
    {
        var method = typeof(MetodosPagoController).GetMethod(accion,
            BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<RequiereMembresiaEmpresaAttribute>());
    }

    [Fact]
    public async Task MembresiaA_AutorizaSoloA_SinConcederHeaderFalsoNiSesionAnonima()
    {
        var scopes = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var activoA = new UsuarioTenantScopeActual(
            UsuarioId: 7, EmpresaId: 11, RolId: 4,
            RolNombre: "Operador A", EsAdministrador: false);
        scopes.Setup(x => x.ObtenerActualAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activoA);
        scopes.Setup(x => x.ObtenerActualAsync(22, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);
        scopes.Setup(x => x.ObtenerUnicoActualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);

        var filtro = new RequiereMembresiaEmpresaAttribute();
        var permitido = Contexto(scopes.Object, 11);
        var ejecutado = false;
        await filtro.OnActionExecutionAsync(permitido, () =>
        {
            ejecutado = true;
            return Task.FromResult(new ActionExecutedContext(
                permitido, new List<IFilterMetadata>(), permitido.Controller));
        });
        Assert.True(ejecutado);
        Assert.Equal(11, VerifiedCommercialTenant.Resolve(permitido.HttpContext));

        var otro = Contexto(scopes.Object, 22);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            filtro.OnActionExecutionAsync(otro, Next(otro)));
        Assert.Null(VerifiedCommercialTenant.Resolve(otro.HttpContext));

        var sinEleccion = Contexto(scopes.Object, null);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            filtro.OnActionExecutionAsync(sinEleccion, Next(sinEleccion)));
        Assert.Null(VerifiedCommercialTenant.Resolve(sinEleccion.HttpContext));

        var anonimo = Contexto(scopes.Object, 11, autenticado: false);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            filtro.OnActionExecutionAsync(anonimo, Next(anonimo)));
        Assert.Null(VerifiedCommercialTenant.Resolve(anonimo.HttpContext));

        scopes.Verify(x => x.ObtenerActualAsync(11, It.IsAny<CancellationToken>()),
            Times.Once());
        scopes.Verify(x => x.ObtenerActualAsync(22, It.IsAny<CancellationToken>()),
            Times.Once());
    }

    private static ActionExecutingContext Contexto(
        IUsuarioScopeService scope, int? empresaId, bool autenticado = true)
    {
        var services = new ServiceCollection()
            .AddSingleton<IUsuarioScopeService>(scope)
            .BuildServiceProvider();
        var http = new DefaultHttpContext
        {
            RequestServices = services,
            User = autenticado
                ? new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "7") }, "TestJwt"))
                : new ClaimsPrincipal(new ClaimsIdentity())
        };
        if (empresaId.HasValue)
            http.Request.Headers["X-Empresa-Id"] = empresaId.Value.ToString();

        return new ActionExecutingContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    private static ActionExecutionDelegate Next(ActionExecutingContext context) =>
        () => Task.FromResult(new ActionExecutedContext(
            context, new List<IFilterMetadata>(), context.Controller));
}
