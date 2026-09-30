using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class RentabilidadVentasContractTests
{
    [Fact]
    public void Controller_ExigeAutenticacionRutaYPermisoVentasVerEnTodosLosEndpoints()
    {
        var type = typeof(RentabilidadVentasController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("ventas/rentabilidad", type.GetCustomAttribute<RouteAttribute>()?.Template);

        foreach (var methodName in new[]
        {
            nameof(RentabilidadVentasController.GetVendedores),
            nameof(RentabilidadVentasController.GetClientes),
            nameof(RentabilidadVentasController.GetProductos),
            nameof(RentabilidadVentasController.GetCategorias)
        })
        {
            var method = type.GetMethod(methodName);
            Assert.NotNull(method);
            var permiso = Assert.Single(method!.CustomAttributes.Where(a => a.AttributeType == typeof(RequierePermisoAttribute)));
            Assert.Equal((int)ModuloSistema.Ventas, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
            Assert.Equal((int)AccionPermiso.Ver, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
        }
    }

    [Fact]
    public async Task Vendedores_ConsultaValidaRegistraAuditoriaConCorrelationIdYAgrupacion()
    {
        var service = new Mock<IRentabilidadVentasService>(MockBehavior.Strict);
        service.Setup(s => s.ObtenerAsync(
                It.IsAny<ReporteVentasFiltroDto>(),
                RentabilidadAgrupacion.Vendedor,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ReporteRentabilidadDto>());
        var auditoria = CreateAuditoriaStrictMock();
        var controller = new RentabilidadVentasController(service.Object, auditoria.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "corr-n54-f2" }
            }
        };

        var result = await controller.GetVendedores(new ReporteVentasFiltroDto());

        Assert.IsType<OkObjectResult>(result);
        auditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Ventas,
            AccionPermiso.Ver,
            It.Is<string>(descripcion => descripcion.Contains("'Vendedor'", StringComparison.Ordinal)),
            null,
            "RentabilidadVentas",
            null,
            It.Is<object>(valores =>
                ReadProperty(valores, "CorrelationId") == "corr-n54-f2" &&
                ReadProperty(valores, "Agrupacion") == "Vendedor"),
            null,
            "Exito",
            null), Times.Once);
        service.VerifyAll();
        auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ConsultaInvalidaFallaAntesDeServicioYAuditoria()
    {
        var service = new Mock<IRentabilidadVentasService>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var controller = new RentabilidadVentasController(service.Object, auditoria.Object);
        var filtro = new ReporteVentasFiltroDto
        {
            Desde = new DateTime(2026, 9, 10),
            Hasta = new DateTime(2026, 9, 1)
        };

        var result = await controller.GetClientes(filtro);

        Assert.IsType<BadRequestObjectResult>(result);
        service.VerifyNoOtherCalls();
        auditoria.VerifyNoOtherCalls();
    }

    private static Mock<IAuditoriaService> CreateAuditoriaStrictMock()
    {
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        auditoria.Setup(a => a.RegistrarAsync(
                It.IsAny<ModuloSistema>(),
                It.IsAny<AccionPermiso>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        return auditoria;
    }

    private static string? ReadProperty(object value, string propertyName) =>
        value.GetType().GetProperty(propertyName)?.GetValue(value)?.ToString();
}
