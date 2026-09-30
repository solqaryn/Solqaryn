using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IFacturaProveedorService
{
    Task<PagedResult<FacturaProveedorDto>> GetPagedAsync(FacturaProveedorFiltroDto filtro);
    Task<FacturaProveedorDto?> GetByIdAsync(int id);
    Task<FacturaProveedorDto> CreateAsync(CreateFacturaProveedorDto dto);
    Task<FacturaProveedorDto> UpdateAsync(int id, UpdateFacturaProveedorDto dto);
    Task<FacturaProveedorDto> RegistrarAsync(int id);
    Task<FacturaProveedorDto> AnularAsync(int id, AnularFacturaProveedorDto dto);
}
