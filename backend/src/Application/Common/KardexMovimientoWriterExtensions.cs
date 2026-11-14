using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Common;

/// <summary>
/// Operaciones empresariales tipadas para registrar movimientos de Kardex sin
/// repetir convenciones de correlación en cada servicio consumidor.
/// </summary>
public static class KardexMovimientoWriterExtensions
{
    public static Task RegistrarCompraConfirmadaAsync(
        this IKardexMovimientoWriter writer,
        MovimientoInventario movimiento,
        int compraId) =>
        RegistrarCorrelacionadoAsync(
            writer,
            movimiento,
            OrigenMovimientoInventario.DesdeCompra(compraId),
            KardexCorrelationId.CompraConfirmar(compraId));

    public static Task RegistrarCompraAnuladaAsync(
        this IKardexMovimientoWriter writer,
        MovimientoInventario movimiento,
        int compraId) =>
        RegistrarCorrelacionadoAsync(
            writer,
            movimiento,
            OrigenMovimientoInventario.DesdeCompra(compraId),
            KardexCorrelationId.CompraAnular(compraId));

    public static Task RegistrarVentaConfirmadaAsync(
        this IKardexMovimientoWriter writer,
        MovimientoInventario movimiento,
        int ventaId) =>
        RegistrarCorrelacionadoAsync(
            writer,
            movimiento,
            OrigenMovimientoInventario.DesdeVenta(ventaId),
            KardexCorrelationId.VentaConfirmar(ventaId));

    public static Task RegistrarVentaAnuladaAsync(
        this IKardexMovimientoWriter writer,
        MovimientoInventario movimiento,
        int ventaId) =>
        RegistrarCorrelacionadoAsync(
            writer,
            movimiento,
            OrigenMovimientoInventario.DesdeVenta(ventaId),
            KardexCorrelationId.VentaAnular(ventaId));

    public static Task RegistrarConsumoConfirmadoAsync(
        this IKardexMovimientoWriter writer,
        MovimientoInventario movimiento,
        int consumoInsumoId) =>
        RegistrarCorrelacionadoAsync(
            writer,
            movimiento,
            OrigenMovimientoInventario.DesdeConsumoInsumo(consumoInsumoId),
            KardexCorrelationId.ConsumoConfirmar(consumoInsumoId));

    public static Task RegistrarConsumoAnuladoAsync(
        this IKardexMovimientoWriter writer,
        MovimientoInventario movimiento,
        int consumoInsumoId) =>
        RegistrarCorrelacionadoAsync(
            writer,
            movimiento,
            OrigenMovimientoInventario.DesdeConsumoInsumo(consumoInsumoId),
            KardexCorrelationId.ConsumoAnular(consumoInsumoId));

    private static Task RegistrarCorrelacionadoAsync(
        IKardexMovimientoWriter writer,
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(movimiento);
        return writer.RegistrarCorrelacionadoAsync(movimiento, origen, correlationId);
    }
}
