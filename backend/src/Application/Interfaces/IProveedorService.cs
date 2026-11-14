using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IProveedorService
{
    Task<List<ProveedorDto>> GetAllAsync();
    Task<List<ProveedorDto>> GetActivosAsync();
    Task<List<ProveedorDto>> BuscarActivosAsync(string termino);
    Task<ProveedorDto?> GetByIdAsync(int id);
    Task<ProveedorDto> CreateAsync(CreateProveedorDto dto);
    Task<ProveedorDto?> UpdateAsync(int id, UpdateProveedorDto dto);
    Task<ProveedorDto?> CambiarEstadoAsync(int id, bool activo);
    Task<bool> DeleteAsync(int id);
}
