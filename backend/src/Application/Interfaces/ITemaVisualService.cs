using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ITemaVisualService
{
    Task<TemaVisualDto> GetAsync();
    Task<TemaVisualDto> UpdateAsync(ActualizarTemaVisualDto dto);
    Task<TemaVisualDto> RestaurarPredeterminadoAsync();
}
