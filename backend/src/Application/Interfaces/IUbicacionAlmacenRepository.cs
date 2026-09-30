using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public interface IUbicacionAlmacenRepository
{
    Task<UbicacionAlmacen?> GetByIdAsync(int id);
    Task<(List<UbicacionAlmacen> Items, int Total)> BuscarAsync(
        string? termino,
        int? almacenId,
        int? ubicacionPadreId,
        bool soloRaiz,
        TipoUbicacionAlmacen? tipo,
        bool? activa,
        int pagina,
        int tamanoPagina);
    Task<List<UbicacionAlmacen>> GetActivasAsync(int? almacenId = null, int? ubicacionPadreId = null);
    Task<bool> ExisteCodigoAsync(int almacenId, string codigo, int? excluirId = null);
    Task<bool> TieneHijasActivasAsync(int ubicacionId);
    Task<bool> TieneHijasNoEliminadasAsync(int ubicacionId);
    Task<bool> CreariaCicloAsync(int ubicacionId, int almacenId, int? nuevoPadreId);
    Task AddAsync(UbicacionAlmacen ubicacion);
    void Update(UbicacionAlmacen ubicacion);
    Task<bool> SaveChangesAsync();
}
