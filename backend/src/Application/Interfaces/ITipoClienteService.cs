using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ITipoClienteService
{
    Task<List<TipoClienteDto>> GetAllAsync();
    Task<List<TipoClienteDto>> GetActivosAsync();
    Task<TipoClienteDto?> GetByIdAsync(int id);
    Task<TipoClienteDto> CreateAsync(CreateTipoClienteDto dto);
    Task<TipoClienteDto?> UpdateAsync(int id, UpdateTipoClienteDto dto);
    Task<TipoClienteDto?> CambiarEstadoAsync(int id, bool activo);
    Task<bool> DeleteAsync(int id);
}
