using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ICategoriaService
{
    Task<List<CategoriaDto>> GetAllAsync();
    Task<List<CategoriaDto>> GetActivasAsync();
    Task<CategoriaDto?> GetByIdAsync(int id);
    Task<CategoriaDto> CreateAsync(CreateCategoriaDto dto);
    Task<CategoriaDto?> UpdateAsync(int id, UpdateCategoriaDto dto);
    Task<CategoriaDto?> CambiarEstadoAsync(int id, bool activa);
    Task<bool> DeleteAsync(int id);
}
