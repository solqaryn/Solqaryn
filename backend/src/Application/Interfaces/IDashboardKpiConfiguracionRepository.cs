using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IDashboardKpiConfiguracionRepository
{
    Task<List<DashboardKpiConfiguracion>> GetForScopeAsync(
        int usuarioId,
        int rolId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        DashboardKpiConfiguracion configuracion,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
