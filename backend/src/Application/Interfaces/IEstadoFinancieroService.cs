using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Contrato de aplicación para generar los estados financieros soportados por N4.10.
/// </summary>
public interface IEstadoFinancieroService
{
    Task<EstadoFinancieroDto> GenerarAsync(
        TipoEstadoFinanciero tipo,
        EstadoFinancieroFiltroDto filtro,
        CancellationToken cancellationToken = default);
}
