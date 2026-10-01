using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IPoliticaCosteoInventarioService
{
    Task<PoliticaCosteoInventarioDto> GetVigenteAsync();
    Task<PagedResult<PoliticaCosteoInventarioDto>> GetHistorialAsync(PoliticaCosteoInventarioQueryDto query);
    Task<IReadOnlyList<MetodoCosteoInventarioDto>> GetMetodosAsync();
    Task<PoliticaCosteoInventarioDto> CambiarAsync(CambiarPoliticaCosteoInventarioDto dto);
}
