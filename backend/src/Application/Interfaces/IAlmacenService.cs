using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IAlmacenService
{
    Task<AlmacenPaginaDto> BuscarAsync(AlmacenFiltroDto filtro);
    Task<List<AlmacenDto>> GetActivosAsync(int? sucursalId = null);
    IReadOnlyList<TipoAlmacenDto> GetTipos();
    Task<AlmacenDto?> GetByIdAsync(int id);
    Task<AlmacenDto> CreateAsync(CreateAlmacenDto dto);
    Task<AlmacenDto?> UpdateAsync(int id, UpdateAlmacenDto dto);
    Task<AlmacenDto?> CambiarEstadoAsync(int id, bool activo);
    Task<bool> DeleteAsync(int id);
}
