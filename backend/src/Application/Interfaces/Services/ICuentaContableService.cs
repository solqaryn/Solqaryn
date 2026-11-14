using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces.Services;

public interface ICuentaContableService
{
    Task<CuentaContableDto?> GetByIdAsync(int id);
    Task<IReadOnlyList<CuentaContableDto>> GetAllAsync();
    Task<IReadOnlyList<CuentaContableDto>> GetRaicesAsync();
    Task<CuentaContableDto> CreateAsync(CreateCuentaContableDto dto);
    Task<CuentaContableDto> UpdateAsync(int id, UpdateCuentaContableDto dto);
}
