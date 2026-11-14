using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IOrdenCompraService
{
    Task<PagedResult<OrdenCompraDto>> GetPagedAsync(OrdenCompraFiltroDto filtro);
    Task<OrdenCompraDto?> GetByIdAsync(int id);
    Task<OrdenCompraDto> CreateAsync(CreateOrdenCompraDto dto, string idempotencyKey);
    Task<OrdenCompraDto> UpdateAsync(int id, UpdateOrdenCompraDto dto);
    Task<OrdenCompraDto> EnviarAprobacionAsync(int id);
    Task<OrdenCompraDto> AprobarAsync(int id);
    Task<OrdenCompraDto> CancelarAsync(int id, CancelarOrdenCompraDto dto);
}
