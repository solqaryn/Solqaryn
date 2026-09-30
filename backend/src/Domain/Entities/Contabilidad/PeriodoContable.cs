using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities.Contabilidad;

public enum EstadoPeriodoContable
{
    Abierto = 1,
    Cerrado = 2
}

/// <summary>
/// Represents an accounting period and its fail-closed lifecycle.
/// Persistence/API authorization are intentionally handled by later N4.9 slices.
/// </summary>
public sealed class PeriodoContable : AuditableEntity
{
    public DateTime FechaInicio { get; private set; }
    public DateTime FechaFin { get; private set; }
    public EstadoPeriodoContable Estado { get; private set; } = EstadoPeriodoContable.Abierto;
    public DateTime? CerradoEnUtc { get; private set; }

    private PeriodoContable() { }

    public PeriodoContable(DateTime fechaInicio, DateTime fechaFin)
    {
        if (fechaInicio.Kind == DateTimeKind.Unspecified || fechaFin.Kind == DateTimeKind.Unspecified)
            throw new ArgumentException("Las fechas del período contable deben tener zona horaria explícita.");

        if (fechaFin < fechaInicio)
            throw new ArgumentException("La fecha final del período contable no puede ser anterior a la fecha inicial.");

        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
    }

    public void Cerrar(DateTime cerradoEnUtc)
    {
        if (Estado == EstadoPeriodoContable.Cerrado)
            throw new InvalidOperationException("El período contable ya está cerrado.");

        if (cerradoEnUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha de cierre debe expresarse en UTC.", nameof(cerradoEnUtc));

        Estado = EstadoPeriodoContable.Cerrado;
        CerradoEnUtc = cerradoEnUtc;
    }

    public void ValidarCambio(DateTime fechaOperacion, bool autorizadoCambioRetroactivo)
    {
        if (fechaOperacion < FechaInicio || fechaOperacion > FechaFin)
            throw new InvalidOperationException("La operación está fuera del período contable.");

        if (Estado == EstadoPeriodoContable.Cerrado && !autorizadoCambioRetroactivo)
            throw new InvalidOperationException("El período contable está cerrado; el cambio retroactivo requiere autorización explícita.");
    }
}
