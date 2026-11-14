using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IProductoService
{
    Task<ProductoDto?> GetByIdAsync(int id);
    Task<PagedResult<ProductoDto>> GetPagedAsync(PagedRequest request);
    Task<ProductoDto> CreateAsync(CreateProductoDto dto);
    Task<ProductoDto?> UpdateAsync(int id, UpdateProductoDto dto);
    Task<ProductoDto?> CambiarEstadoAsync(int id, bool activo);
    Task<bool> DeleteAsync(int id);
}
