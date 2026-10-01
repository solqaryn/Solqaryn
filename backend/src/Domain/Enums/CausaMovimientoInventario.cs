namespace Solqaryn.Domain.Enums;

public enum CausaMovimientoInventario
{
    NoEspecificada = 0,
    Compra = 1,
    Venta = 2,
    ConsumoAdministrativo = 3,
    AjusteManual = 4,
    AnulacionCompra = 5,
    AnulacionVenta = 6,
    ReversionConsumo = 7,
    TransferenciaDespacho = 8,
    TransferenciaRecepcion = 9,
    TransferenciaCancelacion = 10,
    RecepcionCompra = 11,
    AnulacionRecepcionCompra = 12
}
