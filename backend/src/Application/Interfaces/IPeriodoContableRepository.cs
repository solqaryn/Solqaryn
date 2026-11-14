using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Domain.Entities.Contabilidad;

namespace Solqaryn.Application.Interfaces;

public interface IPeriodoContableRepository
{
    Task<PeriodoContable?> GetByIdAsync(int id, bool tracking = false, CancellationToken cancellationToken = default);
    Task<PeriodoContable?> GetByDateAsync(DateTime date, bool tracking = false, CancellationToken cancellationToken = default);
    Task<bool> IsValidDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingPeriodAsync(DateTime fechaInicio, DateTime fechaFin, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<PeriodoContable>> GetPagedAsync(PeriodoContableQueryDto filter, CancellationToken cancellationToken = default);
    Task AddAsync(PeriodoContable periodo, CancellationToken cancellationToken = default);
    void Update(PeriodoContable periodo);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
