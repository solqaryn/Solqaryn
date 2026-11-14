using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IEmpresaRepository
{
    Task<List<Empresa>> ListAsync(bool? activa = null, CancellationToken cancellationToken = default);
    Task<Empresa?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Empresa empresa, CancellationToken cancellationToken = default);
    void Update(Empresa empresa);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
