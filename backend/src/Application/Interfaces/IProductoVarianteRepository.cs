using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public interface IProductoVarianteRepository
{
    Task<ProductoVariante?> GetByIdAsync(int id);
    Task<ProductoVariante?> GetByIdForUpdateAsync(int id);
    Task<List<ProductoVariante>> GetByIdsForUpdateAsync(IEnumerable<int> ids);
    Task<List<ProductoVariante>> GetByProductoIdAsync(int productoId, bool incluirInactivas = true);
    Task<List<ProductoVariante>> GetForReporteAsync(
        int? productoId = null,
        int? marcaId = null,
        int? modeloId = null,
        int? colorId = null,
        int? tallaId = null,
        bool incluirInactivas = true,
        CancellationToken cancellationToken = default);
    Task<ProductoVariante?> GetTecnicaByProductoIdAsync(int productoId, bool incluirEliminada = false);
    Task<ProductoVariante?> GetBySkuAsync(string sku);
    Task<ProductoVariante?> GetByCodigoBarrasAsync(string codigoBarras);
    Task<ProductoVariante?> GetByCombinacionAsync(
        int productoId,
        int? marcaId,
        int? modeloId,
        int? colorId,
        int? tallaId);
    Task<List<ProductoVariante>> BuscarPorCodigoAsync(
        string skuNormalizado,
        string codigoBarrasNormalizado,
        CancellationToken cancellationToken = default);
    Task<List<ProductoVariante>> BuscarPorTerminoAsync(
        string terminoNormalizado,
        bool soloConStock,
        int limite,
        CancellationToken cancellationToken = default,
        TipoInventario? tipoInventario = null);
    Task AddAsync(ProductoVariante variante);
    void Update(ProductoVariante variante);
    Task<bool> SaveChangesAsync();
}
