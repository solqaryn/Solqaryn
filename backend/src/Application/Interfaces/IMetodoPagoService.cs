using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IMetodoPagoService
{
    Task<List<MetodoPagoDto>> GetAllAsync();
    Task<List<MetodoPagoDto>> GetActivosAsync();
    Task<MetodoPagoDto?> GetByIdAsync(int id);
    Task<MetodoPagoDto> CreateAsync(CreateMetodoPagoDto dto);
    Task<MetodoPagoDto?> UpdateAsync(int id, UpdateMetodoPagoDto dto);
    Task<MetodoPagoDto?> CambiarEstadoAsync(int id, bool activo);
    Task<bool> DeleteAsync(int id);
    Task ReordenarAsync(IReadOnlyCollection<ReordenarMetodoPagoDto> items);
}
