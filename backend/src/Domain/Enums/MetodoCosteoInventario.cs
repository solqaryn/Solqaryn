namespace Solqaryn.Domain.Enums;

/// <summary>
/// Política contable utilizada para valorar movimientos de inventario.
/// ERP-N1.10 conserva PromedioPonderado como default de compatibilidad.
/// </summary>
public enum MetodoCosteoInventario
{
    PromedioPonderado = 1,
    FIFO = 2,
    Estandar = 3
}
