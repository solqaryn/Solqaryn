using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface ICompraDocumentoRepository
{
    Task<List<CompraDocumento>> GetByCompraIdAsync(int compraId);
    Task<CompraDocumento?> GetByIdAsync(int compraId, int documentoId);
    Task<int> CountByCompraIdAsync(int compraId);
    Task AddAsync(CompraDocumento documento);
    void Update(CompraDocumento documento);
    Task<bool> SaveChangesAsync();
}
