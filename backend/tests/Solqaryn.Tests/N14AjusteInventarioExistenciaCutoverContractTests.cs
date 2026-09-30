using System.Reflection;
using Solqaryn.Application.Services;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N14AjusteInventarioExistenciaCutoverContractTests
{
    [Fact]
    public void Cutover_service_expone_operaciones_fisicas_de_confirmacion_y_reversion()
    {
        var type = typeof(AjusteInventarioExistenciaCutoverService);

        var bloquearConfirmacion = type.GetMethod(
            nameof(AjusteInventarioExistenciaCutoverService.BloquearParaConfirmacionAsync),
            BindingFlags.Instance | BindingFlags.Public);
        var bloquearReversion = type.GetMethod(
            nameof(AjusteInventarioExistenciaCutoverService.BloquearParaReversionAsync),
            BindingFlags.Instance | BindingFlags.Public);
        var aplicarConfirmacion = type.GetMethod(
            nameof(AjusteInventarioExistenciaCutoverService.AplicarObjetivoConfirmacionAsync),
            BindingFlags.Instance | BindingFlags.Public);
        var aplicarConfirmacionConSnapshot = type.GetMethod(
            nameof(AjusteInventarioExistenciaCutoverService.AplicarConfirmacionConSnapshotAsync),
            BindingFlags.Instance | BindingFlags.Public);
        var aplicarReversion = type.GetMethod(
            nameof(AjusteInventarioExistenciaCutoverService.AplicarReversionAsync),
            BindingFlags.Instance | BindingFlags.Public);
        var aplicarReversionConSnapshot = type.GetMethod(
            nameof(AjusteInventarioExistenciaCutoverService.AplicarReversionConSnapshotAsync),
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(bloquearConfirmacion);
        Assert.NotNull(bloquearReversion);
        Assert.NotNull(aplicarConfirmacion);
        Assert.NotNull(aplicarConfirmacionConSnapshot);
        Assert.NotNull(aplicarReversion);
        Assert.NotNull(aplicarReversionConSnapshot);
        Assert.Equal(typeof(Task<int>), aplicarConfirmacion!.ReturnType);
        Assert.Equal(typeof(Task<AjusteInventarioExistenciaTransicion>), aplicarConfirmacionConSnapshot!.ReturnType);
        Assert.Equal(typeof(Task<int>), aplicarReversion!.ReturnType);
        Assert.Equal(typeof(Task<AjusteInventarioExistenciaTransicion>), aplicarReversionConSnapshot!.ReturnType);
    }

    [Fact]
    public void Transicion_fisica_conserva_stock_anterior_nuevo_y_diferencia()
    {
        var transicion = new AjusteInventarioExistenciaTransicion(
            StockAnterior: 10,
            StockNuevo: 15,
            Diferencia: 5);

        Assert.Equal(10, transicion.StockAnterior);
        Assert.Equal(15, transicion.StockNuevo);
        Assert.Equal(5, transicion.Diferencia);
    }
}
