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
    /// <summary>Bloquea el producto por tenant verificado en la transaccion DELETE.</summary>
    Task<bool> LockForDeleteAsync(int id);
    Task<bool> DeleteAsync(int id);
}
