using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// C07-04.M05.02: tests del controlador real; sin base de datos ni JWT simulado.
/// </summary>
public sealed class V20C0704M0502PedidoVentaApiContractTests
{
    [Theory]
    [InlineData(nameof(PedidosVentaController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(PedidosVentaController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(PedidosVentaController.Crear), AccionPermiso.Crear)]
    [InlineData(nameof(PedidosVentaController.Actualizar), AccionPermiso.Editar)]
    [InlineData(nameof(PedidosVentaController.Confirmar), AccionPermiso.Confirmar)]
    [InlineData(nameof(PedidosVentaController.Anular), AccionPermiso.Anular)]
    public void RutaPrivadaExigeAutenticacionYPermisoVentas(string metodo, AccionPermiso accion)
    {
        var controller = typeof(PedidosVentaController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("pedidos-venta", controller.GetCustomAttribute<RouteAttribute>()?.Template);
        var method = Assert.IsAssignableFrom<MethodInfo>(controller.GetMethod(metodo));
        var permiso = Assert.IsType<RequierePermisoAttribute>(
            method.GetCustomAttribute<RequierePermisoAttribute>());
        Assert.Equal(ModuloSistema.Ventas, permiso.Modulo);
        Assert.Equal(accion, permiso.Accion);
    }

    [Fact]
    public void CrearDeclaraEncabezadoDeIdempotenciaNoBody()
    {
        var method = typeof(PedidosVentaController).GetMethod(nameof(PedidosVentaController.Crear));
        Assert.NotNull(method);
        var header = method.GetParameters()[1].GetCustomAttribute<FromHeaderAttribute>();
        Assert.Equal("Idempotency-Key", header?.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CrearSinClaveRechaza400AntesDeServicio(string? key)
    {
        var service = new Mock<IPedidoVentaService>(MockBehavior.Strict);
        var controller = Make(service);
        var result = await controller.Crear(new CreatePedidoVentaDto { CotizacionId = 8 }, key);
        var failure = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, failure.StatusCode);
        var body = Assert.IsType<ProblemDetails>(failure.Value);
        Assert.Equal("Idempotency-Key requerido", body.Title);
        Assert.DoesNotContain("password", body.Detail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CrearConClaveReutilizaContratoDtoYLocationSinMutacionAdicional()
    {
        var service = new Mock<IPedidoVentaService>(MockBehavior.Strict);
        var requested = new CreatePedidoVentaDto { CotizacionId = 8 };
        var saved = new PedidoVentaDto { Id = 91, ClienteId = 13, Total = 29.50m };
        service.Setup(x => x.CrearDesdeCotizacionAsync(requested, "idemp-91")).ReturnsAsync(saved);
        var result = await Make(service).Crear(requested, "idemp-91");
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(PedidosVentaController.GetById), created.ActionName);
        Assert.Equal(91, created.RouteValues?["id"]);
        var body = Assert.IsType<ApiResponse<PedidoVentaDto>>(created.Value);
        Assert.True(body.Success);
        Assert.Same(saved, body.Data);
        Assert.Empty(body.Errors);
        service.Verify(x => x.CrearDesdeCotizacionAsync(requested, "idemp-91"), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ActualizarUsaIdDeRutaEnLugarDeIdFalsificadoEnBody()
    {
        var service = new Mock<IPedidoVentaService>(MockBehavior.Strict);
        var incoming = new UpdatePedidoVentaDto { Id = 999, Observaciones = "control" };
        service.Setup(x => x.ActualizarAsync(It.Is<UpdatePedidoVentaDto>(d => d.Id == 7)))
            .ReturnsAsync(new PedidoVentaDto { Id = 7 });
        var result = await Make(service).Actualizar(7, incoming);
        Assert.Equal(7, incoming.Id);
        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ApiResponse<PedidoVentaDto>>(ok.Value);
        Assert.Equal(7, body.Data?.Id);
        service.Verify(x => x.ActualizarAsync(It.Is<UpdatePedidoVentaDto>(d => d.Id == 7)), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ConsultarIdDevuelveEnvelopeTipadoYNoInvocaMutaciones()
    {
        var service = new Mock<IPedidoVentaService>(MockBehavior.Strict);
        var pedido = new PedidoVentaDto { Id = 77, Total = 19m };
        service.Setup(x => x.GetByIdAsync(77)).ReturnsAsync(pedido);
        var response = await Make(service).GetById(77);
        var ok = Assert.IsType<OkObjectResult>(response);
        var body = Assert.IsType<ApiResponse<PedidoVentaDto>>(ok.Value);
        Assert.True(body.Success);
        Assert.Same(pedido, body.Data);
        Assert.Empty(body.Errors);
        service.Verify(x => x.GetByIdAsync(77), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuscarPreservaFiltroYEnvelopePaginado()
    {
        var service = new Mock<IPedidoVentaService>(MockBehavior.Strict);
        var filtro = new PedidoVentaFiltroDto { ClienteId = 19, Page = 2, PageSize = 10 };
        var page = new PagedResult<PedidoVentaDto> { Page = 2, PageSize = 10, TotalCount = 1 };
        service.Setup(x => x.GetPagedAsync(filtro)).ReturnsAsync(page);
        var response = await Make(service).Buscar(filtro);
        var ok = Assert.IsType<OkObjectResult>(response);
        var body = Assert.IsType<ApiResponse<PagedResult<PedidoVentaDto>>>(ok.Value);
        Assert.True(body.Success);
        Assert.Same(page, body.Data);
        Assert.Equal(19, filtro.ClienteId);
        service.Verify(x => x.GetPagedAsync(filtro), Times.Once);
        service.VerifyNoOtherCalls();
    }

    private static PedidosVentaController Make(Mock<IPedidoVentaService> mock)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        var provider = services.BuildServiceProvider();
        return new PedidosVentaController(mock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider }
            }
        };
    }
}
