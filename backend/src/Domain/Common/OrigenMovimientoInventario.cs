using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Common;

/// <summary>
/// Contrato de dominio para identificar de forma tipada el documento empresarial
/// que origina un movimiento de inventario. Exige exactamente un origen válido.
/// La persistencia de sus FKs se incorpora por fases según el documento origen.
/// </summary>
public sealed record OrigenMovimientoInventario
{
    public TipoOrigenMovimientoInventario Tipo { get; }
    public int DocumentoId { get; }

    public int? CompraId => Tipo == TipoOrigenMovimientoInventario.Compra ? DocumentoId : null;
    public int? VentaId => Tipo == TipoOrigenMovimientoInventario.Venta ? DocumentoId : null;
    public int? ConsumoInsumoId => Tipo == TipoOrigenMovimientoInventario.ConsumoInsumo ? DocumentoId : null;
    public int? AjusteInventarioId => Tipo == TipoOrigenMovimientoInventario.AjusteInventario ? DocumentoId : null;
    public int? TransferenciaInventarioId => Tipo == TipoOrigenMovimientoInventario.TransferenciaInventario ? DocumentoId : null;
    public int? RecepcionCompraId => Tipo == TipoOrigenMovimientoInventario.RecepcionCompra ? DocumentoId : null;

    private OrigenMovimientoInventario(TipoOrigenMovimientoInventario tipo, int documentoId)
    {
        if (documentoId <= 0)
            throw new ArgumentOutOfRangeException(nameof(documentoId), "El identificador del documento origen debe ser mayor que cero.");

        Tipo = tipo;
        DocumentoId = documentoId;
    }

    public static OrigenMovimientoInventario DesdeCompra(int compraId) =>
        new(TipoOrigenMovimientoInventario.Compra, compraId);

    public static OrigenMovimientoInventario DesdeVenta(int ventaId) =>
        new(TipoOrigenMovimientoInventario.Venta, ventaId);

    public static OrigenMovimientoInventario DesdeConsumoInsumo(int consumoInsumoId) =>
        new(TipoOrigenMovimientoInventario.ConsumoInsumo, consumoInsumoId);

    public static OrigenMovimientoInventario DesdeAjusteInventario(int ajusteInventarioId) =>
        new(TipoOrigenMovimientoInventario.AjusteInventario, ajusteInventarioId);

    public static OrigenMovimientoInventario DesdeTransferenciaInventario(int transferenciaInventarioId) =>
        new(TipoOrigenMovimientoInventario.TransferenciaInventario, transferenciaInventarioId);

    public static OrigenMovimientoInventario DesdeRecepcionCompra(int recepcionCompraId) =>
        new(TipoOrigenMovimientoInventario.RecepcionCompra, recepcionCompraId);

    public static OrigenMovimientoInventario DesdeIds(
        int? compraId,
        int? ventaId,
        int? consumoInsumoId,
        int? ajusteInventarioId = null,
        int? transferenciaInventarioId = null,
        int? recepcionCompraId = null)
    {
        var cantidadOrigenes =
            (compraId.HasValue ? 1 : 0) +
            (ventaId.HasValue ? 1 : 0) +
            (consumoInsumoId.HasValue ? 1 : 0) +
            (ajusteInventarioId.HasValue ? 1 : 0) +
            (transferenciaInventarioId.HasValue ? 1 : 0) +
            (recepcionCompraId.HasValue ? 1 : 0);

        if (cantidadOrigenes != 1)
            throw new InvalidOperationException("Un movimiento originado por documento debe tener exactamente un origen tipado.");

        if (compraId.HasValue)
            return DesdeCompra(compraId.Value);
        if (ventaId.HasValue)
            return DesdeVenta(ventaId.Value);
        if (consumoInsumoId.HasValue)
            return DesdeConsumoInsumo(consumoInsumoId.Value);
        if (ajusteInventarioId.HasValue)
            return DesdeAjusteInventario(ajusteInventarioId.Value);
        if (transferenciaInventarioId.HasValue)
            return DesdeTransferenciaInventario(transferenciaInventarioId.Value);

        return DesdeRecepcionCompra(recepcionCompraId!.Value);
    }
}
