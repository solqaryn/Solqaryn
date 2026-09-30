using Solqaryn.Application.Common;
using Solqaryn.Domain.Entities;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Application.Interfaces;

public interface ICompraRepository
{
    Task<Compra?> GetByIdAsync(int id);
    Task<Compra?> GetByIdForUpdateAsync(int id);
    Task<CatalogoMetodoPago?> GetMetodoPagoPorCodigoONombreAsync(string valor);
    Task<(List<Compra> Items, int TotalCount)> GetPagedAsync(PagedRequest request);
    Task<int> GetTotalDelMesAsync(int? usuarioId = null);
    Task<decimal> GetCuentasPorPagarAsync(int? usuarioId = null);
    Task<List<Compra>> GetUltimasAsync(int cantidad = 5, int? usuarioId = null);
    Task<int> ContarTodasAsync();
    Task AddAsync(Compra compra);
    void Update(Compra compra);
    Task<bool> SaveChangesAsync();
}
