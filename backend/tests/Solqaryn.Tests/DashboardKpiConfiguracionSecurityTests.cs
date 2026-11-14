using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class DashboardKpiConfiguracionSecurityTests
{
    private static AppDbContext CrearContexto() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n57f-kpi-security-{Guid.NewGuid():N}")
            .Options);

    private static DashboardKpiConfiguracionController CrearController(
        AppDbContext db,
        int usuarioId,
        int rolId)
    {
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(usuarioId, rolId, "Operador", false));

        return new DashboardKpiConfiguracionController(
            db,
            scope.Object,
            new Mock<IDashboardService>().Object);
    }

    [Fact]
    public async Task PutConfiguracion_NoModificaNiEliminaConfiguracionDeOtroUsuario()
    {
        await using var db = CrearContexto();
        db.DashboardKpiConfiguraciones.Add(new DashboardKpiConfiguracion
        {
            MetricKey = DashboardKpiMetricKeys.IngresosMes,
            UsuarioId = 88,
            Habilitado = false,
            Orden = 9,
            EtiquetaVisible = "PRIVADO-88"
        });
        await db.SaveChangesAsync();

        var controller = CrearController(db, usuarioId: 77, rolId: 7);
        var result = await controller.PutConfiguracionAsync(
            new[]
            {
                new DashboardKpiConfiguracionDto
                {
                    MetricKey = DashboardKpiMetricKeys.IngresosMes,
                    Habilitado = true,
                    Orden = 1,
                    EtiquetaVisible = "PROPIO-77"
                }
            },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);

        var ajena = await db.DashboardKpiConfiguraciones.SingleAsync(x => x.UsuarioId == 88);
        Assert.False(ajena.Habilitado);
        Assert.Equal(9, ajena.Orden);
        Assert.Equal("PRIVADO-88", ajena.EtiquetaVisible);

        var propia = await db.DashboardKpiConfiguraciones.SingleAsync(x => x.UsuarioId == 77);
        Assert.True(propia.Habilitado);
        Assert.Equal(1, propia.Orden);
        Assert.Equal("PROPIO-77", propia.EtiquetaVisible);
    }

    [Fact]
    public async Task GetConfiguracion_NoExponeOverrideDeOtroUsuario()
    {
        await using var db = CrearContexto();
        db.DashboardKpiConfiguraciones.Add(new DashboardKpiConfiguracion
        {
            MetricKey = DashboardKpiMetricKeys.IngresosMes,
            UsuarioId = 88,
            Habilitado = false,
            Orden = 0,
            EtiquetaVisible = "SECRETO-OTRO-USUARIO"
        });
        await db.SaveChangesAsync();

        var controller = CrearController(db, usuarioId: 77, rolId: 7);
        var action = await controller.GetConfiguracionAsync(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var configuracion = Assert.IsAssignableFrom<IReadOnlyList<DashboardKpiConfiguracionDto>>(ok.Value);

        Assert.DoesNotContain(configuracion, x => x.EtiquetaVisible == "SECRETO-OTRO-USUARIO");
        var ingresos = Assert.Single(configuracion, x => x.MetricKey == DashboardKpiMetricKeys.IngresosMes);
        Assert.True(ingresos.Habilitado);
        Assert.Null(ingresos.EtiquetaVisible);
    }

    [Fact]
    public async Task PutConfiguracion_MetricKeyDesconocida_FallaCerradoYSinPersistir()
    {
        await using var db = CrearContexto();
        var controller = CrearController(db, usuarioId: 77, rolId: 7);

        var action = await controller.PutConfiguracionAsync(
            new[]
            {
                new DashboardKpiConfiguracionDto
                {
                    MetricKey = "FORMULA_ARBITRARIA",
                    Habilitado = true,
                    Orden = 0
                }
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(action.Result);
        Assert.Empty(await db.DashboardKpiConfiguraciones.ToListAsync());
    }
}
