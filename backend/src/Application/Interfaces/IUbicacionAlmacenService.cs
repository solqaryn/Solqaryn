using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IUbicacionAlmacenService
{
    Task<UbicacionAlmacenPaginaDto> BuscarAsync(UbicacionAlmacenFiltroDto filtro);
    Task<List<UbicacionAlmacenDto>> GetActivasAsync(int? almacenId = null, int? ubicacionPadreId = null);
    IReadOnlyList<TipoUbicacionAlmacenDto> GetTipos();
    Task<UbicacionAlmacenDto?> GetByIdAsync(int id);
    Task<UbicacionAlmacenDto> CreateAsync(CreateUbicacionAlmacenDto dto);
    Task<UbicacionAlmacenDto?> UpdateAsync(int id, UpdateUbicacionAlmacenDto dto);
    Task<UbicacionAlmacenDto?> CambiarEstadoAsync(int id, bool activa);
    Task<bool> DeleteAsync(int id);
}
