using Solqaryn.Domain.ValueObjects;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N110CosteoPromedioPonderadoTests
{
    [Fact]
    public void Calcula_mismo_promedio_ponderado_y_redondeo_historico()
    {
        var costo = CosteoPromedioPonderado.CalcularCostoUnitario(
            stockAnterior: 3,
            costoAnterior: 10m,
            cantidadEntrada: 2,
            valorEntrada: 27.55m);

        Assert.Equal(11.51m, costo);
    }

    [Fact]
    public void Sin_stock_anterior_el_costo_es_el_promedio_de_la_entrada()
    {
        var costo = CosteoPromedioPonderado.CalcularCostoUnitario(
            stockAnterior: 0,
            costoAnterior: 0m,
            cantidadEntrada: 3,
            valorEntrada: 37.02m);

        Assert.Equal(12.34m, costo);
    }

    [Fact]
    public void Rechaza_entradas_invalidas()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CosteoPromedioPonderado.CalcularCostoUnitario(-1, 0m, 1, 10m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CosteoPromedioPonderado.CalcularCostoUnitario(0, -1m, 1, 10m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CosteoPromedioPonderado.CalcularCostoUnitario(0, 0m, 0, 10m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CosteoPromedioPonderado.CalcularCostoUnitario(0, 0m, 1, -1m));
    }
}
