using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IReservaInventarioRepository
{
    Task<(IReadOnlyList<ReservaInventario> Items, int Total)> GetPagedAsync(ReservaInventarioQueryDto query);
    Task<ReservaInventario?> GetByIdAsync(int id, bool tracking = false);
    Task<ReservaInventario?> GetByPedidoVentaIdAsync(int pedidoVentaId, bool tracking = false);
    Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null);
    Task AddAsync(ReservaInventario reserva);
    Task SaveChangesAsync();
}
