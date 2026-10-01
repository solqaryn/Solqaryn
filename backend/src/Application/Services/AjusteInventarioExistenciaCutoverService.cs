using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

public sealed record AjusteInventarioExistenciaTransicion(
    int StockAnterior,
    int StockNuevo,
    int Diferencia);

/// <summary>
/// Orquesta el cutover de AjusteInventario hacia ExistenciaVariante como autoridad
/// física de stock. Mantiene los locks y escrituras pesimistas fuera del bridge
/// legacy basado en ProductoVariante.Cantidad.
/// </summary>
public sealed class AjusteInventarioExistenciaCutoverService
{
    private readonly IExistenciaVarianteConcurrencyService _existenciaConcurrency;

    public AjusteInventarioExistenciaCutoverService(
        IExistenciaVarianteConcurrencyService existenciaConcurrency)
    {
        _existenciaConcurrency = existenciaConcurrency;
    }

    public Task<InventarioExistenciaLockSet> BloquearParaConfirmacionAsync(
        IEnumerable<AjusteInventarioDetalle> detalles)
    {
        var demandas = AjusteInventarioExistenciaStock.CrearDemandas(detalles);
        return _existenciaConcurrency.BloquearYValidarExistenciasAsync(
            demandas,
            esDeduccion: false);
    }

    public Task<InventarioExistenciaLockSet> BloquearParaReversionAsync(
        IEnumerable<AjusteInventarioDetalle> detalles)
    {
        var demandas = AjusteInventarioExistenciaStock.CrearDemandas(detalles);
        return _existenciaConcurrency.BloquearExistenciasParaReversionAsync(demandas);
    }

    public async Task<AjusteInventarioExistenciaTransicion> AplicarConfirmacionConSnapshotAsync(
        InventarioExistenciaLockSet lockSet,
        AjusteInventarioDetalle detalle)
    {
        var existencia = AjusteInventarioExistenciaStock.ObtenerExistencia(lockSet, detalle);
        AjusteInventarioExistenciaStock.ValidarObjetivoContraReservado(
            existencia,
            detalle.CantidadObjetivo);

        var stockAnterior = existencia.StockFisico;
        var diferencia = AjusteInventarioExistenciaStock.CalcularDiferencia(
            stockAnterior,
            detalle.CantidadObjetivo);
        if (diferencia == 0)
            throw new BusinessRuleException("El detalle no produce una diferencia real sobre el stock físico autoritativo.");

        var clave = AjusteInventarioExistenciaContext.CrearClave(detalle);
        await _existenciaConcurrency.AjustarStockFisicoPesimistaAsync(
            clave,
            stockAnterior,
            detalle.CantidadObjetivo);

        return new AjusteInventarioExistenciaTransicion(
            stockAnterior,
            detalle.CantidadObjetivo,
            diferencia);
    }

    public async Task<int> AplicarObjetivoConfirmacionAsync(
        InventarioExistenciaLockSet lockSet,
        AjusteInventarioDetalle detalle)
    {
        var transicion = await AplicarConfirmacionConSnapshotAsync(lockSet, detalle);
        return transicion.Diferencia;
    }

    public async Task<AjusteInventarioExistenciaTransicion> AplicarReversionConSnapshotAsync(
        InventarioExistenciaLockSet lockSet,
        AjusteInventarioDetalle detalle)
    {
        var existencia = AjusteInventarioExistenciaStock.ObtenerExistencia(lockSet, detalle);
        var diferenciaOriginal = detalle.DiferenciaSnapshot
            ?? throw new BusinessRuleException("El ajuste no contiene una diferencia histórica válida para revertir.");
        var stockAnterior = existencia.StockFisico;
        var objetivo = AjusteInventarioExistenciaStock.CalcularObjetivoReversion(
            existencia,
            diferenciaOriginal);

        var clave = AjusteInventarioExistenciaContext.CrearClave(detalle);
        await _existenciaConcurrency.AjustarStockFisicoPesimistaAsync(
            clave,
            stockAnterior,
            objetivo);

        return new AjusteInventarioExistenciaTransicion(
            stockAnterior,
            objetivo,
            -diferenciaOriginal);
    }

    public async Task<int> AplicarReversionAsync(
        InventarioExistenciaLockSet lockSet,
        AjusteInventarioDetalle detalle)
    {
        var transicion = await AplicarReversionConSnapshotAsync(lockSet, detalle);
        return transicion.StockNuevo;
    }
}
