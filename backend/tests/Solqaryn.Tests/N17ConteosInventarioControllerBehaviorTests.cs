using System.Text.Json;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteosInventarioControllerBehaviorTests
{
    [Fact]
    public async Task Create_RetornaCreatedAtActionConConteoCreado()
    {
        var service = new Mock<IConteoInventarioService>();
        var dto = new CreateConteoInventarioDto { AlmacenId = 1, ProductoVarianteIds = { 10 } };
        var creado = new ConteoInventarioDto { Id = 77, Numero = "CNT-77", AlmacenId = 1 };
        service.Setup(x => x.CreateAsync(dto)).ReturnsAsync(creado);
        var controller = new ConteosInventarioController(service.Object);

        var result = await controller.Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ConteosInventarioController.GetById), created.ActionName);
        Assert.Equal(77, created.RouteValues!["id"]);
        service.Verify(x => x.CreateAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Buscar_PropagaPaginacionTipada()
    {
        var service = new Mock<IConteoInventarioService>();
        var query = new ConteoInventarioQueryDto { Page = 2, PageSize = 15, Search = "CNT" };
        var page = new PagedResult<ConteoInventarioDto> { Page = 2, PageSize = 15, TotalCount = 31 };
        service.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(page);
        var controller = new ConteosInventarioController(service.Object);

        var result = await controller.Buscar(query);

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<PagedResult<ConteoInventarioDto>>>(ok.Value);
        Assert.Same(page, envelope.Data);
        service.Verify(x => x.GetPagedAsync(query), Times.Once);
    }

    [Theory]
    [InlineData("get")]
    [InlineData("iniciar")]
    [InlineData("cerrar")]
    [InlineData("aprobar")]
    public async Task AccionesSobreConteoInexistente_RetornanNotFound(string accion)
    {
        var service = new Mock<IConteoInventarioService>();
        service.Setup(x => x.GetByIdAsync(404)).ReturnsAsync((ConteoInventarioDto?)null);
        service.Setup(x => x.IniciarAsync(404)).ReturnsAsync((ConteoInventarioDto?)null);
        service.Setup(x => x.CerrarAsync(404)).ReturnsAsync((ConteoInventarioDto?)null);
        service.Setup(x => x.AprobarAsync(404)).ReturnsAsync((ConteoInventarioDto?)null);
        var controller = new ConteosInventarioController(service.Object);

        IActionResult result = accion switch
        {
            "get" => await controller.GetById(404),
            "iniciar" => await controller.Iniciar(404),
            "cerrar" => await controller.Cerrar(404),
            _ => await controller.Aprobar(404)
        };

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Capturar_ReglaDeNegocio_PropagaExcepcionParaProblemDetailsGlobal()
    {
        var service = new Mock<IConteoInventarioService>();
        var dto = new CapturarConteoInventarioDetalleDto { CantidadContada = 3 };
        service.Setup(x => x.CapturarDetalleAsync(12, 4, dto))
            .ThrowsAsync(new BusinessRuleException("Solo un conteo en proceso admite capturas."));
        var controller = new ConteosInventarioController(service.Object);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => controller.Capturar(12, 4, dto));

        Assert.Contains("proceso", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Capturar_AuditaDetalleYCantidadContada()
    {
        var service = new Mock<IConteoInventarioService>();
        var auditoria = new Mock<IAuditoriaService>();
        var dto = new CapturarConteoInventarioDetalleDto { CantidadContada = 17 };
        var conteo = new ConteoInventarioDto { Id = 12, Numero = "CNT-12", AlmacenId = 3 };
        service.Setup(x => x.CapturarDetalleAsync(12, 4, dto)).ReturnsAsync(conteo);
        var controller = new ConteosInventarioController(service.Object, auditoria.Object);

        var result = await controller.Capturar(12, 4, dto);

        Assert.IsType<OkObjectResult>(result);
        auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Editar,
            It.Is<string>(descripcion => descripcion.Contains("4", StringComparison.Ordinal)),
            12,
            "ConteoInventario",
            null,
            It.Is<object>(valores =>
                JsonSerializer.Serialize(valores, (JsonSerializerOptions?)null).Contains("4", StringComparison.Ordinal) &&
                JsonSerializer.Serialize(valores, (JsonSerializerOptions?)null).Contains("17", StringComparison.Ordinal)),
            null,
            "Exito",
            null), Times.Once);
    }

    [Fact]
    public async Task CapturarLote_AuditaLineasYCantidades()
    {
        var service = new Mock<IConteoInventarioService>();
        var auditoria = new Mock<IAuditoriaService>();
        var dto = new CapturarConteoInventarioLoteDto
        {
            Lineas =
            {
                new CapturaConteoInventarioLineaDto { DetalleId = 4, CantidadContada = 17 },
                new CapturaConteoInventarioLineaDto { DetalleId = 5, CantidadContada = 21 }
            }
        };
        var conteo = new ConteoInventarioDto { Id = 12, Numero = "CNT-12", AlmacenId = 3 };
        service.Setup(x => x.CapturarLoteAsync(12, dto)).ReturnsAsync(conteo);
        var controller = new ConteosInventarioController(service.Object, auditoria.Object);

        var result = await controller.CapturarLote(12, dto);

        Assert.IsType<OkObjectResult>(result);
        auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Editar,
            It.Is<string>(descripcion => descripcion.Contains("2", StringComparison.Ordinal)),
            12,
            "ConteoInventario",
            null,
            It.Is<object>(valores =>
                JsonSerializer.Serialize(valores, (JsonSerializerOptions?)null).Contains("17", StringComparison.Ordinal) &&
                JsonSerializer.Serialize(valores, (JsonSerializerOptions?)null).Contains("21", StringComparison.Ordinal)),
            null,
            "Exito",
            null), Times.Once);
    }

    [Fact]
    public async Task Cancelar_DelegaMotivoYRetornaOk()
    {
        var service = new Mock<IConteoInventarioService>();
        var dto = new CancelarConteoInventarioDto { Motivo = "Reconteo requerido" };
        var cancelado = new ConteoInventarioDto { Id = 20, Numero = "CNT-20" };
        service.Setup(x => x.CancelarAsync(20, dto.Motivo)).ReturnsAsync(cancelado);
        var controller = new ConteosInventarioController(service.Object);

        var result = await controller.Cancelar(20, dto);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(x => x.CancelarAsync(20, dto.Motivo), Times.Once);
    }

    [Fact]
    public async Task Cancelar_RegistraAuditoriaConMotivoYReferencia()
    {
        var service = new Mock<IConteoInventarioService>();
        var auditoria = new Mock<IAuditoriaService>();
        var dto = new CancelarConteoInventarioDto { Motivo = "Diferencias requieren reconteo" };
        var cancelado = new ConteoInventarioDto { Id = 20, Numero = "CNT-20" };
        service.Setup(x => x.CancelarAsync(20, dto.Motivo)).ReturnsAsync(cancelado);
        var controller = new ConteosInventarioController(service.Object, auditoria.Object);

        var result = await controller.Cancelar(20, dto);

        Assert.IsType<OkObjectResult>(result);
        auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Anular,
            It.Is<string>(descripcion => descripcion.Contains("cancelado", StringComparison.OrdinalIgnoreCase)),
            20,
            "ConteoInventario",
            null,
            It.IsAny<object>(),
            dto.Motivo,
            "Exito",
            null), Times.Once);
    }

    [Fact]
    public async Task Cerrar_RegistraAuditoriaConPermisoCerrar()
    {
        var service = new Mock<IConteoInventarioService>();
        var auditoria = new Mock<IAuditoriaService>();
        var cerrado = new ConteoInventarioDto { Id = 30, Numero = "CNT-30", Estado = EstadoConteoInventario.Cerrado };
        service.Setup(x => x.CerrarAsync(30)).ReturnsAsync(cerrado);
        var controller = new ConteosInventarioController(service.Object, auditoria.Object);

        var result = await controller.Cerrar(30);

        Assert.IsType<OkObjectResult>(result);
        auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Cerrar,
            It.Is<string>(descripcion => descripcion.Contains("cerrado", StringComparison.OrdinalIgnoreCase)),
            30,
            "ConteoInventario",
            null,
            It.IsAny<object>(),
            null,
            "Exito",
            null), Times.Once);
    }

    [Fact]
    public async Task GenerarAjuste_AuditaConteoYAjusteGenerado()
    {
        var service = new Mock<IConteoInventarioService>();
        var auditoria = new Mock<IAuditoriaService>();
        var ajuste = new AjusteInventarioDto { Id = 901 };
        service.Setup(x => x.GenerarAjusteAsync(77)).ReturnsAsync(ajuste);
        var controller = new ConteosInventarioController(service.Object, auditoria.Object);

        var result = await controller.GenerarAjuste(77);

        Assert.IsType<OkObjectResult>(result);
        auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Crear,
            It.Is<string>(descripcion => descripcion.Contains("Ajuste", StringComparison.OrdinalIgnoreCase)),
            77,
            "ConteoInventario",
            null,
            It.Is<object>(valores => JsonSerializer.Serialize(valores, (JsonSerializerOptions?)null).Contains("901", StringComparison.Ordinal)),
            null,
            "Exito",
            null), Times.Once);
    }

    [Fact]
    public async Task GenerarAjuste_ConteoInexistente_NoAudita()
    {
        var service = new Mock<IConteoInventarioService>();
        var auditoria = new Mock<IAuditoriaService>();
        service.Setup(x => x.GenerarAjusteAsync(404)).ReturnsAsync((AjusteInventarioDto?)null);
        var controller = new ConteosInventarioController(service.Object, auditoria.Object);

        var result = await controller.GenerarAjuste(404);

        Assert.IsType<NotFoundObjectResult>(result);
        auditoria.Verify(x => x.RegistrarAsync(
            It.IsAny<ModuloSistema>(),
            It.IsAny<AccionPermiso>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Never);
    }
}