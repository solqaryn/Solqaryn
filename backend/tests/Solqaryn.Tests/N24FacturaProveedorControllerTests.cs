using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N24FacturaProveedorControllerTests
{
    private readonly Mock<IFacturaProveedorService> _serviceMock = new();
    private readonly Mock<ILogger<FacturasProveedorController>> _loggerMock = new();
    private readonly FacturasProveedorController _controller;

    public N24FacturaProveedorControllerTests()
    {
        _controller = new FacturasProveedorController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Buscar_Retorna200OkConResultados()
    {
        var filtro = new FacturaProveedorFiltroDto();
        var pagedResult = new PagedResult<FacturaProveedorDto> { Items = new List<FacturaProveedorDto>(), TotalCount = 0 };
        _serviceMock.Setup(s => s.GetPagedAsync(filtro)).ReturnsAsync(pagedResult);

        var result = await _controller.Buscar(filtro);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResult<FacturaProveedorDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(pagedResult, apiResponse.Data);
    }

    [Fact]
    public async Task GetById_Existente_Retorna200Ok()
    {
        const int facturaId = 1;
        var facturaDto = new FacturaProveedorDto { Id = facturaId };
        _serviceMock.Setup(s => s.GetByIdAsync(facturaId)).ReturnsAsync(facturaDto);

        var result = await _controller.GetById(facturaId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<FacturaProveedorDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(facturaDto, apiResponse.Data);
    }

    [Fact]
    public async Task GetById_NoExistente_Retorna404ProblemDetails()
    {
        const int facturaId = 99;
        _serviceMock.Setup(s => s.GetByIdAsync(facturaId)).ReturnsAsync((FacturaProveedorDto?)null);

        var result = await _controller.GetById(facturaId);

        var problemResult = Assert.IsType<ObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(problemResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal("Factura de proveedor no encontrada", problemDetails.Title);
    }

    [Fact]
    public async Task Create_Valido_Retorna201CreatedAtAction()
    {
        var dto = new CreateFacturaProveedorDto();
        var creada = new FacturaProveedorDto { Id = 1 };
        _serviceMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync(creada);

        var result = await _controller.Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(FacturasProveedorController.GetById), created.ActionName);
        Assert.Equal(1, created.RouteValues!["id"]);
        var apiResponse = Assert.IsType<ApiResponse<FacturaProveedorDto>>(created.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(creada, apiResponse.Data);
    }

    [Fact]
    public async Task Update_Valido_Retorna200Ok()
    {
        const int id = 1;
        var dto = new UpdateFacturaProveedorDto();
        var actualizada = new FacturaProveedorDto { Id = id };
        _serviceMock.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(actualizada);

        var result = await _controller.Update(id, dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<FacturaProveedorDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(actualizada, apiResponse.Data);
    }

    [Fact]
    public async Task Registrar_Valido_Retorna200Ok()
    {
        const int id = 1;
        var registrada = new FacturaProveedorDto { Id = id };
        _serviceMock.Setup(s => s.RegistrarAsync(id)).ReturnsAsync(registrada);

        var result = await _controller.Registrar(id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<FacturaProveedorDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(registrada, apiResponse.Data);
    }

    [Fact]
    public async Task Anular_Valido_Retorna200Ok()
    {
        const int id = 1;
        var dto = new AnularFacturaProveedorDto { Motivo = "Prueba" };
        var anulada = new FacturaProveedorDto { Id = id };
        _serviceMock.Setup(s => s.AnularAsync(id, dto)).ReturnsAsync(anulada);

        var result = await _controller.Anular(id, dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<FacturaProveedorDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(anulada, apiResponse.Data);
    }

    [Fact]
    public void Controller_ExigeAutenticacion()
    {
        Assert.Contains(
            typeof(FacturasProveedorController).GetCustomAttributes(inherit: true),
            attribute => attribute is AuthorizeAttribute);
    }

    [Theory]
    [InlineData(nameof(FacturasProveedorController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(FacturasProveedorController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(FacturasProveedorController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(FacturasProveedorController.Update), AccionPermiso.Editar)]
    [InlineData(nameof(FacturasProveedorController.Registrar), AccionPermiso.Confirmar)]
    [InlineData(nameof(FacturasProveedorController.Anular), AccionPermiso.Anular)]
    public void Endpoint_DeclaraPermisoComprasEsperado(string methodName, AccionPermiso accion)
    {
        var method = typeof(FacturasProveedorController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"No se encontró {methodName}.");
        var permiso = Assert.Single(
            method.CustomAttributes.Where(attribute => attribute.AttributeType == typeof(RequierePermisoAttribute)));

        Assert.Equal((int)ModuloSistema.Compras, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)accion, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }
}
