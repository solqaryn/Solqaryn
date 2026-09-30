using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ReportesInventarioKardexControllerTests
{
    private readonly Mock<IMovimientoInventarioService> _movimientosMock;
    private readonly Mock<IAlmacenRepository> _almacenesMock;
    private readonly Mock<IUsuarioScopeService> _usuarioScopeMock;
    private readonly Mock<IAuditoriaService> _auditoriaMock;
    private readonly ReportesInventarioKardexController _controller;

    public ReportesInventarioKardexControllerTests()
    {
        _movimientosMock = new Mock<IMovimientoInventarioService>();
        _almacenesMock = new Mock<IAlmacenRepository>();
        _usuarioScopeMock = new Mock<IUsuarioScopeService>();
        _auditoriaMock = new Mock<IAuditoriaService>();

        _controller = new ReportesInventarioKardexController(
            _movimientosMock.Object,
            _almacenesMock.Object,
            _usuarioScopeMock.Object,
            _auditoriaMock.Object);
    }

    [Fact]
    public async Task Get_InvalidFilter_ReturnsBadRequest()
    {
        var filtro = new ReporteInventarioKardexFiltroDto
        {
            SortDirection = "invalid"
        };

        var result = await _controller.Get(filtro);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Consulta de Kardex inválida.", response.Message);
        Assert.Contains("SortDirection", response.Errors!.First().ToString());
    }

    [Fact]
    public async Task Get_ExplicitPhysicalScopeWithoutAdmin_ReturnsForbid()
    {
        var filtro = new ReporteInventarioKardexFiltroDto { AlmacenId = 1 };
        _usuarioScopeMock.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "User", false));

        var result = await _controller.Get(filtro);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Get_SucursalFilterWithoutAlmacenId_ReturnsBadRequest()
    {
        var filtro = new ReporteInventarioKardexFiltroDto { SucursalId = 1 };
        _usuarioScopeMock.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Admin", true));

        var result = await _controller.Get(filtro);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("Cuando se filtra por SucursalId", response.Errors!.First().ToString());
    }

    [Fact]
    public async Task Get_AlmacenNotFoundOrMismatchSucursal_ReturnsForbid()
    {
        var filtro = new ReporteInventarioKardexFiltroDto { SucursalId = 1, AlmacenId = 2 };
        _usuarioScopeMock.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Admin", true));

        _almacenesMock.Setup(a => a.GetByIdAsync(2)).ReturnsAsync((Almacen)null!);

        var result = await _controller.Get(filtro);
        Assert.IsType<ForbidResult>(result);

        _almacenesMock.Setup(a => a.GetByIdAsync(2)).ReturnsAsync(new Almacen { SucursalId = 3 });

        var result2 = await _controller.Get(filtro);
        Assert.IsType<ForbidResult>(result2);
    }

    [Fact]
    public async Task Get_ValidQuery_ReturnsOkAndAudits()
    {
        var filtro = new ReporteInventarioKardexFiltroDto { Page = 1, PageSize = 10 };
        _usuarioScopeMock.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Admin", true));

        var pagedResult = new PagedResult<MovimientoInventarioDto> { Items = new List<MovimientoInventarioDto>(), TotalCount = 0, Page = 1, PageSize = 10 };
        _movimientosMock.Setup(m => m.GetPagedAsync(It.IsAny<MovimientoInventarioQueryDto>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.Get(filtro);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<MovimientoInventarioDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Same(pagedResult, response.Data);

        _auditoriaMock.Verify(a => a.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.ConsultarHistorial,
            "Consulta de Kardex de inventario.",
            null,
            "KardexInventario",
            null,
            It.IsAny<object>(),
            null,
            "Exito",
            null), Times.Once);
    }
}
