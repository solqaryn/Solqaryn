using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IRentabilidadVentasService
{
    Task<IReadOnlyList<ReporteRentabilidadDto>> ObtenerAsync(
        ReporteVentasFiltroDto filtro,
        RentabilidadAgrupacion agrupacion,
        CancellationToken cancellationToken = default);
}
