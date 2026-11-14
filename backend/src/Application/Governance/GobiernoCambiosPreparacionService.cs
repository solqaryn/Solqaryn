using Solqaryn.Domain.Governance;

namespace Solqaryn.Application.Governance;

/// <summary>
/// Inputs for an inert consultation. IDs and the SHA are validated in Domain,
/// not trusted as a proof of tenant, RBAC, approval, or catalog membership.
/// </summary>
public sealed record SolicitudPreparacionCambio(
    string? MicrotareaId,
    string? DecisionId,
    string? Motivo,
    string? EvidenciaSha,
    string? IdempotencyKey,
    long RevisionEsperada,
    TipoCambioGobierno Tipo,
    bool RollbackDocumentado);

/// <summary>
/// Read-only projection. No returned value can authorize a write or an approval.
/// Motivo and IdempotencyKey are deliberately not echoed to consumers.
/// </summary>
public sealed record ResultadoPreparacionCambio(
    string MicrotareaId,
    string DecisionId,
    string EvidenciaSha,
    long RevisionEsperada,
    TipoCambioGobierno Tipo,
    EvaluacionCambioGobierno Evaluacion)
{
    public bool AutorizaEjecucion => false;
    public bool AutorizaPersistencia => false;
    public bool AutorizaConcesionPermisos => false;
    public string Estado => Evaluacion == EvaluacionCambioGobierno.NoGo
        ? "NO_GO"
        : "PENDIENTE_AUTORIZACION";
}

/// <summary>
/// C00-04.M03.02: Application use case limited to preparing a non-executable
/// governance review. Callers must independently verify current tenant, actor,
/// JWT/RBAC, decision authority, DAG and institutional CAS before any operation.
/// This class neither performs those checks nor grants those permissions.
/// </summary>
public sealed class GobiernoCambiosPreparacionService
{
    public ResultadoPreparacionCambio Preparar(SolicitudPreparacionCambio solicitud)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var propuesta = GobiernoCambiosReglas.CrearPropuesta(
            solicitud.MicrotareaId,
            solicitud.DecisionId,
            solicitud.Motivo,
            solicitud.EvidenciaSha,
            solicitud.IdempotencyKey,
            solicitud.RevisionEsperada,
            solicitud.Tipo,
            solicitud.RollbackDocumentado);

        return new ResultadoPreparacionCambio(
            propuesta.MicrotareaId,
            propuesta.DecisionId,
            propuesta.EvidenciaSha,
            propuesta.RevisionEsperada,
            propuesta.Tipo,
            propuesta.Evaluacion);
    }
}
