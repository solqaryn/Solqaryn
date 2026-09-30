using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs.Contabilidad;

/// <summary>
/// Boundary estricto para solicitar contabilización desde un módulo de negocio.
/// No contiene cuentas contables: su resolución pertenece a la configuración del motor.
/// </summary>
public sealed record EventoContableDto(
    TipoEventoContable Tipo,
    int DocumentoOrigenId,
    DateTime Fecha,
    decimal Monto,
    string Referencia,
    decimal? Costo = null)
{
    public void Validar()
    {
        if (DocumentoOrigenId <= 0)
            throw new ArgumentOutOfRangeException(nameof(DocumentoOrigenId));
        if (Monto <= 0)
            throw new ArgumentOutOfRangeException(nameof(Monto), "El monto del evento contable debe ser mayor que cero.");
        if (Costo is < 0)
            throw new ArgumentOutOfRangeException(nameof(Costo), "El costo no puede ser negativo.");
        if (string.IsNullOrWhiteSpace(Referencia))
            throw new ArgumentException("La referencia del evento contable es obligatoria.", nameof(Referencia));
    }
}
