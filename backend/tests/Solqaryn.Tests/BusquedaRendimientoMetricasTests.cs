using Solqaryn.API.Filters;
using Xunit;

namespace Solqaryn.Tests;

public class BusquedaRendimientoMetricasTests
{
    [Fact]
    public void Registrar_CalculaP50YP95PorRuta()
    {
        var metricas = new BusquedaRendimientoMetricas();
        BusquedaRendimientoResumen? resumen = null;

        foreach (var duracion in Enumerable.Range(1, 100).Select(x => (long)x))
            resumen = metricas.Registrar("/productos", duracion);

        Assert.NotNull(resumen);
        Assert.Equal(100, resumen!.Muestras);
        Assert.Equal(50, resumen.P50Ms);
        Assert.Equal(95, resumen.P95Ms);
    }

    [Fact]
    public void Registrar_MantieneVentanaMaximaDeDoscientasMuestras()
    {
        var metricas = new BusquedaRendimientoMetricas();
        BusquedaRendimientoResumen? resumen = null;

        foreach (var duracion in Enumerable.Range(1, 250).Select(x => (long)x))
            resumen = metricas.Registrar("/clientes/buscar", duracion);

        Assert.NotNull(resumen);
        Assert.Equal(200, resumen!.Muestras);
        Assert.Equal(150, resumen.P50Ms);
        Assert.Equal(240, resumen.P95Ms);
    }

    [Fact]
    public void Registrar_AislaLasMetricasPorRuta()
    {
        var metricas = new BusquedaRendimientoMetricas();

        metricas.Registrar("/productos", 10);
        metricas.Registrar("/productos", 20);
        var clientes = metricas.Registrar("/clientes/buscar", 99);

        Assert.Equal(1, clientes.Muestras);
        Assert.Equal(99, clientes.P50Ms);
        Assert.Equal(99, clientes.P95Ms);
    }
}
