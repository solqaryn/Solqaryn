using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Cuenta jerárquica del plan contable. La jerarquía se expresa mediante CuentaPadreId/Subcuentas
/// y la clasificación raíz mediante Tipo.
/// </summary>
public class CuentaContable : AuditableEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TipoCuentaContable Tipo { get; set; }

    public int? CuentaPadreId { get; set; }
    public CuentaContable? CuentaPadre { get; set; }
    public ICollection<CuentaContable> Subcuentas { get; set; } = new List<CuentaContable>();

    /// <summary>
    /// Una cuenta agrupadora puede deshabilitar movimientos directos sin dejar de formar parte del plan.
    /// </summary>
    public bool AceptaMovimientos { get; set; } = true;
    public bool Activa { get; set; } = true;

    public bool EsRaiz => CuentaPadreId is null;
}
