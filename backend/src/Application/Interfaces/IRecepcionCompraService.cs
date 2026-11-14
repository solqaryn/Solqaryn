using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IRecepcionCompraService
{
    Task<PagedResult<RecepcionCompraDto>> GetPagedAsync(RecepcionCompraQueryDto filtro);
    Task<RecepcionCompraDto?> GetByIdAsync(int id);
    Task<RecepcionCompraSaldoOrdenDto?> GetSaldoOrdenAsync(int ordenCompraId);
    Task<RecepcionCompraDto> CreateAsync(CreateRecepcionCompraDto dto, string idempotencyKey);
    Task<RecepcionCompraDto> UpdateAsync(int id, UpdateRecepcionCompraDto dto);
    Task<RecepcionCompraDto> ConfirmarAsync(int id);
    Task<RecepcionCompraDto> AnularAsync(int id, AnularRecepcionCompraDto dto);
}
