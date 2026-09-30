using Solqaryn.Application.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public interface IProductoRepository
{
    Task<Producto?> GetByIdAsync(int id);
    Task<Producto?> GetByIdForUpdateAsync(int id);
    Task<List<Producto>> GetByIdsForUpdateAsync(IEnumerable<int> ids);
    Task<(List<Producto> Items, int TotalCount)> GetPagedAsync(PagedRequest request);
    Task<List<Producto>> GetStockBajoAsync();
    Task<List<Producto>> GetUltimosAgregadosAsync(int cantidad = 5);
    Task<int> GetTotalProductosAsync();
    Task<int> GetTotalUnidadesAsync();
    Task<decimal> GetValorTotalCostoAsync();
    Task<decimal> GetValorTotalPrecioAsync();
    Task<int> GetTotalProductosPorTipoAsync(TipoInventario tipoInventario);
    Task<int> GetTotalUnidadesPorTipoAsync(TipoInventario tipoInventario);
    Task<decimal> GetValorTotalCostoPorTipoAsync(TipoInventario tipoInventario);
    Task<decimal> GetValorTotalPrecioPorTipoAsync(TipoInventario tipoInventario);
    Task AddAsync(Producto producto);
    void Update(Producto producto);
    void Remove(Producto producto);
    Task<bool> SaveChangesAsync();
}
