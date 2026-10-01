using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.DTOs;

/// <summary>
/// Contratos mínimos N6.9.D para exponer suscripción SaaS y límites al tenant.
/// EmpresaId no forma parte de los body DTO: la selección de tenant viaja por la
/// ruta y debe verificarse server-side antes de ejecutar cualquier caso de uso.
/// </summary>
public sealed record OnboardingSuscripcionSaaSRequest(
    string PlanCodigo,
    DateTime InicioUtc);

public sealed record SuscripcionSaaSDto(
    int Id,
    string PlanCodigo,
    string PlanNombre,
    EstadoSuscripcion Estado,
    DateTime InicioUtc,
    DateTime? FinUtc);

public sealed record LimiteSuscripcionSaaSDto(
    string Clave,
    int? ValorMaximo);

/// <summary>
/// Decisión server-side de acceso a un módulo SaaS. El cliente nunca aporta
/// EmpresaId dentro del payload ni una decisión de entitlement: ambos se resuelven
/// desde el tenant verificado y las reglas PlanModulo persistidas.
/// </summary>
public sealed record EntitlementModuloSaaSDto(
    string ModuloClave,
    bool Habilitado,
    MotivoDecisionModuloSaaS Motivo,
    int? PlanId,
    string? PlanCodigo);

public sealed record LimitesSuscripcionSaaSQuery(
    int Pagina = 1,
    int TamanoPagina = 50,
    string? Clave = null)
{
    public const int TamanoPaginaMaximo = 100;

    public LimitesSuscripcionSaaSQuery Normalizada()
    {
        if (Pagina <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Pagina), "La página debe ser mayor que cero.");
        }

        if (TamanoPagina <= 0 || TamanoPagina > TamanoPaginaMaximo)
        {
            throw new ArgumentOutOfRangeException(nameof(TamanoPagina), $"El tamaño de página debe estar entre 1 y {TamanoPaginaMaximo}.");
        }

        return this with { Clave = string.IsNullOrWhiteSpace(Clave) ? null : Clave.Trim().ToUpperInvariant() };
    }
}

public sealed record PaginaSuscripcionSaaSDto<T>(
    IReadOnlyList<T> Items,
    int Pagina,
    int TamanoPagina,
    int Total);

/// <summary>
/// Errores estables del contrato N6.9.D. Permiten mapear respuestas API sin
/// filtrar detalles internos ni convertir ausencia de autoridad tenant en acceso.
/// </summary>
public static class SuscripcionSaaSErrorCodes
{
    public const string TenantNoAutorizado = "SAAS_TENANT_NO_AUTORIZADO";
    public const string PlanNoEncontrado = "SAAS_PLAN_NO_ENCONTRADO";
    public const string SuscripcionNoEncontrada = "SAAS_SUSCRIPCION_NO_ENCONTRADA";
    public const string IdempotencyKeyRequerida = "SAAS_IDEMPOTENCY_KEY_REQUERIDA";
    public const string IdempotencyKeyConflictiva = "SAAS_IDEMPOTENCY_KEY_CONFLICTIVA";
}
