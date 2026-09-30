using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Versión temporal de costo estándar por Variante. El costo real de adquisición
/// permanece separado para poder registrar variaciones sin perder evidencia.
/// </summary>
public sealed class CostoEstandarInventario : AuditableEntity
{
    public int ProductoVarianteId { get; private set; }
    public decimal CostoUnitario { get; private set; }
    public DateTime VigenteDesdeUtc { get; private set; }
    public DateTime? VigenteHastaUtc { get; private set; }
    public string Motivo { get; private set; } = string.Empty;

    public bool EstaVigente => !VigenteHastaUtc.HasValue;

    private CostoEstandarInventario()
    {
    }

    public static CostoEstandarInventario Crear(
        int productoVarianteId,
        decimal costoUnitario,
        DateTime vigenteDesdeUtc,
        string motivo)
    {
        if (productoVarianteId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productoVarianteId));
        if (costoUnitario < 0m)
            throw new ArgumentOutOfRangeException(nameof(costoUnitario), "El costo estándar no puede ser negativo.");
        if (vigenteDesdeUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La vigencia debe expresarse en UTC.", nameof(vigenteDesdeUtc));
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo del costo estándar es obligatorio.", nameof(motivo));

        return new CostoEstandarInventario
        {
            ProductoVarianteId = productoVarianteId,
            CostoUnitario = costoUnitario,
            VigenteDesdeUtc = vigenteDesdeUtc,
            Motivo = motivo.Trim()
        };
    }

    public void Cerrar(DateTime vigenteHastaUtc)
    {
        if (!EstaVigente)
            throw new InvalidOperationException("El costo estándar ya fue cerrado.");
        if (vigenteHastaUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha de cierre debe expresarse en UTC.", nameof(vigenteHastaUtc));
        if (vigenteHastaUtc <= VigenteDesdeUtc)
            throw new ArgumentOutOfRangeException(nameof(vigenteHastaUtc), "El cierre debe ser posterior al inicio de vigencia.");

        VigenteHastaUtc = vigenteHastaUtc;
        FechaActualizacion = DateTime.UtcNow;
    }

    public decimal CalcularVariacion(decimal costoRealUnitario, int cantidad)
    {
        if (costoRealUnitario < 0m)
            throw new ArgumentOutOfRangeException(nameof(costoRealUnitario));
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad));

        return (costoRealUnitario - CostoUnitario) * cantidad;
    }
}
