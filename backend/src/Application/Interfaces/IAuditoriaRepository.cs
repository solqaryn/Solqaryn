using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IAuditoriaRepository
{
    Task AddAsync(RegistroAuditoria registro);
    Task<(List<RegistroAuditoria> Items, int TotalCount)> GetFilteredAsync(AuditoriaFiltroDto filtro);
    Task<bool> SaveChangesAsync();
}
