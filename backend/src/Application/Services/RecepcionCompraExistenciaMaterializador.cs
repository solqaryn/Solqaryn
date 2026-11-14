using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

public sealed record RecepcionCompraExistenciaTransicion(
    InventarioExistenciaClave Clave,
    int ProductoId,
    int CantidadAceptada,
    int StockAnterior,
    int StockNuevo);

/// <summary>
/// Materializa/revierte exclusivamente el saldo físico autoritativo de una recepción.
/// No genera Kardex ni cambia el estado documental: esas responsabilidades se coordinan
/// desde RecepcionCompraService dentro de la misma transacción de N2.3.D.
/// </summary>
public sealed class RecepcionCompraExistenciaMaterializador
{
    private readonly IExistenciaVarianteConcurrencyService _existencias;

    public RecepcionCompraExistenciaMaterializador(IExistenciaVarianteConcurrencyService existencias)
    {
        _existencias = existencias ?? throw new ArgumentNullException(nameof(existencias));
    }

    public async Task<IReadOnlyList<RecepcionCompraExistenciaTransicion>> AplicarAsync(
        IEnumerable<RecepcionCompraDetalle> detalles)
    {
        var demandas = ConstruirDemandas(detalles);
        var lockSet = await _existencias.BloquearYValidarExistenciasAsync(demandas, esDeduccion: false);
        var transiciones = new List<RecepcionCompraExistenciaTransicion>(lockSet.Demandas.Count);

        foreach (var demanda in lockSet.Demandas)
        {
            var existencia = lockSet.Existencias[demanda.Clave];
            var anterior = existencia.StockFisico;
            var nuevo = checked(anterior + demanda.Cantidad);
            await _existencias.AjustarStockFisicoPesimistaAsync(demanda.Clave, anterior, nuevo);
            transiciones.Add(new RecepcionCompraExistenciaTransicion(
                demanda.Clave,
                demanda.ProductoId,
                demanda.Cantidad,
                anterior,
                nuevo));
        }

        return transiciones;
    }

    public async Task<IReadOnlyList<RecepcionCompraExistenciaTransicion>> RevertirAsync(
        IEnumerable<RecepcionCompraDetalle> detalles)
    {
        var demandas = ConstruirDemandas(detalles);
        var lockSet = await _existencias.BloquearYValidarExistenciasAsync(demandas, esDeduccion: false);
        var transiciones = new List<RecepcionCompraExistenciaTransicion>(lockSet.Demandas.Count);

        foreach (var demanda in lockSet.Demandas)
        {
            var existencia = lockSet.Existencias[demanda.Clave];
            var anterior = existencia.StockFisico;
            var nuevo = anterior - demanda.Cantidad;
            if (nuevo < 0)
                throw new BusinessRuleException("La anulación no puede dejar el stock físico en negativo.");
            if (nuevo < existencia.StockReservado)
                throw new BusinessRuleException("La anulación no puede reducir el stock físico por debajo del stock reservado actual.");

            await _existencias.AjustarStockFisicoPesimistaAsync(demanda.Clave, anterior, nuevo);
            transiciones.Add(new RecepcionCompraExistenciaTransicion(
                demanda.Clave,
                demanda.ProductoId,
                demanda.Cantidad,
                anterior,
                nuevo));
        }

        return transiciones;
    }

    internal static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandas(
        IEnumerable<RecepcionCompraDetalle> detalles)
    {
        ArgumentNullException.ThrowIfNull(detalles);
        var demandas = new List<InventarioDemandaExistencia>();

        foreach (var detalle in detalles)
        {
            if (detalle is null)
                throw new BusinessRuleException("La recepción contiene un detalle nulo.");
            if (detalle.CantidadAceptada <= 0m)
                continue;
            if (!detalle.ProductoVarianteId.HasValue || detalle.ProductoVarianteId.Value <= 0)
                throw new BusinessRuleException("Toda cantidad aceptada debe estar asociada a una variante para materializar stock autoritativo.");
            if (decimal.Truncate(detalle.CantidadAceptada) != detalle.CantidadAceptada || detalle.CantidadAceptada > int.MaxValue)
                throw new BusinessRuleException("La cantidad aceptada debe expresarse en unidades enteras compatibles con ExistenciaVariante.");

            demandas.Add(new InventarioDemandaExistencia(
                detalle.ProductoId,
                detalle.ProductoVarianteId.Value,
                detalle.AlmacenId,
                detalle.UbicacionAlmacenId,
                decimal.ToInt32(detalle.CantidadAceptada)));
        }

        if (demandas.Count == 0)
            throw new BusinessRuleException("La recepción no contiene cantidad aceptada para materializar en inventario.");

        return demandas;
    }
}
