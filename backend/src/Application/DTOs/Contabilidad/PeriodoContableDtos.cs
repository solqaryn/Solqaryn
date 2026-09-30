using Solqaryn.Domain.Entities.Contabilidad;

namespace Solqaryn.Application.DTOs.Contabilidad;

public sealed class PeriodoContableDto
{
    public int Id { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public EstadoPeriodoContable Estado { get; set; }
    public DateTime? CerradoEnUtc { get; set; }
}

public sealed class CrearPeriodoContableDto
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}
