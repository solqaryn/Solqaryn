using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface ISolicitudCompraRepository
{
    Task<(IReadOnlyList<SolicitudCompra> Items, int Total)> GetPagedAsync(SolicitudCompraFiltroDto filtro);
    Task<SolicitudCompra?> GetByIdAsync(int id, bool tracking = false);
    Task<SolicitudCompra?> GetByIdForUpdateAsync(int id);
    Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null);
    Task AddAsync(SolicitudCompra solicitud);
    Task SaveChangesAsync();
}
