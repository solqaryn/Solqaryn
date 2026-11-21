namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C04-01.M03.01: validacion PURA fail-closed del catalogo SaaS por tenant.
/// Todos los snapshots son evidencia obtenida en el servidor: no son DTO del cliente.
/// No sustituye JWT, RolPermiso, EF/MySQL ni autoriza cobros o escrituras.
/// </summary>
public static class C0401PlanDomainRules
{
    public sealed record VerifiedActor(
        int TenantEmpresaId,
        int ActorId,
        int RolId,
        int? SucursalAutorizadaId,
        bool TenantActivo,
        bool MembresiaActiva,
        bool RolActivo,
        bool PermisoRelacionalExplicito,
        bool PermisoRevocado);

    public sealed record PlanSnapshot(
        int PlanId,
        string? CodigoCanonico,
        long Revision,
        bool Activo,
        DateTime VigenteDesdeUtc,
        DateTime? VigenteHastaUtc);

    public sealed record SuscripcionSnapshot(
        int EmpresaId,
        int PlanId,
        long Version,
        bool Activa,
        DateTime InicioUtc,
        DateTime? FinUtc);

    public sealed record CapacidadSnapshot(
        string? Clave,
        long? Maximo,
        bool IlimitadoExplicito,
        long ConsumoActual,
        bool HabilitadaPorPlan,
        bool HabilitadaPorRubro,
        bool HabilitadaPorRol,
        bool HabilitadaPorTenant);

    public sealed record SolicitudUso(
        int EmpresaObjetivoId,
        int? SucursalRecursoId,
        long VersionSuscripcionEsperada,
        long RevisionPlanEsperada,
        string? ClaveCapacidad,
        long UnidadesAdicionales,
        bool SolicitaPrecioImpuestoProrrateoOCobro);

    public readonly record struct Decision(string Codigo)
    {
        // Las verificaciones de Domain nunca son una grant ni una orden ejecutable.
        public bool AutorizaAcceso => false;
        public bool AutorizaEscritura => false;
        public bool AutorizaCobro => false;
        public bool CertificaJwtMysqlOHttp => false;
    }

