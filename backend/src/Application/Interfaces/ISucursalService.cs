using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ISucursalService
{
    Task<SucursalPaginaDto> BuscarAsync(SucursalFiltroDto filtro);
    Task<List<SucursalDto>> GetActivasAsync(int? empresaId = null);
    Task<SucursalDto?> GetByIdAsync(int id);
    Task<SucursalDto> CreateAsync(CreateSucursalDto dto);
    Task<SucursalDto?> UpdateAsync(int id, UpdateSucursalDto dto);
    Task<SucursalDto?> CambiarEstadoAsync(int id, bool activa);
    Task<bool> DeleteAsync(int id);
}
