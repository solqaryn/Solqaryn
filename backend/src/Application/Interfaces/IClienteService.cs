using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IClienteService
{
    Task<List<ClienteDto>> GetAllAsync();
    Task<List<ClienteDto>> GetActivosAsync();
    Task<List<ClienteDto>> BuscarActivosAsync(string termino);
    Task<ClienteDto?> GetByIdAsync(int id);
    Task<ClienteDto> CreateAsync(CreateClienteDto dto);
    Task<ClienteDto?> UpdateAsync(int id, UpdateClienteDto dto);
    Task<ClienteDto?> CambiarEstadoAsync(int id, bool activo);
    Task<bool> DeleteAsync(int id);
}
