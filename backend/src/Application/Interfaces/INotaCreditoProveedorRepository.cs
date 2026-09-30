using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface INotaCreditoProveedorRepository
{
    Task<(IReadOnlyList<NotaCreditoProveedor> Items, int Total)> GetPagedAsync(NotaCreditoProveedorFiltroDto filtro);
    Task<NotaCreditoProveedor?> GetByIdAsync(int id, bool tracking = false);
    Task<NotaCreditoProveedor?> GetByIdForUpdateAsync(int id);
    Task<NotaCreditoProveedor?> GetByProveedorNumeroAsync(int proveedorId, string numeroNotaCredito, bool tracking = false);
    Task<decimal> GetCreditoRegistradoAcumuladoPorFacturaAsync(int facturaProveedorId, int? excluirNotaCreditoId = null);
    Task AddAsync(NotaCreditoProveedor notaCredito);
    Task SaveChangesAsync();
}
