using Rules = global::Solqaryn.Domain.AutomationV5.C0401PlanDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Fuentes que DEBEN derivar identidad y RBAC de autoridad autenticada en servidor.
/// Esta microtarea define el contrato; NO instala un adaptador ni concede permisos.
/// </summary>
public interface IC0401VerifiedPlanActorSource
{
    ValueTask<C0401PlanApplicationService.AuthenticatedActor?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// El tenant viene del actor verificado, nunca de un identificador de cliente.
/// La implementacion real debe consultar membresia, plan y cuota en MySQL vigente.
/// </summary>
public interface IC0401VerifiedPlanSnapshotSource
{
    ValueTask<C0401PlanApplicationService.ServerPlanContext?> ReadForTenantAsync(
        int authenticatedTenantId, int planIdHint, CancellationToken cancellationToken);
}

/// <summary>
/// C04-01.M03.02: caso de uso de preflight; nunca autoriza cobros ni escrituras.
/// Los adaptadores reales JWT/RBAC/MySQL/HTTP y la transaccion atomica quedan
/// obligatoriamente para integraciones posteriores, no se registran en DI aqui.
/// </summary>
public sealed class C0401PlanApplicationService
{
    private readonly IC0401VerifiedPlanActorSource _actors;
    private readonly IC0401VerifiedPlanSnapshotSource _plans;
    private readonly TimeProvider _clock;

    public C0401PlanApplicationService(
        IC0401VerifiedPlanActorSource actors,
        IC0401VerifiedPlanSnapshotSource plans,
        TimeProvider? clock = null)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _clock = clock ?? TimeProvider.System;
    }

    public sealed record ClientIntent(
        int PlanIdHint,
        Rules.SolicitudUso? Solicitud,
        bool DeclaraAutoridadDeCliente = false,
        bool PideReservaOEscrituraDirecta = false);

    public sealed record AuthenticatedActor(
        Rules.VerifiedActor Scope,
        bool JwtFirmadoYValidado,
        bool TenantResueltoEnServidor,
        bool RbacRelacionalFresco,
        bool MembresiaFresca);

    public sealed record ServerPlanContext(
        Rules.PlanSnapshot Plan,
        Rules.SuscripcionSnapshot Suscripcion,
        Rules.CapacidadSnapshot Capacidad,
        bool ConsultaFiltradaPorTenant,
        bool CatalogoVerificadoEnServidor,
        bool VersionesFrescas);

    public readonly record struct Preview(string Code, bool RequiresLiveAtomicBackendGate)
    {
        public bool AuthorizesReadOrWrite => false;
        public bool ReservesQuota => false;
        public bool AuthorizesPayment => false;
        public bool CertifiesJwtMysqlHttp => false;
    }

    public async ValueTask<Preview> PrepareAsync(
        ClientIntent? intent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (intent is null || intent.PlanIdHint <= 0 ||
            intent.Solicitud is null ||
            intent.Solicitud.EmpresaObjetivoId <= 0 ||
            intent.Solicitud.VersionSuscripcionEsperada <= 0 ||
            intent.Solicitud.RevisionPlanEsperada <= 0 ||
            intent.Solicitud.UnidadesAdicionales <= 0)
            return Deny("CLIENT_INTENT_INVALID_OR_STALE");

        if (intent.DeclaraAutoridadDeCliente ||
            intent.PideReservaOEscrituraDirecta ||
            intent.Solicitud.SolicitaPrecioImpuestoProrrateoOCobro)
            return Deny("CLIENT_AUTHORITY_OR_PAYMENT_WRITE_DENIED");

        AuthenticatedActor? actor;
        try
        {
            actor = await _actors.ReadCurrentAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Deny("AUTHENTICATED_AUTHORITY_SOURCE_UNAVAILABLE");
        }
        cancellationToken.ThrowIfCancellationRequested();

        Rules.VerifiedActor? scope = actor?.Scope;
        if (scope is null || scope.TenantEmpresaId <= 0 ||
            scope.ActorId <= 0 || scope.RolId <= 0 ||
            !actor!.JwtFirmadoYValidado ||
            !actor.TenantResueltoEnServidor ||
            !actor.RbacRelacionalFresco ||
            !actor.MembresiaFresca ||
            !scope.TenantActivo || !scope.MembresiaActiva ||
            !scope.RolActivo || scope.PermisoRevocado ||
            !scope.PermisoRelacionalExplicito)
            return Deny("LIVE_SIGNED_JWT_RBAC_MEMBERSHIP_REQUIRED");

        if (scope.TenantEmpresaId != intent.Solicitud.EmpresaObjetivoId)
            return Deny("CLIENT_TENANT_HINT_CONFLICT");

        ServerPlanContext? snapshot;
        try
        {
            snapshot = await _plans.ReadForTenantAsync(
                scope.TenantEmpresaId, intent.PlanIdHint, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Deny("SERVER_PLAN_SOURCE_UNAVAILABLE");
        }
        cancellationToken.ThrowIfCancellationRequested();

        if (snapshot is null || snapshot.Plan is null ||
            snapshot.Suscripcion is null || snapshot.Capacidad is null ||
            !snapshot.ConsultaFiltradaPorTenant ||
            !snapshot.CatalogoVerificadoEnServidor ||
            !snapshot.VersionesFrescas ||
            snapshot.Plan.PlanId != intent.PlanIdHint ||
            snapshot.Suscripcion.EmpresaId != scope.TenantEmpresaId)
            return Deny("TRUSTED_TENANT_PLAN_SNAPSHOT_REQUIRED");

        var result = Rules.Evaluar(scope, snapshot.Plan, snapshot.Suscripcion,
            snapshot.Capacidad, intent.Solicitud, _clock.GetUtcNow().UtcDateTime);

        // Exito de precondiciones NO es un grant. La futura API debe revalidar
        // tenant, RBAC, cuota y versiones en la transaccion persistente.
        return result.Codigo == "PRECONDICIONES_DOMINIO_SOLAMENTE_GATE_BACKEND_REQUERIDO"
            ? new Preview("REAL_JWT_RBAC_MYSQL_TRANSACTION_STILL_REQUIRED", true)
            : Deny(result.Codigo);
    }

    private static Preview Deny(string code) => new(code, false);
}
