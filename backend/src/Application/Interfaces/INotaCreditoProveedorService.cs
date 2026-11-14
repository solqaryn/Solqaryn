using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface INotaCreditoProveedorService
{
    Task<PagedResult<NotaCreditoProveedorDto>> GetPagedAsync(NotaCreditoProveedorFiltroDto filtro);
    Task<NotaCreditoProveedorDto?> GetByIdAsync(int id);
    Task<NotaCreditoProveedorDto> CreateAsync(CreateNotaCreditoProveedorDto dto);
    Task<NotaCreditoProveedorDto> UpdateAsync(int id, UpdateNotaCreditoProveedorDto dto);
    Task<NotaCreditoProveedorDto> RegistrarAsync(int id);
    Task<NotaCreditoProveedorDto> AnularAsync(int id, AnularNotaCreditoProveedorDto dto);
}
