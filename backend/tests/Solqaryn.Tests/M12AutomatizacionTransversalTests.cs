using Solqaryn.API.Controllers;
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
}
