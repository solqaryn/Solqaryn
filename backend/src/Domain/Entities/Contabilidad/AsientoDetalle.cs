using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities.Contabilidad;

public class AsientoDetalle : AuditableEntity
{
    public int AsientoContableId { get; set; }
    public AsientoContable? AsientoContable { get; set; }

    public int CuentaContableId { get; set; }
    public CuentaContable? CuentaContable { get; set; }

    public decimal Debe { get; private set; }
    public decimal Haber { get; private set; }
    public string? Referencia { get; set; }

    protected AsientoDetalle() { }

    public AsientoDetalle(int cuentaContableId, decimal debe, decimal haber, string? referencia)
    {
        if (cuentaContableId <= 0) throw new ArgumentOutOfRangeException(nameof(cuentaContableId));
        if (debe < 0 || haber < 0) throw new ArgumentOutOfRangeException(nameof(debe), "Los montos no pueden ser negativos.");
        if (debe == 0 && haber == 0) throw new ArgumentException("El detalle debe tener un monto positivo en Debe o Haber.");
        if (debe > 0 && haber > 0) throw new ArgumentException("Un detalle no puede tener montos en Debe y Haber simultáneamente.");

        CuentaContableId = cuentaContableId;
        Debe = debe;
        Haber = haber;
        Referencia = referencia;
    }
}
