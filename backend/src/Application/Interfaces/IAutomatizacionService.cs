using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IAutomatizacionService
{
    Task<AutomatizacionConfiguracionDto> GetConfiguracionAsync(CancellationToken cancellationToken = default);
    Task<AutomatizacionConfiguracionDto> UpdateConfiguracionAsync(ActualizarAutomatizacionConfiguracionRequest request, CancellationToken cancellationToken = default);
    Task<AutomatizacionResumenDto> GetSugerenciasAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AutocompletadoItemDto>> AutocompletarAsync(string contexto, string termino, CancellationToken cancellationToken = default);
    Task<AccionMasivaPreviewDto> PrevisualizarAccionMasivaAsync(AccionMasivaPreviewRequest request, CancellationToken cancellationToken = default);
}
