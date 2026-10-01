using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IConsumoInsumoService
{
    Task<List<ConsumoInsumoDto>> GetAllAsync();
    Task<ConsumoInsumoDto?> GetByIdAsync(int id);
    Task<ConsumoInsumoDto> CreateAsync(CreateConsumoInsumoDto dto);
    Task<ConsumoInsumoDto?> UpdateAsync(int id, UpdateConsumoInsumoDto dto);
    Task<ConsumoInsumoDto?> ConfirmarAsync(int id);
    Task<ConsumoInsumoDto?> AnularAsync(int id, string motivoAnulacion);
    Task<bool> DeleteBorradorAsync(int id);
}
