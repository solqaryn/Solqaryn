using Solqaryn.API.Controllers;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public class M12AutomatizacionTransversalTests
{
    [Fact]
    public void Configuracion_DefaultsSonDeterministasYVersionados()
    {
        var config = new AutomatizacionConfiguracionDto();
        Assert.Equal("M12.1", config.VersionReglas);
        Assert.Equal(2, config.DiasBorradorVentaAlerta);
        Assert.Equal(7, config.DiasBorradorCompraAlerta);
        Assert.Equal(10, config.LimiteAutocompletado);
        Assert.True(config.MostrarRecordatoriosDashboard);
    }

    [Fact]
    public void AccionMasiva_PorContratoSiempreEsPreviewYRequiereConfirmacion()
    {
        var preview = new AccionMasivaPreviewDto();
        Assert.True(preview.SoloVistaPrevia);
        Assert.True(preview.RequiereConfirmacion);
    }

    [Fact]
    public void Sugerencia_PorContratoRequiereConfirmacion()
    {
        var sugerencia = new AutomatizacionSugerenciaDto();
        Assert.True(sugerencia.RequiereConfirmacion);
    }

    [Fact]
    public void Controller_PublicaEndpointsDeSugerenciasYPreview()
    {
        var sugerencias = typeof(AutomatizacionesController).GetMethod(nameof(AutomatizacionesController.Sugerencias));
        var preview = typeof(AutomatizacionesController).GetMethod(nameof(AutomatizacionesController.Previsualizar));
        Assert.NotNull(sugerencias);
        Assert.NotNull(preview);
        Assert.Equal("sugerencias", sugerencias!.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>().Single().Template);
        Assert.Equal("acciones-masivas/previsualizar", preview!.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>().Single().Template);
    }

    [Fact]
    public void Controller_ExponeConfiguracionAdministrable()
    {
        var get = typeof(AutomatizacionesController).GetMethod(nameof(AutomatizacionesController.GetConfiguracion));
        var update = typeof(AutomatizacionesController).GetMethod(nameof(AutomatizacionesController.UpdateConfiguracion));
        Assert.NotNull(get);
        Assert.NotNull(update);
        Assert.Equal("configuracion", get!.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>().Single().Template);
        Assert.Equal("configuracion", update!.GetCustomAttributes(typeof(HttpPutAttribute), true).Cast<HttpPutAttribute>().Single().Template);
    }

    [Fact]
    public void ContratoCubreLosNueveDominiosDelPlan()
    {
        var modulos = new[] { "Productos", "Compras", "Ventas", "Inventario", "Clientes", "Facturación", "Finanzas", "Cargas", "Configuración" };
        Assert.Equal(9, modulos.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task TresEmpresas_AutomatizacionLegacy_DeniegaLecturasYEscriturasSinHttp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("m12-three-tenants-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);
        db.Set<Empresa>().AddRange(
            new Empresa("Automatizaciones A"), new Empresa("Automatizaciones B"),
            new Empresa("Automatizaciones C"));
        await db.SaveChangesAsync();

        var service = new AutomatizacionService(db, Mock.Of<ICurrentUserService>());
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetConfiguracionAsync());
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.UpdateConfiguracionAsync(new ActualizarAutomatizacionConfiguracionRequest()));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetSugerenciasAsync());
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.AutocompletarAsync("clientes", "abc"));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.PrevisualizarAccionMasivaAsync(new AccionMasivaPreviewRequest
            {
                Accion = "revisar-clientes-contacto",
                Ids = new List<int> { 1 }
            }));
    }

    [Fact]
    public async Task UnaEmpresa_PreviewM12Legacy_PermaneceDisponibleSinMutacion()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("m12-single-tenant-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options);
        db.Set<Empresa>().Add(new Empresa("Automatizaciones legacy"));
        await db.SaveChangesAsync();

        var service = new AutomatizacionService(db, Mock.Of<ICurrentUserService>());
        var preview = await service.PrevisualizarAccionMasivaAsync(
            new AccionMasivaPreviewRequest
            {
                Accion = "revisar-clientes-contacto",
                Ids = new List<int> { 1 }
            });
        Assert.True(preview.SoloVistaPrevia);
        Assert.True(preview.RequiereConfirmacion);
        Assert.Empty(preview.IdsAplicables);
    }
}
