using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IExistenciaVarianteRepository
{
    Task<ExistenciaVariante?> GetByIdAsync(int id);
    Task<ExistenciaVariante?> GetByClaveAsync(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId,
        bool forUpdate = false);
    Task<ExistenciaVariante?> GetByClaveParaReversionAsync(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId);
    Task<List<ExistenciaVariante>> GetOperativasPublicasPorVariantesAsync(
        IReadOnlyCollection<int> productoVarianteIds);
    Task<(List<ExistenciaVariante> Items, int Total)> BuscarAsync(
        int? productoId,
        int? productoVarianteId,
        int? almacenId,
        int? ubicacionAlmacenId,
        bool? soloSinUbicacion,
        bool? stockBajo,
        bool? agotada,
        int pagina,
        int tamanoPagina);
    Task<bool> ExisteClaveAsync(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId,
        int? excluirId = null);
    Task AddAsync(ExistenciaVariante existencia);
    void Update(ExistenciaVariante existencia);
    Task<bool> SaveChangesAsync();
}
