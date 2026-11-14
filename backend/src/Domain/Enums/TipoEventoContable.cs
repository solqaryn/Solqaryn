namespace Solqaryn.Domain.Enums;

/// <summary>
/// Tipos de evento de negocio admitidos por el motor de contabilización.
/// Los valores son estables porque forman parte del contrato entre módulos y reglas contables.
/// </summary>
public enum TipoEventoContable
{
    Venta = 1,
    Compra = 2,
    Cobro = 3,
    Pago = 4,
    MovimientoInventario = 5,
    CostoVenta = 6,
    DevolucionCliente = 7,
    DevolucionProveedor = 8,
    AjusteInventario = 9,
    MovimientoCaja = 10,
    MovimientoBanco = 11
}
