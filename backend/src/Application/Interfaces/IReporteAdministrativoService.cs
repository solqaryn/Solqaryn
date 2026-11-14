using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IReporteAdministrativoService
{
    Task<ResumenAdministrativoDto> ObtenerResumenAsync(
        ReporteAdministrativoFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<List<UsuarioAccesoReporteDto>> ObtenerUsuariosAccesoAsync(
        CancellationToken cancellationToken = default);

    Task<List<RolPermisosReporteDto>> ObtenerRolesPermisosAsync(
        CancellationToken cancellationToken = default);

    Task<AuditoriaResumenDto> ObtenerResumenAuditoriaAsync(
        ReporteAdministrativoFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<ArchivoDescargableDto> ExportarAsync(
        string tipo,
        string formato,
        ReporteAdministrativoFiltroDto filtro,
        CancellationToken cancellationToken = default);
}
