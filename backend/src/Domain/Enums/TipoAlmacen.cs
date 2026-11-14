namespace Solqaryn.Domain.Enums;

/// <summary>
/// Clasificación operacional estable de un almacén en ERP-N1.
/// Las ubicaciones internas y la autoridad de existencias se incorporan en N1.3/N1.4.
/// </summary>
public enum TipoAlmacen
{
    Tienda = 1,
    Bodega = 2,
    Transito = 3,
    Devolucion = 4,
    Cuarentena = 5
}
