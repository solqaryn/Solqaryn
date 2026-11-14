using System.Reflection;
using System.Security.Claims;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ConciliacionBancariaControllerTests
{
    private static void AssertRequierePermiso(MethodInfo method, ModuloSistema modulo, AccionPermiso accion)
    {
        var attr = Assert.Single(method.CustomAttributes.Where(a => a.AttributeType == typeof(RequierePermisoAttribute)));
        
        Assert.Equal((int)modulo, Convert.ToInt32(attr.ConstructorArguments[0].Value));
        Assert.Equal((int)accion, Convert.ToInt32(attr.ConstructorArguments[1].Value));
    }
    
    private ConciliacionBancariaController CreateController(IConciliacionBancariaService service, int usuarioId = 1)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())
        }, "mock"));

        return new ConciliacionBancariaController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }

    [Fact]
    public void Controller_HasCorrectAttributesAndRoute()
    {
        var type = typeof(ConciliacionBancariaController);

        Assert.Single(type.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true));
        Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));

        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        Assert.Equal("conciliaciones-bancarias", route.Template);
    }

    [Fact]
    public void ImportarEstadoCuenta_HasCorrectRouteAndPermission()
    {
        var method = typeof(ConciliacionBancariaController).GetMethod(nameof(ConciliacionBancariaController.ImportarEstadoCuenta));
        Assert.NotNull(method);

        var httpPost = Assert.Single(method.GetCustomAttributes(inherit: true).OfType<HttpPostAttribute>());
        Assert.Equal("importaciones-estado-cuenta", httpPost.Template);

        AssertRequierePermiso(method, ModuloSistema.Finanzas, AccionPermiso.Importar);
    }

    [Fact]
    public async Task ImportarEstadoCuenta_ValidRequest_ReturnsOk()
    {
        var mockService = new Mock<IConciliacionBancariaService>();
        var dto = new ImportarEstadoCuentaRequestDto { CuentaBancariaId = 1, IdempotencyKey = "key1" };
        var responseDto = new ImportarEstadoCuentaResponseDto { CuentaBancariaId = 1, MovimientosImportados = 5 };

        mockService.Setup(s => s.ImportarEstadoCuentaAsync(dto, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateController(mockService.Object, 1);
        var result = await controller.ImportarEstadoCuenta(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(responseDto, okResult.Value);
    }
    
    [Fact]
    public async Task ImportarEstadoCuenta_UnauthorizedUser_ReturnsUnauthorized()
    {
        var mockService = new Mock<IConciliacionBancariaService>();
        var dto = new ImportarEstadoCuentaRequestDto();
        var controller = new ConciliacionBancariaController(mockService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            }
        };

        var result = await controller.ImportarEstadoCuenta(dto, CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void ConciliarMovimientos_HasCorrectRouteAndPermission()
    {
        var method = typeof(ConciliacionBancariaController).GetMethod(nameof(ConciliacionBancariaController.ConciliarMovimientos));
        Assert.NotNull(method);

        var httpPost = Assert.Single(method.GetCustomAttributes(inherit: true).OfType<HttpPostAttribute>());
        Assert.Equal("matches", httpPost.Template);

        AssertRequierePermiso(method, ModuloSistema.Finanzas, AccionPermiso.Crear);
    }

    [Fact]
    public async Task ConciliarMovimientos_ValidRequest_ReturnsOk()
    {
        var mockService = new Mock<IConciliacionBancariaService>();
        var dto = new ConciliarMovimientosRequestDto { CuentaBancariaId = 1, IdempotencyKey = "key2" };
        var responseDto = new ConciliarMovimientosResponseDto { MatchesExitosos = 2 };

        mockService.Setup(s => s.ConciliarMovimientosAsync(dto, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateController(mockService.Object, 2);
        var result = await controller.ConciliarMovimientos(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(responseDto, okResult.Value);
    }

    [Fact]
    public void SolicitarAjuste_HasCorrectRouteAndPermission()
    {
        var method = typeof(ConciliacionBancariaController).GetMethod(nameof(ConciliacionBancariaController.SolicitarAjuste));
        Assert.NotNull(method);

        var httpPost = Assert.Single(method.GetCustomAttributes(inherit: true).OfType<HttpPostAttribute>());
        Assert.Equal("ajustes", httpPost.Template);

        AssertRequierePermiso(method, ModuloSistema.Finanzas, AccionPermiso.Crear);
    }

    [Fact]
    public async Task SolicitarAjuste_ValidRequest_ReturnsOk()
    {
        var mockService = new Mock<IConciliacionBancariaService>();
        var dto = new SolicitarAjusteRequestDto { CuentaBancariaId = 1, IdempotencyKey = "key3" };
        var responseDto = new SolicitarAjusteResponseDto { AjustesSolicitados = 1 };

        mockService.Setup(s => s.SolicitarAjusteAsync(dto, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateController(mockService.Object, 3);
        var result = await controller.SolicitarAjuste(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(responseDto, okResult.Value);
    }

    [Fact]
    public void CerrarPeriodo_HasCorrectRouteAndPermission()
    {
        var method = typeof(ConciliacionBancariaController).GetMethod(nameof(ConciliacionBancariaController.CerrarPeriodo));
        Assert.NotNull(method);

        var httpPost = Assert.Single(method.GetCustomAttributes(inherit: true).OfType<HttpPostAttribute>());
        Assert.Equal("cierre-periodo", httpPost.Template);

        AssertRequierePermiso(method, ModuloSistema.Finanzas, AccionPermiso.Cerrar);
    }

    [Fact]
    public async Task CerrarPeriodo_ValidRequest_ReturnsOk()
    {
        var mockService = new Mock<IConciliacionBancariaService>();
        var dto = new CerrarPeriodoConciliacionRequestDto { CuentaBancariaId = 1, Mes = 8, Anio = 2026 };
        var responseDto = new CerrarPeriodoConciliacionResponseDto { Exitoso = true };

        mockService.Setup(s => s.CerrarPeriodoAsync(dto, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateController(mockService.Object, 4);
        var result = await controller.CerrarPeriodo(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(responseDto, okResult.Value);
    }

    [Fact]
    public void ReabrirPeriodo_HasCorrectRouteAndPermission()
    {
        var method = typeof(ConciliacionBancariaController).GetMethod(nameof(ConciliacionBancariaController.ReabrirPeriodo));
        Assert.NotNull(method);

        var httpPost = Assert.Single(method.GetCustomAttributes(inherit: true).OfType<HttpPostAttribute>());
        Assert.Equal("reapertura-periodo", httpPost.Template);

        AssertRequierePermiso(method, ModuloSistema.Finanzas, AccionPermiso.Reabrir);
    }

    [Fact]
    public async Task ReabrirPeriodo_ValidRequest_ReturnsOk()
    {
        var mockService = new Mock<IConciliacionBancariaService>();
        var dto = new ReabrirPeriodoConciliacionRequestDto { CuentaBancariaId = 1, Mes = 8, Anio = 2026 };
        var responseDto = new ReabrirPeriodoConciliacionResponseDto { Exitoso = true };

        mockService.Setup(s => s.ReabrirPeriodoAsync(dto, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var controller = CreateController(mockService.Object, 5);
        var result = await controller.ReabrirPeriodo(dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(responseDto, okResult.Value);
    }
}
