using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

public sealed record TransferenciaInventarioExistenciaTransicion(
    InventarioExistenciaClave Clave,
    int StockAnterior,
    int StockNuevo,
    int Cantidad);

public sealed record TransferenciaInventarioTransitoTransicion(
    InventarioExistenciaClave Clave,
    int StockFisicoAnterior,
    int StockFisicoNuevo,
    int StockTransitoAnterior,
    int StockTransitoNuevo,
    int CantidadFisica);

/// <summary>
/// Orquesta despacho, recepción y reversión contra ExistenciaVariante como autoridad única.
/// Los locks se adquieren en un único conjunto ordenado antes de mutar origen o destino.
/// </summary>
public sealed class TransferenciaInventarioExistenciaService
{
    private readonly IExistenciaVarianteConcurrencyService _existencias;

    public TransferenciaInventarioExistenciaService(IExistenciaVarianteConcurrencyService existencias)
    {
        _existencias = existencias;
    }

    public Task<InventarioExistenciaLockSet> BloquearParaDespachoAsync(TransferenciaInventario transferencia)
    {
        var demandas = TransferenciaInventarioExistenciaContext.ConstruirDemandasBloqueoDespacho(transferencia);
        return _existencias.BloquearYValidarExistenciasAsync(demandas, esDeduccion: false);
    }

    public Task<InventarioExistenciaLockSet> BloquearParaCancelacionEnTransitoAsync(TransferenciaInventario transferencia)
    {
        var demandas = TransferenciaInventarioExistenciaContext.ConstruirDemandasBloqueoDespacho(transferencia);
        return _existencias.BloquearYValidarExistenciasAsync(demandas, esDeduccion: false);
    }

    public async Task<IReadOnlyList<TransferenciaInventarioExistenciaTransicion>> AplicarDespachoAsync(
        InventarioExistenciaLockSet lockSet)
    {
        ArgumentNullException.ThrowIfNull(lockSet);
        if (lockSet.Demandas.Count == 0)
            throw new BusinessRuleException("No existen demandas físicas bloqueadas para el despacho.");

        var transiciones = new List<TransferenciaInventarioExistenciaTransicion>(lockSet.Demandas.Count);
        foreach (var demanda in lockSet.Demandas)
        {
            var existencia = ObtenerExistencia(lockSet, demanda.Clave, "despacho");
            ValidarDisponible(existencia, demanda.Cantidad);
            var stockAnterior = existencia.StockFisico;
            var stockNuevo = checked(stockAnterior - demanda.Cantidad);

            await _existencias.AjustarStockFisicoPesimistaAsync(
                demanda.Clave,
                stockAnterior,
                stockNuevo);

            transiciones.Add(new TransferenciaInventarioExistenciaTransicion(
                demanda.Clave,
                stockAnterior,
                stockNuevo,
                demanda.Cantidad));
        }

        return transiciones;
    }

    public async Task<IReadOnlyList<TransferenciaInventarioTransitoTransicion>> AplicarDespachoCompletoAsync(
        InventarioExistenciaLockSet lockSet,
        TransferenciaInventario transferencia)
    {
        ArgumentNullException.ThrowIfNull(lockSet);
        ArgumentNullException.ThrowIfNull(transferencia);

        var origen = Consolidar(TransferenciaInventarioExistenciaContext.ConstruirDemandasDespacho(transferencia));
        var destino = Consolidar(TransferenciaInventarioExistenciaContext.ConstruirDemandasTransitoDestino(transferencia));
        var transiciones = new List<TransferenciaInventarioTransitoTransicion>(origen.Count + destino.Count);

        foreach (var demanda in origen)
        {
            var existencia = ObtenerExistencia(lockSet, demanda.Clave, "despacho de origen");
            ValidarDisponible(existencia, demanda.Cantidad);
            var fisicoAnterior = existencia.StockFisico;
            var fisicoNuevo = checked(fisicoAnterior - demanda.Cantidad);
            var transito = existencia.StockTransito;

            await _existencias.AjustarStocksPesimistaAsync(
                demanda.Clave,
                fisicoAnterior,
                fisicoNuevo,
                transito,
                transito);

            transiciones.Add(new TransferenciaInventarioTransitoTransicion(
                demanda.Clave,
                fisicoAnterior,
                fisicoNuevo,
                transito,
                transito,
                -demanda.Cantidad));
        }

        foreach (var demanda in destino)
        {
            var existencia = ObtenerExistencia(lockSet, demanda.Clave, "tránsito de destino");
            var fisico = existencia.StockFisico;
            var transitoAnterior = existencia.StockTransito;
            var transitoNuevo = checked(transitoAnterior + demanda.Cantidad);

            await _existencias.AjustarStocksPesimistaAsync(
                demanda.Clave,
                fisico,
                fisico,
                transitoAnterior,
                transitoNuevo);

            transiciones.Add(new TransferenciaInventarioTransitoTransicion(
                demanda.Clave,
                fisico,
                fisico,
                transitoAnterior,
                transitoNuevo,
                0));
        }

        return transiciones;
    }

    public Task<InventarioExistenciaLockSet> BloquearParaRecepcionAsync(TransferenciaInventario transferencia)
    {
        var demandas = TransferenciaInventarioExistenciaContext.ConstruirDemandasCierreTransitoDestino(transferencia);
        return _existencias.BloquearYValidarExistenciasAsync(demandas, esDeduccion: false);
    }

