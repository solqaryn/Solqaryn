using Solqaryn.Application.DTOs.Bancos;

namespace Solqaryn.Application.Interfaces;

public interface IConciliacionBancariaService
{
    Task<ImportarEstadoCuentaResponseDto> ImportarEstadoCuentaAsync(ImportarEstadoCuentaRequestDto request, int usuarioId, CancellationToken cancellationToken = default);
    Task<ConciliarMovimientosResponseDto> ConciliarMovimientosAsync(ConciliarMovimientosRequestDto request, int usuarioId, CancellationToken cancellationToken = default);
    Task<SolicitarAjusteResponseDto> SolicitarAjusteAsync(SolicitarAjusteRequestDto request, int usuarioId, CancellationToken cancellationToken = default);
    Task<CerrarPeriodoConciliacionResponseDto> CerrarPeriodoAsync(CerrarPeriodoConciliacionRequestDto request, int usuarioId, CancellationToken cancellationToken = default);
    Task<ReabrirPeriodoConciliacionResponseDto> ReabrirPeriodoAsync(ReabrirPeriodoConciliacionRequestDto request, int usuarioId, CancellationToken cancellationToken = default);
    Task<ConciliacionBancariaPageDto> GetConciliacionesAsync(ConciliacionBancariaFilterDto filter, CancellationToken cancellationToken = default);
}
