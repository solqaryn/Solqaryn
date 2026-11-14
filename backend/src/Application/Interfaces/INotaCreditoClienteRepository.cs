using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface INotaCreditoClienteRepository
{
    Task<NotaCreditoCliente?> GetByIdAsync(int id, bool tracking = false);
    Task AddAsync(NotaCreditoCliente notaCredito);
    Task SaveChangesAsync();
}

public interface INotaCreditoClienteService
{
    Task<NotaCreditoClienteDto?> GetByIdAsync(int id);
    Task<NotaCreditoClienteDto> CreateAsync(CreateNotaCreditoClienteDto dto);
}
