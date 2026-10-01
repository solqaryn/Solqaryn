using Solqaryn.Application.Common;
using Solqaryn.Domain.Entities;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Application.Interfaces;

public interface IVentaRepository
{
    Task<Venta?> GetByIdAsync(int id);
    Task<Venta?> GetByIdForUpdateAsync(int id);
    Task<CatalogoMetodoPago?> GetMetodoPagoPorCodigoONombreAsync(string valor);
    Task<(List<Venta> Items, int TotalCount)> GetPagedAsync(PagedRequest request);
    Task<int> GetTotalDelMesAsync(int? usuarioId = null);
    Task<decimal> GetIngresosDelMesAsync(int? usuarioId = null);
    Task<decimal> GetCuentasPorCobrarAsync(int? usuarioId = null);
    Task<decimal> GetUtilidadBrutaTotalAsync(int? usuarioId = null);
    Task<List<Venta>> GetUltimasAsync(int cantidad = 5, int? usuarioId = null);
    Task AddAsync(Venta venta);
    void Update(Venta venta);
    Task<bool> SaveChangesAsync();
}
