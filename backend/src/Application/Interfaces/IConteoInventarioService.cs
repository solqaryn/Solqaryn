using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IConteoInventarioService
{
    Task<PagedResult<ConteoInventarioDto>> GetPagedAsync(ConteoInventarioQueryDto query);
    Task<ConteoInventarioDto?> GetByIdAsync(int id);
    Task<ConteoInventarioDto> CreateAsync(CreateConteoInventarioDto dto);
    Task<ConteoInventarioDto?> UpdateAsync(int id, UpdateConteoInventarioDto dto);
    Task<ConteoInventarioDto?> IniciarAsync(int id);
    Task<ConteoInventarioDto?> CapturarDetalleAsync(int id, int detalleId, CapturarConteoInventarioDetalleDto dto);
    Task<ConteoInventarioDto?> CapturarLoteAsync(int id, CapturarConteoInventarioLoteDto dto);
    Task<ConteoInventarioDto?> CerrarAsync(int id);
    Task<ConteoInventarioDto?> AprobarAsync(int id);
    Task<AjusteInventarioDto?> GenerarAjusteAsync(int id);
    Task<ConteoInventarioDto?> CancelarAsync(int id, string motivo);
}