    public static Decision Evaluar(
        VerifiedActor? actor,
        PlanSnapshot? plan,
        SuscripcionSnapshot? suscripcion,
        CapacidadSnapshot? capacidad,
        SolicitudUso? solicitud,
        DateTime ahoraUtc)
    {
        if (actor is null || plan is null || suscripcion is null ||
            capacidad is null || solicitud is null ||
            actor.TenantEmpresaId <= 0 || actor.ActorId <= 0 || actor.RolId <= 0 ||
            solicitud.EmpresaObjetivoId <= 0)
            return Denegar("CONTEXTO_SERVIDOR_VERIFICADO_REQUERIDO");

        if (actor.TenantEmpresaId != solicitud.EmpresaObjetivoId ||
            suscripcion.EmpresaId != actor.TenantEmpresaId)
            return Denegar("TENANT_SUSCRIPCION_NO_COINCIDE");

        if (!actor.TenantActivo || !actor.MembresiaActiva || !actor.RolActivo ||
            actor.PermisoRevocado || !actor.PermisoRelacionalExplicito)
            return Denegar("RBAC_O_MEMBRESIA_DENEGADOS");

        if (solicitud.SucursalRecursoId is int sucursal &&
            (sucursal <= 0 || actor.SucursalAutorizadaId != sucursal))
            return Denegar("SUCURSAL_RECURSO_NO_AUTORIZADA");

        if (plan.PlanId <= 0 || !plan.Activo ||
            !CodigoPlanCanonico(plan.CodigoCanonico) ||
            suscripcion.PlanId != plan.PlanId)
            return Denegar("PLAN_NO_DISPONIBLE_O_DISTINTO");

        if (suscripcion.Version <= 0 || plan.Revision <= 0 ||
            solicitud.VersionSuscripcionEsperada != suscripcion.Version ||
            solicitud.RevisionPlanEsperada != plan.Revision)
            return Denegar("VERSION_O_REVISION_OBSOLETA");

        if (ahoraUtc.Kind != DateTimeKind.Utc ||
            plan.VigenteDesdeUtc.Kind != DateTimeKind.Utc ||
            suscripcion.InicioUtc.Kind != DateTimeKind.Utc ||
            (plan.VigenteHastaUtc.HasValue && plan.VigenteHastaUtc.Value.Kind != DateTimeKind.Utc) ||
            (suscripcion.FinUtc.HasValue && suscripcion.FinUtc.Value.Kind != DateTimeKind.Utc))
            return Denegar("EVIDENCIA_UTC_INVALIDA");

        if (!suscripcion.Activa || ahoraUtc < suscripcion.InicioUtc ||
            (suscripcion.FinUtc.HasValue && ahoraUtc >= suscripcion.FinUtc.Value) ||
            ahoraUtc < plan.VigenteDesdeUtc ||
            (plan.VigenteHastaUtc.HasValue && ahoraUtc >= plan.VigenteHastaUtc.Value) ||
            (plan.VigenteHastaUtc.HasValue && plan.VigenteHastaUtc.Value <= plan.VigenteDesdeUtc) ||
            (suscripcion.FinUtc.HasValue && suscripcion.FinUtc.Value <= suscripcion.InicioUtc))
            return Denegar("PLAN_O_SUSCRIPCION_NO_VIGENTES");

        // Las tarifas, tasas, periodicidad economica y cobros no fueron aprobados
        // por la microtarea M03.01. No inferir autoridad de un plan activo.
        if (solicitud.SolicitaPrecioImpuestoProrrateoOCobro)
            return Denegar("DECISION_COMERCIAL_O_COBRO_NO_AUTORIZADOS");

        if (!ClaveCapacidadCanonica(solicitud.ClaveCapacidad) ||
            !string.Equals(capacidad.Clave, solicitud.ClaveCapacidad, StringComparison.Ordinal))
            return Denegar("CAPACIDAD_CANONICA_NO_VERIFICADA");

        if (!capacidad.HabilitadaPorPlan || !capacidad.HabilitadaPorRubro ||
            !capacidad.HabilitadaPorRol || !capacidad.HabilitadaPorTenant)
            return Denegar("CAPACIDAD_NO_HABILITADA_EXPRESAMENTE");

        // null es ilimitado SOLO con consentimiento explicito del servidor.
        if ((capacidad.Maximo is null) != capacidad.IlimitadoExplicito ||
            capacidad.Maximo is <= 0)
            return Denegar("LIMITE_O_ILIMITADO_AMBIGUO");

        if (capacidad.ConsumoActual < 0 || solicitud.UnidadesAdicionales <= 0)
            return Denegar("CONSUMO_O_INCREMENTO_INVALIDO");

        if (capacidad.Maximo is long maximo &&
            (capacidad.ConsumoActual > maximo ||
             solicitud.UnidadesAdicionales > maximo - capacidad.ConsumoActual))
            return Denegar("CUOTA_INSUFICIENTE");

        // No se reserva cuota aqui. Application debe revalidar el permiso/tenant,
        // revisar la version y consumir la cuota en transaccion MySQL atomica.
        return Denegar("PRECONDICIONES_DOMINIO_SOLAMENTE_GATE_BACKEND_REQUERIDO");
    }

    private static Decision Denegar(string codigo) => new(codigo);

    private static bool CodigoPlanCanonico(string? codigo) =>
        codigo is { Length: >= 2 and <= 48 } &&
        codigo.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_');

    private static bool ClaveCapacidadCanonica(string? clave) =>
        clave is { Length: >= 3 and <= 96 } &&
        clave.Contains('.') &&
        clave.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-' or '_');
}
