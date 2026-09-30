using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

/// <summary>
/// Casos de uso tenant-aware para Suscripciones SaaS. La ruta empresaId nunca se
/// usa como autoridad: toda operación deriva el tenant efectivo de IUsuarioScopeService.
/// </summary>
public sealed class SuscripcionesSaaSService : ISuscripcionesSaaSService
{
    private const int IdempotencyKeyMaxLength = 160;
    private readonly ISuscripcionesSaaSRepository _repository;
    private readonly IUsuarioScopeService _usuarioScopeService;
    private readonly IAuditoriaService? _auditoria;

    public SuscripcionesSaaSService(
        ISuscripcionesSaaSRepository repository,
        IUsuarioScopeService usuarioScopeService,
        IAuditoriaService? auditoria = null)
    {
        _repository = repository;
        _usuarioScopeService = usuarioScopeService;
        _auditoria = auditoria;
    }

    public async Task<SuscripcionSaaSDto> OnboardingAsync(
        int empresaId,
        OnboardingSuscripcionSaaSRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var scope = await ResolverTenantAsync(empresaId, cancellationToken);
        var key = NormalizarIdempotencyKey(idempotencyKey);
        var codigoPlan = NormalizarCodigoPlan(request.PlanCodigo);
        var inicioUtc = NormalizarUtc(request.InicioUtc);

        var replay = await _repository.ObtenerPorIdempotenciaAsync(
            scope.EmpresaId,
            key,
            cancellationToken);

        if (replay is not null)
            return await ResolverReplayAsync(replay, codigoPlan, inicioUtc, cancellationToken);

        var vigente = await _repository.ObtenerVigenteAsync(
            scope.EmpresaId,
            inicioUtc,
            cancellationToken);
        if (vigente is not null)
        {
            throw new ConflictException(
                $"{SuscripcionSaaSErrorCodes.IdempotencyKeyConflictiva}: el tenant ya posee una suscripción vigente para el instante solicitado.");
        }

        var plan = await _repository.ObtenerPlanActivoPorCodigoAsync(codigoPlan, cancellationToken)
            ?? throw new ResourceNotFoundException(
                $"{SuscripcionSaaSErrorCodes.PlanNoEncontrado}: no existe un plan activo con código '{codigoPlan}'.");

        var suscripcion = new Suscripcion(scope.EmpresaId, plan.Id, inicioUtc);
        await _repository.AgregarAsync(suscripcion, key, cancellationToken);

        try
        {
            await _repository.GuardarCambiosAsync(cancellationToken);
            var dto = Mapear(suscripcion, plan);

            if (_auditoria is not null)
            {
                await _auditoria.RegistrarAsync(
                    ModuloSistema.Configuracion,
                    AccionPermiso.Crear,
                    $"Suscripción SaaS creada para empresa {scope.EmpresaId} con plan {plan.Codigo}.",
                    suscripcion.Id,
                    entidad: "Suscripcion",
                    valoresNuevos: new
                    {
                        EmpresaId = scope.EmpresaId,
                        PlanCodigo = plan.Codigo,
                        suscripcion.InicioUtc
                    });
            }

            return dto;
        }
        catch (IdempotencyConcurrencyException)
        {
            // Otra solicitud ganó la UNIQUE(EmpresaId, IdempotencyKey) después del
            // primer read. El ledger ganador es ahora la única autoridad: payload
            // equivalente => replay; payload distinto => conflicto determinista.
            var ganador = await _repository.ObtenerPorIdempotenciaAsync(
                scope.EmpresaId,
                key,
                cancellationToken);

            if (ganador is null)
            {
                // No degradar una colisión real a un éxito sin evidencia durable.
                // Si el ganador aún no es observable, propagar el fallo causal.
                throw;
            }

            return await ResolverReplayAsync(ganador, codigoPlan, inicioUtc, cancellationToken);
        }
    }

    public async Task<SuscripcionSaaSDto> ObtenerActualAsync(
        int empresaId,
        DateTime? instanteUtc = null,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolverTenantAsync(empresaId, cancellationToken);
        var instante = NormalizarUtc(instanteUtc ?? DateTime.UtcNow);
        var suscripcion = await _repository.ObtenerVigenteAsync(
            scope.EmpresaId,
            instante,
            cancellationToken)
            ?? throw new ResourceNotFoundException(
                $"{SuscripcionSaaSErrorCodes.SuscripcionNoEncontrada}: el tenant no posee una suscripción vigente.");

        var plan = await ObtenerPlanPersistidoAsync(suscripcion.PlanId, cancellationToken);
        return Mapear(suscripcion, plan);
    }

    public async Task<PaginaSuscripcionSaaSDto<LimiteSuscripcionSaaSDto>> ObtenerLimitesAsync(
        int empresaId,
        LimitesSuscripcionSaaSQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var normalizada = query.Normalizada();
        var scope = await ResolverTenantAsync(empresaId, cancellationToken);
        var suscripcion = await _repository.ObtenerVigenteAsync(
            scope.EmpresaId,
            DateTime.UtcNow,
            cancellationToken)
            ?? throw new ResourceNotFoundException(
                $"{SuscripcionSaaSErrorCodes.SuscripcionNoEncontrada}: el tenant no posee una suscripción vigente.");

        var plan = await ObtenerPlanPersistidoAsync(suscripcion.PlanId, cancellationToken);
        var consulta = plan.Limites
            .OrderBy(x => x.Clave)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(normalizada.Clave))
            consulta = consulta.Where(x => x.Clave == normalizada.Clave);

