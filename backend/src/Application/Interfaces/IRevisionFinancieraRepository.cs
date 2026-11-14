using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IRevisionFinancieraRepository
{
    Task AddAsync(RevisionFinanciera revision);
    Task<RevisionFinanciera?> GetUltimaAsync();
    Task<List<RevisionFinanciera>> GetAllAsync();
    Task<bool> SaveChangesAsync();
}
