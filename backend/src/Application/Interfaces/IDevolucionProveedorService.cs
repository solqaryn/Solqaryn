using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IDevolucionProveedorService
{
    Task<PagedResult<DevolucionProveedorDto>> GetPagedAsync(DevolucionProveedorQueryDto filtro);
    Task<DevolucionProveedorDto?> GetByIdAsync(int id);
    Task<DevolucionProveedorDto> CreateAsync(CreateDevolucionProveedorDto dto, string idempotencyKey);
    Task<DevolucionProveedorDto> UpdateAsync(int id, UpdateDevolucionProveedorDto dto);
    Task<DevolucionProveedorDto> ConfirmarAsync(int id);
    Task<DevolucionProveedorDto> AnularAsync(int id, AnularDevolucionProveedorDto dto);
}