        var materializada = consulta.ToList();
        var total = materializada.Count;
        var items = materializada
            .Skip((normalizada.Pagina - 1) * normalizada.TamanoPagina)
            .Take(normalizada.TamanoPagina)
            .Select(x => new LimiteSuscripcionSaaSDto(x.Clave, x.ValorMaximo))
            .ToList();

        return new PaginaSuscripcionSaaSDto<LimiteSuscripcionSaaSDto>(
            items,
            normalizada.Pagina,
            normalizada.TamanoPagina,
            total);
    }

    public async Task<EntitlementModuloSaaSDto> EvaluarModuloAsync(
        int empresaId,
        string moduloClave,
        DateTime? instanteUtc = null,
        CancellationToken cancellationToken = default)
    {
        // La autoridad tenant se resuelve antes de cualquier lectura SaaS. La ruta
        // sólo selecciona; nunca concede acceso por sí misma.
        var scope = await ResolverTenantAsync(empresaId, cancellationToken);
        var instante = NormalizarUtc(instanteUtc ?? DateTime.UtcNow);
        var suscripcion = await _repository.ObtenerVigenteAsync(
            scope.EmpresaId,
            instante,
            cancellationToken);

        Plan? plan = null;
        IReadOnlyList<PlanModulo> reglas = Array.Empty<PlanModulo>();
        if (suscripcion is not null)
        {
            plan = await _repository.ObtenerPlanPorIdAsync(suscripcion.PlanId, cancellationToken);
            if (plan is not null)
            {
                reglas = await _repository.ObtenerModulosPlanAsync(
                    plan.Id,
                    plan.Codigo,
                    cancellationToken);
            }
        }

        var decision = PoliticaModulosSaaS.Evaluar(
            scope.EmpresaId,
            empresaId,
            suscripcion,
            plan,
            reglas,
            moduloClave,
            instante);

        var moduloNormalizado = string.IsNullOrWhiteSpace(moduloClave)
            ? string.Empty
            : moduloClave.Trim().ToUpperInvariant();

        return new EntitlementModuloSaaSDto(
            moduloNormalizado,
            decision.Habilitado,
            decision.Motivo,
            plan?.Id,
            plan?.Codigo);
    }

    private async Task<SuscripcionSaaSDto> ResolverReplayAsync(
        Suscripcion replay,
        string codigoPlan,
        DateTime inicioUtc,
        CancellationToken cancellationToken)
    {
        var replayPlan = await ObtenerPlanPersistidoAsync(replay.PlanId, cancellationToken);
        if (!string.Equals(replayPlan.Codigo, codigoPlan, StringComparison.Ordinal) ||
            replay.InicioUtc != inicioUtc)
        {
            throw new ConflictException(
                $"{SuscripcionSaaSErrorCodes.IdempotencyKeyConflictiva}: la Idempotency-Key ya fue usada con otro payload.");
        }

        return Mapear(replay, replayPlan);
    }

    private async Task<UsuarioTenantScopeActual> ResolverTenantAsync(
        int empresaId,
        CancellationToken cancellationToken)
    {
        if (empresaId <= 0)
        {
            throw new ForbiddenAccessException(
                $"{SuscripcionSaaSErrorCodes.TenantNoAutorizado}: tenant inválido.");
        }

        var scope = await _usuarioScopeService.ObtenerActualAsync(empresaId, cancellationToken);
        if (scope is null || scope.EmpresaId != empresaId)
        {
            throw new ForbiddenAccessException(
                $"{SuscripcionSaaSErrorCodes.TenantNoAutorizado}: el usuario no posee membresía activa en el tenant solicitado.");
        }

        return scope;
    }

    private async Task<Plan> ObtenerPlanPersistidoAsync(
        int planId,
        CancellationToken cancellationToken)
    {
        return await _repository.ObtenerPlanPorIdAsync(planId, cancellationToken)
            ?? throw new ResourceNotFoundException(
                $"{SuscripcionSaaSErrorCodes.PlanNoEncontrado}: el plan asociado a la suscripción no existe.");
    }

    private static SuscripcionSaaSDto Mapear(Suscripcion suscripcion, Plan plan) =>
        new(
            suscripcion.Id,
            plan.Codigo,
            plan.Nombre,
            suscripcion.Estado,
            suscripcion.InicioUtc,
            suscripcion.FinUtc);

    private static string NormalizarCodigoPlan(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("El código del plan es obligatorio.", nameof(codigo));

        return codigo.Trim().ToUpperInvariant();
    }

    private static string NormalizarIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency-Key es obligatorio.", nameof(idempotencyKey));

        var key = idempotencyKey.Trim();
        if (key.Length > IdempotencyKeyMaxLength)
            throw new ArgumentException($"Idempotency-Key no puede superar {IdempotencyKeyMaxLength} caracteres.", nameof(idempotencyKey));

        return key;
    }

    private static DateTime NormalizarUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
