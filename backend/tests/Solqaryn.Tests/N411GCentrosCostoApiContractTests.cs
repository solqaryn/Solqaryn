using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N411GCentrosCostoApiContractTests
{
    private readonly Mock<ICentroCostoService> _service = new();

    [Fact]
    public void Controller_ExponeRutaAutenticadaYContratoDePermisosCompleto()
    {
        var type = typeof(CentrosCostoController);

        Assert.Single(type.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true));
        Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        Assert.Equal("centros-costo", route.Template);

        AssertEndpoint(nameof(CentrosCostoController.Buscar), typeof(HttpGetAttribute), null, AccionPermiso.Ver);
        AssertEndpoint(nameof(CentrosCostoController.GetActivos), typeof(HttpGetAttribute), "activos", AccionPermiso.Ver);
        AssertEndpoint(nameof(CentrosCostoController.GetById), typeof(HttpGetAttribute), "{id:int}", AccionPermiso.Ver);
        AssertEndpoint(nameof(CentrosCostoController.Create), typeof(HttpPostAttribute), null, AccionPermiso.Crear);
        AssertEndpoint(nameof(CentrosCostoController.Update), typeof(HttpPutAttribute), "{id:int}", AccionPermiso.Editar);
        AssertEndpoint(nameof(CentrosCostoController.Activar), typeof(HttpPatchAttribute), "{id:int}/activar", AccionPermiso.Activar);
        AssertEndpoint(nameof(CentrosCostoController.Desactivar), typeof(HttpPatchAttribute), "{id:int}/desactivar", AccionPermiso.Desactivar);
        AssertEndpoint(nameof(CentrosCostoController.Delete), typeof(HttpDeleteAttribute), "{id:int}", AccionPermiso.EliminarLogico);
    }

    [Fact]
    public async Task Buscar_NormalizaPaginacionEnRespuestaYSinAlterarArgumentosDelServicio()
    {
        _service
            .Setup(s => s.BuscarAsync("adm", null, 7, true, 0, 250))
            .ReturnsAsync((new List<CentroCostoDto> { new() { Id = 1, Codigo = "ADM", Nombre = "Administracion" } }, 1));

        var controller = new CentrosCostoController(_service.Object);
        var result = await controller.Buscar("adm", null, 7, true, 0, 250);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.BuscarAsync("adm", null, 7, true, 0, 250), Times.Once);
    }

    [Fact]
    public async Task GetById_NoExistente_Retorna404()
    {
        _service.Setup(s => s.GetByIdAsync(404)).ReturnsAsync((CentroCostoDto?)null);
        var controller = new CentrosCostoController(_service.Object);

        var result = await controller.GetById(404);

        Assert.IsType<NotFoundObjectResult>(result);
        _service.Verify(s => s.GetByIdAsync(404), Times.Once);
    }

    [Fact]
    public async Task Create_RetornaCreatedAtActionConIdCreado()
    {
        var request = new CreateCentroCostoDto { Codigo = "CC-01", Nombre = "Centro 01" };
        var created = new CentroCostoDto { Id = 21, Codigo = request.Codigo, Nombre = request.Nombre };
        _service.Setup(s => s.CreateAsync(request)).ReturnsAsync(created);
        var controller = new CentrosCostoController(_service.Object);

        var result = await controller.Create(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(CentrosCostoController.GetById), createdResult.ActionName);
        Assert.Equal(21, createdResult.RouteValues?["id"]);
        _service.Verify(s => s.CreateAsync(request), Times.Once);
    }

    [Fact]
    public async Task Update_NoExistente_Retorna404()
    {
        var request = new UpdateCentroCostoDto { Codigo = "CC-99", Nombre = "Inexistente", Activo = true };
        _service.Setup(s => s.UpdateAsync(99, request)).ReturnsAsync((CentroCostoDto?)null);
        var controller = new CentrosCostoController(_service.Object);

        var result = await controller.Update(99, request);

        Assert.IsType<NotFoundObjectResult>(result);
        _service.Verify(s => s.UpdateAsync(99, request), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CambioEstado_PropagaEstadoYRetorna404CuandoNoExiste(bool activo)
    {
        _service.Setup(s => s.CambiarEstadoAsync(77, activo)).ReturnsAsync((CentroCostoDto?)null);
        var controller = new CentrosCostoController(_service.Object);

        IActionResult result = activo
            ? await controller.Activar(77)
            : await controller.Desactivar(77);

        Assert.IsType<NotFoundObjectResult>(result);
        _service.Verify(s => s.CambiarEstadoAsync(77, activo), Times.Once);
    }

    [Fact]
    public async Task Delete_ReflejaContratoDeEliminacionLogica()
    {
        _service.Setup(s => s.DeleteAsync(5)).ReturnsAsync(true);
        _service.Setup(s => s.DeleteAsync(6)).ReturnsAsync(false);
        var controller = new CentrosCostoController(_service.Object);

        Assert.IsType<OkObjectResult>(await controller.Delete(5));
        Assert.IsType<NotFoundObjectResult>(await controller.Delete(6));
        _service.Verify(s => s.DeleteAsync(5), Times.Once);
        _service.Verify(s => s.DeleteAsync(6), Times.Once);
    }

    private static void AssertEndpoint(string methodName, Type verbType, string? expectedTemplate, AccionPermiso expectedPermission)
    {
        var method = typeof(CentrosCostoController).GetMethod(methodName);
        Assert.NotNull(method);

        var verb = Assert.Single(method!.GetCustomAttributes(verbType, inherit: false));
        var template = verbType.GetProperty("Template")?.GetValue(verb) as string;
        Assert.Equal(expectedTemplate, template);

        var permission = Assert.Single(method.GetCustomAttributes(typeof(RequierePermisoAttribute), inherit: false).Cast<RequierePermisoAttribute>());
        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(ModuloSistema.Finanzas, (ModuloSistema)moduloField!.GetValue(permission)!);
        Assert.Equal(expectedPermission, (AccionPermiso)accionField!.GetValue(permission)!);
    }
}
