using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IReservaInventarioService
{
    Task<PagedResult<ReservaInventarioDto>> GetPagedAsync(ReservaInventarioQueryDto query);
    Task<ReservaInventarioDto?> GetByIdAsync(int id);
    Task<ReservaInventarioDto> CreateAsync(CreateReservaInventarioDto dto);
    Task<ReservaInventarioDto> UpdateAsync(int id, UpdateReservaInventarioDto dto);
    Task<ReservaInventarioDto> ActivarAsync(int id);
    Task<ReservaInventarioDto> ConsumirAsync(int id);
    Task<ReservaInventarioDto> LiberarAsync(int id, LiberarReservaInventarioDto dto);
    Task<ReservaInventarioDto> ExpirarAsync(int id);
    Task<ReservaInventarioDto> CancelarAsync(int id, CancelarReservaInventarioDto dto);
}
