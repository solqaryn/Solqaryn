namespace Solqaryn.Domain.Enums;

/// <summary>
/// Clasificación principal del plan de cuentas contable.
/// Los valores son estables porque se persistirán como parte del contrato de dominio.
/// </summary>
public enum TipoCuentaContable
{
    Activo = 1,
    Pasivo = 2,
    Patrimonio = 3,
    Ingreso = 4,
    Gasto = 5,
    Costo = 6
}
