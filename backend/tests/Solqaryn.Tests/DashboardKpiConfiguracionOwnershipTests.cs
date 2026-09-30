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

public sealed class DashboardKpiConfiguracionOwnershipTests
{
    private static AppDbContext CrearContexto() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n57f-kpi-ownership-{Guid.NewGuid():N}")
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
    public async Task GetConfiguracion_UsaFallbackSoloDelRolActual()
    {
        await using var db = CrearContexto();
        db.DashboardKpiConfiguraciones.AddRange(
            new DashboardKpiConfiguracion
            {
                MetricKey = DashboardKpiMetricKeys.VentasMes,
                RolId = 7,
                Habilitado = false,
                Orden = 3,
                EtiquetaVisible = "ROL-7"
            },
            new DashboardKpiConfiguracion
            {
                MetricKey = DashboardKpiMetricKeys.ComprasMes,
                RolId = 8,
                Habilitado = false,
                Orden = 2,
                EtiquetaVisible = "ROL-8-NO-VISIBLE"
            });
        await db.SaveChangesAsync();

        var controller = CrearController(db, usuarioId: 77, rolId: 7);
        var action = await controller.GetConfiguracionAsync(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var configuracion = Assert.IsAssignableFrom<IReadOnlyList<DashboardKpiConfiguracionDto>>(ok.Value);

        var ventas = Assert.Single(configuracion, x => x.MetricKey == DashboardKpiMetricKeys.VentasMes);
        Assert.False(ventas.Habilitado);
        Assert.Equal("ROL-7", ventas.EtiquetaVisible);

        Assert.DoesNotContain(configuracion, x => x.EtiquetaVisible == "ROL-8-NO-VISIBLE");
        var compras = Assert.Single(configuracion, x => x.MetricKey == DashboardKpiMetricKeys.ComprasMes);
        Assert.True(compras.Habilitado);
        Assert.Null(compras.EtiquetaVisible);
    }

    [Fact]
    public async Task GetConfiguracion_OverrideDeUsuarioPrevaleceSobreRol()
    {
        await using var db = CrearContexto();
        db.DashboardKpiConfiguraciones.AddRange(
            new DashboardKpiConfiguracion
            {
                MetricKey = DashboardKpiMetricKeys.VentasMes,
                RolId = 7,
                Habilitado = false,
                Orden = 9,
                EtiquetaVisible = "ROL"
            },
            new DashboardKpiConfiguracion
            {
                MetricKey = DashboardKpiMetricKeys.VentasMes,
                UsuarioId = 77,
                Habilitado = true,
                Orden = 1,
                EtiquetaVisible = "USUARIO"
            });
        await db.SaveChangesAsync();

        var controller = CrearController(db, usuarioId: 77, rolId: 7);
        var action = await controller.GetConfiguracionAsync(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var configuracion = Assert.IsAssignableFrom<IReadOnlyList<DashboardKpiConfiguracionDto>>(ok.Value);

        var ventas = Assert.Single(configuracion, x => x.MetricKey == DashboardKpiMetricKeys.VentasMes);
        Assert.True(ventas.Habilitado);
        Assert.Equal(1, ventas.Orden);
        Assert.Equal("USUARIO", ventas.EtiquetaVisible);
    }
}
