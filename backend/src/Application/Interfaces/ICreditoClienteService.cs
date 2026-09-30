using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ICreditoClienteService
{
    Task<CreditoClienteDto> GetByIdAsync(int id);
    Task<IReadOnlyList<CreditoClienteDto>> GetByClienteIdAsync(int clienteId);
    Task<CreditoClienteDto> CrearAsync(CreateCreditoClienteDto dto);
    Task<CreditoClienteDto> ActualizarPoliticaAsync(int id, UpdateCreditoClienteDto dto);
    Task<CreditoClienteDto> AplicarBloqueoAutomaticoAsync(int id, AplicarBloqueoCreditoClienteDto dto);
    Task<CreditoClienteDto> LiberarBloqueoAutomaticoAsync(int id);
    Task<CreditoClienteDto> AutorizarExcepcionAsync(int id, AutorizarExcepcionCreditoClienteDto dto);
    Task<CreditoClienteDto> RevocarExcepcionAsync(int id);
}
