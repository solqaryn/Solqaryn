using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Puerto de persistencia mínimo N6.9.D. Todos los accesos tenant-owned reciben
/// EmpresaId ya verificado por el caso de uso; ninguna implementación puede usar
/// un EmpresaId proveniente directamente del body como autoridad.
/// </summary>
public interface ISuscripcionesSaaSRepository
{
    Task<Plan?> ObtenerPlanActivoPorCodigoAsync(
        string codigo,
        CancellationToken cancellationToken = default);

    Task<Plan?> ObtenerPlanPorIdAsync(
        int planId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlanModulo>> ObtenerModulosPlanAsync(
        int planId,
        string planCodigo,
        CancellationToken cancellationToken = default);

    Task<Suscripcion?> ObtenerVigenteAsync(
        int empresaId,
        DateTime instanteUtc,
        CancellationToken cancellationToken = default);

    Task<Suscripcion?> ObtenerPorIdempotenciaAsync(
        int empresaId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        Suscripcion suscripcion,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Casos de uso mínimos del parent N6.9.D. El parámetro empresaId es únicamente
/// una selección solicitada por la ruta API. La implementación debe resolverlo
/// con IUsuarioScopeService.ObtenerActualAsync(empresaId) y operar exclusivamente
/// con scope.EmpresaId; un null/mismatch falla cerrado.
/// </summary>
public interface ISuscripcionesSaaSService
{
    Task<SuscripcionSaaSDto> OnboardingAsync(
        int empresaId,
        OnboardingSuscripcionSaaSRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<SuscripcionSaaSDto> ObtenerActualAsync(
        int empresaId,
        DateTime? instanteUtc = null,
        CancellationToken cancellationToken = default);

    Task<PaginaSuscripcionSaaSDto<LimiteSuscripcionSaaSDto>> ObtenerLimitesAsync(
        int empresaId,
        LimitesSuscripcionSaaSQuery query,
        CancellationToken cancellationToken = default);

    Task<EntitlementModuloSaaSDto> EvaluarModuloAsync(
        int empresaId,
        string moduloClave,
        DateTime? instanteUtc = null,
        CancellationToken cancellationToken = default);
}
