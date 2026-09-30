using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IVentaService
{
    Task<VentaDto?> GetByIdAsync(int id);
    Task<PagedResult<VentaDto>> GetPagedAsync(PagedRequest request);
    Task<VentaDto> CreateAsync(CreateVentaDto dto);
    Task<VentaDto?> UpdateAsync(int id, UpdateVentaDto dto);
    Task<VentaDto?> ConfirmarAsync(int id);
    Task<VentaDto?> AnularAsync(int id, string motivo);
    Task<bool> DeleteBorradorAsync(int id);
    Task<ResultadoCalculoDto> CalcularVistaPreviaAsync(CalcularVentaRequest request);
}
