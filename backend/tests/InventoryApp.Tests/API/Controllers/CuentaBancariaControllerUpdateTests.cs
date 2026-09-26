using System.Reflection;
using InventoryApp.API.Controllers;
using InventoryApp.API.Filters;
using InventoryApp.Application.DTOs.Bancos;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryApp.Tests.API.Controllers;

public sealed class CuentaBancariaControllerUpdateTests
{
    private readonly Mock<ICuentaBancariaService> _serviceMock;
    private readonly CuentaBancariaController _controller;

    public CuentaBancariaControllerUpdateTests()
    {
        _serviceMock = new Mock<ICuentaBancariaService>();
        _controller = new CuentaBancariaController(_serviceMock.Object);
    }

    [Fact]
    public async Task Update_RetornaNoContent_LlamaAService()
    {
        var dto = new UpdateCuentaBancariaDto { Nombre = "Nuevo Nombre" };
        var id = 5;

        var result = await _controller.Update(id, dto);

        Assert.IsType<NoContentResult>(result);
        _serviceMock.Verify(s => s.UpdateAsync(id, dto), Times.Once);
    }

    [Fact]
    public async Task Update_RetornaProblemDetails404_CuandoNoExiste()
    {
        var dto = new UpdateCuentaBancariaDto { Nombre = "Nuevo Nombre" };
        var id = 99;
        _serviceMock.Setup(s => s.UpdateAsync(id, dto)).ThrowsAsync(new InvalidOperationException());

        var result = await _controller.Update(id, dto);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(404, problemDetails.Status);
        Assert.Equal("Cuenta bancaria no encontrada", problemDetails.Title);
        Assert.Equal($"No existe una cuenta bancaria con Id {id}.", problemDetails.Detail);
        Assert.Equal("https://solqaryn.local/problems/cuenta-bancaria-no-encontrada", problemDetails.Type);
    }

    [Fact]
    public void Update_TieneAtributoHttpPut_ConRutaId()
    {
        var methodInfo = typeof(CuentaBancariaController).GetMethod(nameof(CuentaBancariaController.Update));
        var attribute = methodInfo?.GetCustomAttribute<HttpPutAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("{id:int}", attribute.Template);
    }

    [Fact]
    public void Update_TieneAtributoRequierePermiso_ParaEditarFinanzas()
    {
        var methodInfo = typeof(CuentaBancariaController).GetMethod(nameof(CuentaBancariaController.Update));
        var attribute = methodInfo?.GetCustomAttribute<RequierePermisoAttribute>();

        Assert.NotNull(attribute);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);

        var modulo = moduloField?.GetValue(attribute);
        var accion = accionField?.GetValue(attribute);

        Assert.Equal(ModuloSistema.Finanzas, modulo);
        Assert.Equal(AccionPermiso.Editar, accion);
    }
}
