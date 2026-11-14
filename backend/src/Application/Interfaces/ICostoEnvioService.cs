using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ICostoEnvioService
{
    Task<List<CostoEnvioDto>> GetAllAsync();
    Task<CostoEnvioDto?> GetByIdAsync(int id);
    Task<CostoEnvioDto?> GetPredeterminadoVigenteAsync();
    Task<CostoEnvioDto> CreateAsync(GuardarCostoEnvioDto dto);
    Task<CostoEnvioDto?> UpdateAsync(int id, GuardarCostoEnvioDto dto);
    Task<bool> CambiarEstadoAsync(int id, bool activo);
    Task<bool> EliminarAsync(int id);
}
