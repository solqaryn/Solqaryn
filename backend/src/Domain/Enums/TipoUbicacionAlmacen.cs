namespace Solqaryn.Domain.Enums;

/// <summary>
/// Clasificación estable de la topología interna de un almacén.
/// Los valores numéricos forman parte del contrato persistente desde ERP-N1.3.
/// </summary>
public enum TipoUbicacionAlmacen
{
    Pasillo = 1,
    Estante = 2,
    Rack = 3,
    Seccion = 4,
    Bin = 5,
    Otra = 6
}
