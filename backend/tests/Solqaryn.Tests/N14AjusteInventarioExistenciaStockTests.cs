using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N14AjusteInventarioExistenciaStockTests
{
    [Fact]
    public void CalcularObjetivoReversion_RestaLaDiferenciaOriginalDelStockFisico()
    {
        var existencia = CrearExistencia(stockFisico: 12, stockReservado: 3);

        var objetivo = AjusteInventarioExistenciaStock.CalcularObjetivoReversion(existencia, diferenciaOriginal: 5);

        Assert.Equal(7, objetivo);
    }

    [Fact]
    public void CalcularObjetivoReversion_RechazaStockFisicoNegativoTrasReversion()
    {
        var existencia = CrearExistencia(stockFisico: 2, stockReservado: 0);

        var ex = Assert.Throws<BusinessRuleException>(() =>
            AjusteInventarioExistenciaStock.CalcularObjetivoReversion(existencia, diferenciaOriginal: 3));

        Assert.Contains("negativo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CalcularObjetivoReversion_RechazaObjetivoMenorAlStockReservado()
    {
        var existencia = CrearExistencia(stockFisico: 10, stockReservado: 8);

        var ex = Assert.Throws<BusinessRuleException>(() =>
            AjusteInventarioExistenciaStock.CalcularObjetivoReversion(existencia, diferenciaOriginal: 4));

        Assert.Contains("reservado", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ExistenciaVariante CrearExistencia(int stockFisico, int stockReservado)
    {
        var existencia = new ExistenciaVariante
        {
            ProductoVarianteId = 101,
            AlmacenId = 7
        };
        existencia.EstablecerStocks(
            stockFisico,
            stockReservado,
            stockTransito: 0,
            stockMinimo: 0,
            stockMaximo: null);
        return existencia;
    }
}
