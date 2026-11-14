using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IMovimientoInventarioService
{
    Task<List<MovimientoInventarioDto>> GetFilteredAsync(int? productoId, string? tipo, DateTime? desde, DateTime? hasta);
    Task<PagedResult<MovimientoInventarioDto>> GetPagedAsync(MovimientoInventarioQueryDto query);
}
