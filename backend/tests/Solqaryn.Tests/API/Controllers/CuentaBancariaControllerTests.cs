using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Bancos;
using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class CuentaBancariaControllerTests
{
    private readonly Mock<ICuentaBancariaService> _serviceMock;
    private readonly CuentaBancariaController _controller;

    public CuentaBancariaControllerTests()
    {
        _serviceMock = new Mock<ICuentaBancariaService>();
        _controller = new CuentaBancariaController(_serviceMock.Object);
    }

    [Fact]
    public void Controller_ExigeAutenticacionYRutaCanonica()
    {
        var type = typeof(CuentaBancariaController);
        Assert.Single(type.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true));
        Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        Assert.Equal("cuentas-bancarias", route.Template);
    }

    [Theory]
    [InlineData(nameof(CuentaBancariaController.GetAll), typeof(HttpGetAttribute), null, AccionPermiso.Ver)]
    [InlineData(nameof(CuentaBancariaController.GetActivas), typeof(HttpGetAttribute), "activas", AccionPermiso.Ver)]
    [InlineData(nameof(CuentaBancariaController.GetById), typeof(HttpGetAttribute), "{id:int}", AccionPermiso.Ver)]
    [InlineData(nameof(CuentaBancariaController.Create), typeof(HttpPostAttribute), null, AccionPermiso.Crear)]
    [InlineData(nameof(CuentaBancariaController.Activar), typeof(HttpPatchAttribute), "{id:int}/activar", AccionPermiso.Activar)]
    [InlineData(nameof(CuentaBancariaController.Desactivar), typeof(HttpPatchAttribute), "{id:int}/desactivar", AccionPermiso.Desactivar)]
    public void Acciones_TienenAtributosDeRutaYPermisoCorrectos(string methodName, Type httpVerbType, string? expectedTemplate, AccionPermiso expectedPermiso)
    {
        var methodInfo = typeof(CuentaBancariaController).GetMethod(methodName);
        Assert.NotNull(methodInfo);
        var httpAttr = methodInfo!.GetCustomAttributes(httpVerbType, inherit: false).FirstOrDefault();
        Assert.NotNull(httpAttr);
        if (expectedTemplate is not null)
        {
            var template = httpVerbType.GetProperty("Template")?.GetValue(httpAttr) as string;
            Assert.Equal(expectedTemplate, template);
        }
        var permisoAttr = methodInfo.GetCustomAttributes(typeof(RequierePermisoAttribute), inherit: false).Cast<RequierePermisoAttribute>().Single();
        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(ModuloSistema.Finanzas, (ModuloSistema)moduloField!.GetValue(permisoAttr)!);
        Assert.Equal(expectedPermiso, (AccionPermiso)accionField!.GetValue(permisoAttr)!);
    }

    [Fact]
    public async Task GetAll_RetornaOkConPagina()
    {
        var items = new List<CuentaBancariaDto> { new() { Id = 1 } };
        var expected = new CuentaBancariaPage<CuentaBancariaDto>(items, 1, 10, 1);
        var filter = new CuentaBancariaQueryFilter();
        _serviceMock.Setup(s => s.GetAllAsync(filter)).ReturnsAsync(expected);
        var result = await _controller.GetAll(filter);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, okResult.Value);
        _serviceMock.Verify(s => s.GetAllAsync(filter), Times.Once);
    }

    [Fact]
    public async Task GetActivas_RetornaOkConLista()
    {
        var expected = new List<CuentaBancariaDto> { new() { Id = 2 } };
        _serviceMock.Setup(s => s.GetActivasAsync()).ReturnsAsync(expected);
        var result = await _controller.GetActivas();
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, okResult.Value);
        _serviceMock.Verify(s => s.GetActivasAsync(), Times.Once);
    }

    [Fact]
    public async Task GetById_CuandoNoExiste_RetornaProblemDetails404()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((CuentaBancariaDto?)null);
        var result = await _controller.GetById(99);
        var notFound = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal("Cuenta bancaria no encontrada", problem.Title);
        _serviceMock.Verify(s => s.GetByIdAsync(99), Times.Once);
    }

    [Fact]
    public async Task GetById_CuandoExiste_RetornaOk()
    {
        var expected = new CuentaBancariaDto { Id = 1 };
        _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(expected);
        var result = await _controller.GetById(1);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, okResult.Value);
        _serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task Create_RetornaCreatedAtAction()
    {
        var dto = new CreateCuentaBancariaDto { Nombre = "Test" };
        var created = new CuentaBancariaDto { Id = 10, Nombre = "Test" };
        _serviceMock.Setup(s => s.AddAsync(dto)).ReturnsAsync(created);
        var result = await _controller.Create(dto);
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(CuentaBancariaController.GetById), createdResult.ActionName);
        Assert.Equal(10, createdResult.RouteValues?["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task Activar_RetornaNoContent_LlamaAService()
    {
        var result = await _controller.Activar(5);
        Assert.IsType<NoContentResult>(result);
        _serviceMock.Verify(s => s.ActivarAsync(5), Times.Once);
    }

    [Fact]
    public async Task Desactivar_RetornaNoContent_LlamaAService()
    {
        var result = await _controller.Desactivar(5);
        Assert.IsType<NoContentResult>(result);
        _serviceMock.Verify(s => s.DesactivarAsync(5), Times.Once);
    }

    [Fact]
    public async Task Activar_CuandoNoExiste_RetornaProblemDetails404()
    {
        _serviceMock.Setup(s => s.ActivarAsync(99)).ThrowsAsync(new InvalidOperationException());
        var result = await _controller.Activar(99);
        var notFound = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal("Cuenta bancaria no encontrada", problem.Title);
        _serviceMock.Verify(s => s.ActivarAsync(99), Times.Once);
    }

    [Fact]
    public async Task Desactivar_CuandoNoExiste_RetornaProblemDetails404()
    {
        _serviceMock.Setup(s => s.DesactivarAsync(99)).ThrowsAsync(new InvalidOperationException());
        var result = await _controller.Desactivar(99);
        var notFound = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal("Cuenta bancaria no encontrada", problem.Title);
        _serviceMock.Verify(s => s.DesactivarAsync(99), Times.Once);
    }
}