    public async Task<IReadOnlyList<TransferenciaInventarioTransitoTransicion>> AplicarRecepcionAsync(
        InventarioExistenciaLockSet lockSet,
        TransferenciaInventario transferencia)
    {
        ArgumentNullException.ThrowIfNull(lockSet);
        ArgumentNullException.ThrowIfNull(transferencia);

        var cierreTransito = Consolidar(
            TransferenciaInventarioExistenciaContext.ConstruirDemandasCierreTransitoDestino(transferencia));
        var ingresos = Consolidar(
                TransferenciaInventarioExistenciaContext.ConstruirDemandasIngresoDestino(transferencia))
            .ToDictionary(x => x.Clave, x => x.Cantidad);
        var transiciones = new List<TransferenciaInventarioTransitoTransicion>(cierreTransito.Count);

        foreach (var demanda in cierreTransito)
        {
            var existencia = ObtenerExistencia(lockSet, demanda.Clave, "recepción de destino");
            if (existencia.StockTransito < demanda.Cantidad)
                throw new BusinessRuleException("La recepción intentaría cerrar más tránsito del registrado en destino.");

            var recibido = ingresos.GetValueOrDefault(demanda.Clave, 0);
            var fisicoAnterior = existencia.StockFisico;
            var fisicoNuevo = checked(fisicoAnterior + recibido);
            var transitoAnterior = existencia.StockTransito;
            var transitoNuevo = checked(transitoAnterior - demanda.Cantidad);

            await _existencias.AjustarStocksPesimistaAsync(
                demanda.Clave,
                fisicoAnterior,
                fisicoNuevo,
                transitoAnterior,
                transitoNuevo);

            transiciones.Add(new TransferenciaInventarioTransitoTransicion(
                demanda.Clave,
                fisicoAnterior,
                fisicoNuevo,
                transitoAnterior,
                transitoNuevo,
                recibido));
        }

        return transiciones;
    }

    public async Task<IReadOnlyList<TransferenciaInventarioTransitoTransicion>> AplicarCancelacionEnTransitoAsync(
        InventarioExistenciaLockSet lockSet,
        TransferenciaInventario transferencia)
    {
        ArgumentNullException.ThrowIfNull(lockSet);
        ArgumentNullException.ThrowIfNull(transferencia);

        var origen = Consolidar(TransferenciaInventarioExistenciaContext.ConstruirDemandasDespacho(transferencia));
        var destino = Consolidar(TransferenciaInventarioExistenciaContext.ConstruirDemandasTransitoDestino(transferencia));
        var transiciones = new List<TransferenciaInventarioTransitoTransicion>(origen.Count + destino.Count);

        foreach (var demanda in origen)
        {
            var existencia = ObtenerExistencia(lockSet, demanda.Clave, "reversión de origen");
            var fisicoAnterior = existencia.StockFisico;
            var fisicoNuevo = checked(fisicoAnterior + demanda.Cantidad);
            var transito = existencia.StockTransito;

            await _existencias.AjustarStocksPesimistaAsync(
                demanda.Clave,
                fisicoAnterior,
                fisicoNuevo,
                transito,
                transito);

            transiciones.Add(new TransferenciaInventarioTransitoTransicion(
                demanda.Clave,
                fisicoAnterior,
                fisicoNuevo,
                transito,
                transito,
                demanda.Cantidad));
        }

        foreach (var demanda in destino)
        {
            var existencia = ObtenerExistencia(lockSet, demanda.Clave, "cierre de tránsito por cancelación");
            if (existencia.StockTransito < demanda.Cantidad)
                throw new BusinessRuleException("La cancelación intentaría revertir más tránsito del registrado en destino.");

            var fisico = existencia.StockFisico;
            var transitoAnterior = existencia.StockTransito;
            var transitoNuevo = checked(transitoAnterior - demanda.Cantidad);

            await _existencias.AjustarStocksPesimistaAsync(
                demanda.Clave,
                fisico,
                fisico,
                transitoAnterior,
                transitoNuevo);

            transiciones.Add(new TransferenciaInventarioTransitoTransicion(
                demanda.Clave,
                fisico,
                fisico,
                transitoAnterior,
                transitoNuevo,
                0));
        }

        return transiciones;
    }

    private static List<InventarioDemandaExistencia> Consolidar(IEnumerable<InventarioDemandaExistencia> demandas) =>
        demandas
            .GroupBy(d => d.Clave)
            .Select(g => new InventarioDemandaExistencia(
                g.Select(x => x.ProductoId).Distinct().Single(),
                g.Key.ProductoVarianteId,
                g.Key.AlmacenId,
                g.Key.UbicacionAlmacenId,
                g.Sum(x => x.Cantidad)))
            .OrderBy(d => d.ProductoVarianteId)
            .ThenBy(d => d.AlmacenId)
            .ThenBy(d => d.UbicacionAlmacenId ?? 0)
            .ToList();

    private static ExistenciaVariante ObtenerExistencia(
        InventarioExistenciaLockSet lockSet,
        InventarioExistenciaClave clave,
        string operacion) =>
        lockSet.Existencias.TryGetValue(clave, out var existencia)
            ? existencia
            : throw new BusinessRuleException($"No se encontró la existencia física bloqueada para {operacion}.");

    private static void ValidarDisponible(ExistenciaVariante existencia, int cantidad)
    {
        if (existencia.StockDisponible < cantidad)
            throw new BusinessRuleException(
                $"Stock insuficiente para despachar: disponible {existencia.StockDisponible}, requerido {cantidad}.");
        if (existencia.StockFisico - cantidad < existencia.StockReservado)
            throw new BusinessRuleException("El despacho dejaría el stock físico por debajo de la reserva vigente.");
    }
}
