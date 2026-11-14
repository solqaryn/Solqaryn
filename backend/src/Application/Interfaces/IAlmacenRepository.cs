using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public interface IAlmacenRepository
{
    Task<Almacen?> GetByIdAsync(int id);
    Task<(List<Almacen> Items, int Total)> BuscarAsync(
        string? termino,
        bool? activo,
        int? sucursalId,
        TipoAlmacen? tipo,
        int pagina,
        int tamanoPagina);
    Task<List<Almacen>> GetActivosAsync(int? sucursalId = null);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null);
    Task AddAsync(Almacen almacen);
    void Update(Almacen almacen);
    Task<bool> SaveChangesAsync();
}
