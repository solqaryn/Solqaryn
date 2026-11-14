using Solqaryn.Domain.Entities.Bancos;
namespace Solqaryn.Application.Interfaces;
public interface IConciliacionBancariaRepository
{
    Task<ConciliacionBancaria?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ConciliacionBancaria?> GetByPeriodoAsync(int cuentaBancariaId, int mes, int anio, CancellationToken cancellationToken = default);
    Task<ConciliacionBancaria?> GetActivaByCuentaAsync(int cuentaBancariaId, CancellationToken cancellationToken = default);
    Task AddAsync(ConciliacionBancaria conciliacion, CancellationToken cancellationToken = default);
    void Update(ConciliacionBancaria conciliacion);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<ConciliacionBancaria> Items, int TotalCount)> GetPagedAsync(
        int? cuentaBancariaId,
        Solqaryn.Domain.Enums.Bancos.EstadoConciliacionBancaria? estado,
        int? mes,
        int? anio,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
