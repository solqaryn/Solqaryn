using Solqaryn.Domain.Entities.Contabilidad;

namespace Solqaryn.Application.Interfaces;

public interface ICentroCostoRepository
{
    Task<CentroCosto?> GetByIdAsync(int id);
    Task<CentroCosto?> GetByCodigoAsync(string codigo);
    Task<(List<CentroCosto> Items, int Total)> BuscarAsync(
        string? termino,
        TipoCentroCosto? tipo,
        int? sucursalId,
        bool? activo,
        int pagina,
        int tamanoPagina);
    Task<List<CentroCosto>> GetActivosAsync(TipoCentroCosto? tipo = null, int? sucursalId = null);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null);
    Task AddAsync(CentroCosto centroCosto);
    void Update(CentroCosto centroCosto);
    Task<bool> SaveChangesAsync();
}
