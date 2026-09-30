using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ReportesInventarioValorizacionContractTests
{
    [Fact]
    public async Task GetResumen_SinFinanzasVer_CensuraSinConsultarFinanzasYAUDitaSinValoresSensibles()
    {
        var finanzas = new Mock<IFinanzasService>(MockBehavior.Strict);
        var permisos = new Mock<IPermisoService>();
        permisos.Setup(x => x.TienePermisoAsync(ModuloSistema.Finanzas, AccionPermiso.Ver))
            .ReturnsAsync(false);
        var auditoria = new Mock<IAuditoriaService>();
        auditoria.Setup(x => x.RegistrarAsync(
                ModuloSistema.Inventario,
                AccionPermiso.Ver,
                It.Is<string>(descripcion => descripcion.Contains("censurados")),
                null,
                "ReporteInventarioValorizacion",
                null,
                null,
                null,
                "Censurado",
                null))
            .Returns(Task.CompletedTask);

        var controller = new ReportesInventarioValorizacionController(
            finanzas.Object,
            permisos.Object,
            auditoria.Object);

        var result = await controller.GetResumen();

        Assert.IsType<OkObjectResult>(result);
        finanzas.Verify(x => x.GetResumenAsync(), Times.Never);
        auditoria.VerifyAll();
        Assert.All(typeof(ReporteInventarioValorizacionResumenDto).GetProperties(), property =>
            Assert.Equal(typeof(decimal?), property.PropertyType));
    }

    [Fact]
    public async Task GetResumen_ConFinanzasVer_ConsultaResumenYAUDitaComoAutorizado()
    {
        var esperado = new FinanzasResumenDto
        {
            ValorInventarioCosto = 1250.50m,
            ValorInventarioCostoMercaderia = 1000m,
            ValorInventarioCostoInsumosAdministrativos = 250.50m,
            ValorPotencialVentaMercaderia = 1800m
        };
        var finanzas = new Mock<IFinanzasService>(MockBehavior.Strict);
        finanzas.Setup(x => x.GetResumenAsync()).ReturnsAsync(esperado);
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(x => x.TienePermisoAsync(ModuloSistema.Finanzas, AccionPermiso.Ver))
            .ReturnsAsync(true);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        auditoria.Setup(x => x.RegistrarAsync(
                ModuloSistema.Inventario,
                AccionPermiso.Ver,
                "Consulta autorizada de valorización de inventario.",
                null,
                "ReporteInventarioValorizacion",
                null,
                null,
                null,
                "Exito",
                null))
            .Returns(Task.CompletedTask);

        var controller = new ReportesInventarioValorizacionController(
            finanzas.Object,
            permisos.Object,
            auditoria.Object);

        var result = await controller.GetResumen();

        Assert.IsType<OkObjectResult>(result);
        finanzas.VerifyAll();
        permisos.VerifyAll();
        auditoria.VerifyAll();
    }
}
