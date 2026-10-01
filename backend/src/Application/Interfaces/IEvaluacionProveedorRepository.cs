using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IEvaluacionProveedorRepository
{
    Task<(IReadOnlyList<EvaluacionProveedor> Items, int Total)> GetPagedAsync(EvaluacionProveedorFiltroDto filtro);
    Task<EvaluacionProveedor?> GetByIdAsync(int id, bool tracking = false);
    Task<EvaluacionProveedor?> GetByRecepcionCompraIdAsync(int recepcionCompraId, bool tracking = false);
    Task AddAsync(EvaluacionProveedor evaluacion);
    Task SaveChangesAsync();
}
