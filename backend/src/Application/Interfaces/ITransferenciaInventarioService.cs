using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ITransferenciaInventarioService
{
    Task<PagedResult<TransferenciaInventarioDto>> GetPagedAsync(TransferenciaInventarioFiltroDto filtro);
    Task<TransferenciaInventarioDto?> GetByIdAsync(int id);
    Task<TransferenciaInventarioDto> CreateAsync(CreateTransferenciaInventarioDto dto);
    Task<TransferenciaInventarioDto?> UpdateAsync(int id, UpdateTransferenciaInventarioDto dto);
    Task<TransferenciaInventarioDto?> SolicitarAsync(int id);
    Task<TransferenciaInventarioDto?> AprobarAsync(int id, AprobarTransferenciaInventarioDto dto);
    Task<TransferenciaInventarioDto?> DespacharAsync(int id, DespacharTransferenciaInventarioDto dto);
    Task<TransferenciaInventarioDto?> RecibirAsync(int id, RecibirTransferenciaInventarioDto dto);
    Task<TransferenciaInventarioDto?> CancelarAsync(int id, CancelarTransferenciaInventarioDto dto);
}
