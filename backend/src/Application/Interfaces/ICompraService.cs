using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ICompraService
{
    Task<CompraDto?> GetByIdAsync(int id);
    Task<PagedResult<CompraDto>> GetPagedAsync(PagedRequest request);
    Task<CompraDto> CreateAsync(CreateCompraDto dto);
    Task<CompraDto?> UpdateAsync(int id, UpdateCompraDto dto);
    Task<CompraDto?> ConfirmarAsync(int id);
    Task<CompraDto?> AnularAsync(int id, string motivo);
    Task<bool> DeleteBorradorAsync(int id);
    Task<ResultadoCalculoDto> CalcularVistaPreviaAsync(CalcularCompraRequest request);
}
