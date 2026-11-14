namespace Solqaryn.Domain.Enums;

/// <summary>
/// Tipo de documento empresarial que origina un movimiento de inventario.
/// La operación concreta (entrada, salida, anulación, reversión o transferencia)
/// se expresa mediante TipoMovimientoInventario/CausaMovimientoInventario.
/// </summary>
public enum TipoOrigenMovimientoInventario
{
    Compra = 1,
    Venta = 2,
    ConsumoInsumo = 3,
    AjusteInventario = 4,
    TransferenciaInventario = 5,
    RecepcionCompra = 6
}

/// <summary>
/// Ciclo de vida del documento empresarial de ajuste de inventario.
/// </summary>
public enum EstadoAjusteInventario
{
    Borrador = 1,
    Confirmado = 2,
    Anulado = 3
}
