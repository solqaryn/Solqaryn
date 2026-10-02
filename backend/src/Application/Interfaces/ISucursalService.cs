using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ISucursalService
{
    Task<SucursalPaginaDto> BuscarAsync(SucursalFiltroDto filtro, int empresaIdAutorizada);
    Task<List<SucursalDto>> GetActivasAsync(int empresaIdAutorizada, int? empresaIdSolicitada = null);
    Task<SucursalDto?> GetByIdAsync(int id, int empresaIdAutorizada);
    Task<SucursalDto> CreateAsync(CreateSucursalDto dto, int empresaIdAutorizada);
    Task<SucursalDto?> UpdateAsync(int id, UpdateSucursalDto dto, int empresaIdAutorizada);
    Task<SucursalDto?> CambiarEstadoAsync(int id, bool activa, int empresaIdAutorizada);
    Task<bool> DeleteAsync(int id, int empresaIdAutorizada);
}
